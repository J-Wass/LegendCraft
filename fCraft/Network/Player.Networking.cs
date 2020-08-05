﻿// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Xml.Linq;
using System.Xml;
using System.Security.Cryptography;
using fCraft.AutoRank;
using fCraft.Drawing;
using fCraft.Events;
using fCraft.MapConversion;
using System.Runtime;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> Represents a connection to a Minecraft client. Handles low-level interactions (e.g. networking). </summary>
    public sealed partial class Player
    {
        public static int SocketTimeout { get; set; }
        public static bool RelayAllUpdates { get; set; }
        const int SleepDelay = 5; // milliseconds
        const int SocketPollInterval = 200; // multiples of SleepDelay, approx. 1 second
        const int PingInterval = 3; // multiples of SocketPollInterval, approx. 3 seconds

        const string NoSmpMessage = "This server is for Minecraft Classic only.";

        static Player()
        {
            MaxBlockPlacementRange = 7 * 32;
            SocketTimeout = 10000;
        }


        public LeaveReason LeaveReason { get; private set; }

        public IPAddress IP { get; private set; }


        bool canReceive = true,
             canSend = true,
             canQueue = true;

        Thread ioThread;
        TcpClient client;
        readonly NetworkStream stream;
        PacketReader reader;
        PacketWriter writer;
        ConcurrentQueue<Packet> outputQueue = new ConcurrentQueue<Packet>(),
                                         priorityOutputQueue = new ConcurrentQueue<Packet>();


        internal static void StartSession([NotNull] TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException("tcpClient");
            new Player(tcpClient);
        }

        Player([NotNull] TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException("tcpClient");
            State = SessionState.Connecting;
            LoginTime = DateTime.UtcNow;
            LastActiveTime = DateTime.UtcNow;
            LastPatrolTime = DateTime.UtcNow;
            LeaveReason = LeaveReason.Unknown;
            LastUsedBlockType = Block.Undefined;

            client = tcpClient;
            client.SendTimeout = SocketTimeout;
            client.ReceiveTimeout = SocketTimeout;

            Brush = NormalBrushFactory.Instance;
            Metadata = new MetadataCollection<object>();

            try
            {
                IP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
                if (Server.RaiseSessionConnectingEvent(IP)) return;

                stream = client.GetStream();
                reader = new PacketReader(stream);
                writer = new PacketWriter(stream);

                ioThread = new Thread(IoLoop)
                {
                    Name = "LegendCraft.Session",
                    IsBackground = true
                };
                ioThread.Start();

            }
            catch (SocketException)
            {
                // Mono throws SocketException when accessing Client.RemoteEndPoint on disconnected sockets
                Disconnect();

            }
            catch (Exception ex)
            {
                Logger.LogAndReportCrash("Session failed to start", "LegendCraft", ex, false);
                Disconnect();
            }
        }


        #region I/O Loop
        void IoLoop()
        {
            try
            {
                Server.RaiseSessionConnectedEvent(this);

                // try to log the player in, otherwise die.
                if (!LoginSequence()) return;

                BandwidthUseMode = Info.BandwidthUseMode;

                // set up some temp variables
                Packet packet = new Packet();

                int pollCounter = 0,
                    pingCounter = 0;

                // main i/o loop
                while (canSend)
                {
                    int packetsSent = 0;

                    // detect player disconnect
                    if (pollCounter > SocketPollInterval)
                    {
                        if (!client.Connected ||
                            (client.Client.Poll(1000, SelectMode.SelectRead) && client.Client.Available == 0))
                        {
                            if (Info != null)
                            {
                                Logger.Log(LogType.Debug,
                                            "Player.IoLoop: Lost connection to player {0} ({1}).", Name, IP);
                            }
                            else
                            {
                                Logger.Log(LogType.Debug,
                                            "Player.IoLoop: Lost connection to unidentified player at {0}.", IP);
                            }
                            LeaveReason = LeaveReason.ClientQuit;
                            return;
                        }
                        if (pingCounter > PingInterval)
                        {
                            writer.WritePing();
                            BytesSent++;
                            pingCounter = 0;
                            MeasureBandwidthUseRates();
                        }
                        pingCounter++;
                        pollCounter = 0;
                        if (IsUsingWoM)
                        {
                            MessageNowPrefixed("", "^detail.user=" + InfoCommands.GetCompassString(Position.R));
                        }
                    }
                    pollCounter++;

                    if (DateTime.UtcNow.Subtract(lastMovementUpdate) > movementUpdateInterval)
                    {
                        UpdateVisibleEntities();
                        lastMovementUpdate = DateTime.UtcNow;
                    }

                    //check for autorank conditions every 15 seconds
                    if ((DateTime.UtcNow - lastAutorankCheck).TotalSeconds > 15 && Server.AutoRankEnabled && !IsAutoRankExempt)
                    {
                        AutoRankManager.Load();
                        AutoRankManager.Check(this);
                        lastAutorankCheck = DateTime.UtcNow;
                    }




                    // send output to player
                    while (canSend && packetsSent < Server.MaxSessionPacketsPerTick)
                    {
                        if (!priorityOutputQueue.TryDequeue(out packet))
                            if (!outputQueue.TryDequeue(out packet)) break;

                        if (IsDeaf && packet.OpCode == OpCode.Message) continue;
                        
                        writer.Write(packet.Data);
                        BytesSent += packet.Data.Length;
                        packetsSent++;

                        if (packet.OpCode == OpCode.Kick)
                        {
                            writer.Flush();
                            if (LeaveReason == LeaveReason.Unknown) LeaveReason = LeaveReason.Kick;
                            return;
                        }

                        if (DateTime.UtcNow.Subtract(lastMovementUpdate) > movementUpdateInterval)
                        {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }

                    // check if player needs to change worlds
                    if (canSend)
                    {
                        lock (joinWorldLock)
                        {
                            if (forcedWorldToJoin != null)
                            {
                                while (priorityOutputQueue.TryDequeue(out packet))
                                {
                                    writer.Write(packet.Data);
                                    BytesSent += packet.Data.Length;
                                    packetsSent++;
                                    if (packet.OpCode == OpCode.Kick)
                                    {
                                        writer.Flush();
                                        if (LeaveReason == LeaveReason.Unknown) LeaveReason = LeaveReason.Kick;
                                        return;
                                    }
                                }
                                if (!JoinWorldNow(forcedWorldToJoin, useWorldSpawn, worldChangeReason))
                                {
                                    Logger.Log(LogType.Warning,
                                                "Player.IoLoop: Player was asked to force-join a world, but it was full.");
                                    KickNow("World is full.", LeaveReason.ServerFull);
                                }
                                forcedWorldToJoin = null;
                            }
                        }

                        if (DateTime.UtcNow.Subtract(lastMovementUpdate) > movementUpdateInterval)
                        {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }


                    // get input from player
                    while (canReceive && stream.DataAvailable)
                    {
                        byte opcode = reader.ReadByte();
                        switch ((OpCode)opcode)
                        {

                            case OpCode.Message:
                                if (!ProcessMessagePacket()) return;
                                break;

                            case OpCode.Teleport:
                                ProcessMovementPacket();
                                break;

                            case OpCode.SetBlockClient:
                                ProcessSetBlockPacket();
                                break;

                            case OpCode.Ping:
                                BytesReceived++;
                                continue;

                            default:
                                Logger.Log(LogType.SuspiciousActivity,
                                            "Player {0} was kicked after sending an invalid opcode ({1}).",
                                            Name, opcode);
                                KickNow("Unknown packet opcode " + opcode,
                                         LeaveReason.InvalidOpcodeKick);
                                return;
                        }

                        if (DateTime.UtcNow.Subtract(lastMovementUpdate) > movementUpdateInterval)
                        {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }

                    Thread.Sleep(SleepDelay);
                }

            }
            catch (IOException)
            {
                LeaveReason = LeaveReason.ClientQuit;

            }
            catch (SocketException)
            {
                LeaveReason = LeaveReason.ClientQuit;
#if !DEBUG
            }
            catch (Exception ex)
            {
                LeaveReason = LeaveReason.ServerError;
                Logger.LogAndReportCrash("Error in Player.IoLoop", "800Craft", ex, false);
#endif
            }
            finally
            {
                canQueue = false;
                canSend = false;
                Disconnect();
            }
        }


        bool ProcessMessagePacket()
        {
            BytesReceived += 66;
            ResetIdleTimer();
            byte continuedMessage = reader.ReadByte();
            string message = reader.ReadString();

            if (String.IsNullOrEmpty(message))
            {
                Message("&WError with handling processed message!");
                return false;
            }

            if (!IsSuper && message.StartsWith("/womid "))
            {
                IsUsingWoM = true;
                return true;
            }

            if (message.IndexOf('&') >= 0)
            {
                Logger.Log(LogType.SuspiciousActivity,
                            "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.",
                            Name);
                Server.Message("{0}&W was kicked for sending invalid chat.", ClassyName);
                KickNow("Illegal characters in chat.", LeaveReason.InvalidMessageKick);
                return false;
            }
            
            if (continuedMessage == 1 && SupportsLongerMessages)
                message = message + "Ω";
#if DEBUG
                ParseMessage( message, false, true );
#else
            try
            {
                ParseMessage(message, false, true);
            }
            catch (IOException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (NullReferenceException)
            {
                //lol
            }
            catch (Exception ex)
            {
                Logger.LogAndReportCrash("Error while parsing player's message", "LegendCraft", ex, false);
                MessageNow("&WError while handling your message ({0}: {1})." +
                            "It is recommended that you reconnect to the server.",
                            ex.GetType().Name, ex.Message);
            }
#endif
            return true;
        }


        void ProcessMovementPacket()
        {
            BytesReceived += 10;
            byte held = reader.ReadByte();
            if (SupportsHeldBlock) Info.HeldBlock = (Block)held;

            Position newPos = new Position
            {
                X = reader.ReadInt16(),
                Z = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };

            Position oldPos = Position;

            // calculate difference between old and new positions
            Position delta = new Position
            {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs(newPos.R - oldPos.R),
                L = (byte)Math.Abs(newPos.L - oldPos.L)
            };

            // skip everything if player hasn't moved
            if (delta.IsZero) return;

            bool rotChanged = (delta.R != 0) || (delta.L != 0);

            // only reset the timer if player rotated
            // if player is just pushed around, rotation does not change (and timer should not reset)
            if (rotChanged) ResetIdleTimer();

            if (Info.IsFrozen)
            {
                // special handling for frozen players
                if (delta.X * delta.X + delta.Y * delta.Y > AntiSpeedMaxDistanceSquared ||
                    Math.Abs(delta.Z) > 40)
                {
                    SendNow(PacketWriter.MakeSelfTeleport(Position));
                }
                newPos.X = Position.X;
                newPos.Y = Position.Y;
                newPos.Z = Position.Z;

                // recalculate deltas
                delta.X = 0;
                delta.Y = 0;
                delta.Z = 0;

            }
            if (IsFlying)
            {
                Vector3I oldPosi = new Vector3I(oldPos.X / 32, oldPos.Y / 32, oldPos.Z / 32);
                Vector3I newPosi = new Vector3I(newPos.X / 32, newPos.Y / 32, newPos.Z / 32);
                //Checking e.Old vs e.New increases accuracy, checking old vs new uses a lot less updates
                if ((oldPosi.X != newPosi.X) || (oldPosi.Y != newPosi.Y) || (oldPosi.Z != newPosi.Z))
                {
                    //finally, /fly decends
                    if ((oldPos.Z > newPos.Z))
                    {
                        foreach (Vector3I block in FlyCache.Values)
                        {
                            SendNow(PacketWriter.MakeSetBlock(block, Block.Air));
                            Vector3I removed;
                            FlyCache.TryRemove(block.ToString(), out removed);
                        }
                    }
                    // Create new block parts
                    for (int i = -1; i <= 1; i++) //reduced width and length by 1
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = 2; k <= 3; k++) //added a 2nd layer
                            {
                                Vector3I layer = new Vector3I(newPosi.X + i, newPosi.Y + j, newPosi.Z - k);
                                if (World.Map.GetBlock(layer) == Block.Air)
                                {
                                    SendNow(PacketWriter.MakeSetBlock(layer, Block.Glass));
                                    FlyCache.TryAdd(layer.ToString(), layer);
                                }
                            }
                        }
                    }


                    // Remove old blocks
                    foreach (Vector3I block in FlyCache.Values)
                    {
                        if (fCraft.Utils.FlyHandler.CanRemoveBlock(this, block, newPosi))
                        {
                            SendNow(PacketWriter.MakeSetBlock(block, Block.Air));
                            Vector3I removed;
                            FlyCache.TryRemove(block.ToString(), out removed);
                        }
                    }
                }
            }

            else if (!Can(Permission.UseSpeedHack))
            {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // speedhack detection
                if (DetectMovementPacketSpam())
                {
                    return;

                }
                else if ((distSquared - delta.Z * delta.Z > AntiSpeedMaxDistanceSquared || delta.Z > AntiSpeedMaxJumpDelta) &&
                         speedHackDetectionCounter >= 0)
                {

                    if (speedHackDetectionCounter == 0)
                    {
                        lastValidPosition = Position;
                    }
                    else if (speedHackDetectionCounter > 1)
                    {
                        DenyMovement();
                        speedHackDetectionCounter = 0;
                        return;
                    }
                    speedHackDetectionCounter++;

                }
                else
                {
                    speedHackDetectionCounter = 0;
                }
            }

            if (RaisePlayerMovingEvent(this, newPos))
            {
                DenyMovement();
                return;
            }

            Position = newPos;
            RaisePlayerMovedEvent(this, oldPos);
        }


        void ProcessSetBlockPacket()
        {
            BytesReceived += 9;
            if (World == null || World.Map == null) return;
            ResetIdleTimer();
            short x = reader.ReadInt16();
            short z = reader.ReadInt16();
            short y = reader.ReadInt16();
            ClickAction action = (reader.ReadByte() == 1) ? ClickAction.Build : ClickAction.Delete;
            byte type = reader.ReadByte();

            // if a player is using InDev or SurvivalTest client, they may try to
            // place blocks that are not found in MC Classic. Convert them!
            if (type > 49)
            {
                type = MapDat.MapBlock(type);
            }

            Vector3I coords = new Vector3I(x, y, z);

            // If block is in bounds, count the click.
            // Sometimes MC allows clicking out of bounds,
            // like at map transitions or at the top layer of the world.
            // Those clicks should be simply ignored.
            if (World.Map.InBounds(coords))
            {
                var e = new PlayerClickingEventArgs(this, coords, action, (Block)type);
                if (RaisePlayerClickingEvent(e))
                {
                    RevertBlockNow(coords);
                }
                else
                {
                    RaisePlayerClickedEvent(this, coords, e.Action, e.Block);
                    PlaceBlock(coords, e.Action, e.Block);
                }
            }
        }

        #endregion


        void Disconnect()
        {
            State = SessionState.Disconnected;
            Server.UnregisterSession(this);
            Server.RaiseSessionDisconnectedEvent(this, LeaveReason);

            if (HasRegistered)
            {
                Server.UnregisterPlayer(this);
                RaisePlayerDisconnectedEvent(this, LeaveReason);
            }

            if (reader != null)
            {
                reader.Close();
                reader = null;
            }

            if (writer != null)
            {
                writer.Close();
                writer = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }

            ioThread = null;
        }


        bool LoginSequence()
        {

            byte opcode = reader.ReadByte();

            switch (opcode)
            {
                case (byte)OpCode.Handshake:
                    break;

                case 2:
                    GentlyKickBetaClients();
                    return false;

                //GET request, check if ServeCfg or WebPanelConfig
                case (byte)'G':
                   
                    ServeCfg();         
                    return false;

                default:
                    Logger.Log(LogType.Error,
                                "Player.LoginSequence: Unexpected opcode in the first packet from {0}: {1}.",
                                IP, opcode);
                    KickNow("Incompatible client, or a network error.", LeaveReason.ProtocolViolation);
                    return false;
            }

            // Check protocol version
            int clientProtocolVersion = reader.ReadByte();
            if (clientProtocolVersion != Config.ProtocolVersion)
            {
                Logger.Log(LogType.Error,
                            "Player.LoginSequence: Wrong protocol version: {0}.",
                            clientProtocolVersion);
                KickNow("Incompatible protocol version!", LeaveReason.ProtocolViolation);
                return false;
            }

            //raw name from player ID packet from the client
            string givenName = reader.ReadString();
            string packetPlayerName = givenName; //make a copy of the full name, in case Mojang support is needed
            bool UsedMojang = false;

            //verificication key found in player ID packet
            string verificationKey = reader.ReadString();
            ClassiCube = Server.VerifyName(givenName, verificationKey, Heartbeat.Salt2);          
            
            byte magicNum = reader.ReadByte(); //for CPE check (previously unused)
            usesCPE = (magicNum == 0x42);
            Skinname = givenName;
            BytesReceived += 131;


            // Check name for nonstandard characters
            if (!IsValidName(givenName))
            {
                //check if email, provide crappy support here
                if (givenName.Contains("@"))
                { //if the user is an email address
                    UsedMojang = true;
                    PlayerInfo[] temp = PlayerDB.FindPlayerInfoByEmail(givenName); //check if they have been here before
                    if (temp != null && temp.Length == 1)
                    { //restore name if needed
                        givenName = temp[0].Name;
                    }
                    else
                    { //else, new player. Build a unique name
                        Logger.Log(LogType.SystemActivity, "Email account " + givenName + " connected, attemping to create unique new name");

                        int nameAppend = 1;//by default, set number to 1                        
                        string trimmedName = givenName.Split('@')[0].Replace("@", ""); //this should be the first part of the name ("Jonty800"@email.com)
                        if (trimmedName == null) throw new ArgumentNullException("trimmedName");
                        if (trimmedName.Length > 16)
                        {
                            trimmedName = trimmedName.Substring(0, 15 - (nameAppend.ToString().Length)); //shorten name
                        }
                        foreach (char ch in trimmedName.ToCharArray())
                        { //replace invalid chars with "_"
                            if ((ch < '0' && ch != '.') || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z')
                            {
                                trimmedName = trimmedName.Replace(ch, '_');
                            }
                        }
                        while (PlayerDB.PlayerInfoList.Contains(PlayerDB.FindPlayerInfoExact(trimmedName + "." + nameAppend.ToString())))//while the name is taken
                        {
                            nameAppend++; //while the default name is taken, continuously add one until the name is free (shouldn't be a very long loop at all)
                        }
                        givenName = trimmedName + "." + nameAppend.ToString(); //Once the name is free, the new generated number is added to the end of the player

                        //run a test to see if it is unique or not (unable to test)
                        PlayerInfo[] Players = PlayerDB.FindPlayers(givenName); //gather matches
                        while (Players.Length != 0)
                        { //while matches were found
                            //Name already exists. Reroll
                            if (givenName.Length > 14)
                            { //kick if substring causes invalid name
                                Logger.Log(LogType.SuspiciousActivity,
                                "Player.LoginSequence: Unacceptable player name, player failed to get new name", IP);
                                KickNow("Invalid characters in player name!", LeaveReason.ProtocolViolation);
                                return false;
                                //returning false breaks loops in C#, right? haha been a while
                            }
                            givenName = givenName + (nameAppend + 1); //keep adding a new number to the end of the string until unique
                            Players = PlayerDB.FindPlayers(givenName); //update Players based on new name
                        }
                    }
                }
                else if (ClassiCube)
                {
                    //if the name is still invalid with or without the +
                    if (!IsValidName(givenName))
                    {
                        Logger.Log(LogType.SuspiciousActivity, "Player.LoginSequence: Unacceptable player name: {0} ({1})", givenName, IP);                                     
                        KickNow("Invalid characters in player name!", LeaveReason.ProtocolViolation);
                        return false;
                    }
                }
                else
                {
                    Logger.Log(LogType.SuspiciousActivity,
                                "Player.LoginSequence: Unacceptable player name: {0} ({1})",
                                givenName, IP);
                    KickNow("Invalid characters in player name!", LeaveReason.ProtocolViolation);
                    return false;
                }
            }

            // ReSharper disable PossibleNullReferenceException
            Position = WorldManager.MainWorld.Map.Spawn;
            // ReSharper restore PossibleNullReferenceException

            Info = PlayerDB.FindOrCreateInfoForPlayer(givenName, IP);
            ResetAllBinds();

            //if the user is not from classicube, go ahead and verify for minecraft
            if ((!ClassiCube && Server.VerifyName(packetPlayerName, verificationKey, Heartbeat.Salt)) || ClassiCube && Server.VerifyName(packetPlayerName, verificationKey, Heartbeat.Salt2))
            {
                IsVerified = true;
                // update capitalization of player's name
                if (!Info.Name.Equals(givenName, StringComparison.Ordinal))
                {
                    Info.Name = givenName;
                }
                if (UsedMojang)
                {
                    Info.MojangAccount = packetPlayerName;
                }
            }
            else
            {

                NameVerificationMode nameVerificationMode = ConfigKey.VerifyNames.GetEnum<NameVerificationMode>();

                string standardMessage = String.Format("Player.LoginSequence: Could not verify player name for {0} ({1}).",
                                                        Name, IP);
                if (IP.Equals(IPAddress.Loopback) && nameVerificationMode != NameVerificationMode.Always)
                {
                    Logger.Log(LogType.SuspiciousActivity,
                                "{0} Player was identified as connecting from localhost and allowed in.",
                                standardMessage);
                    IsVerified = true;

                }
                else if (IP.IsLAN() && ConfigKey.AllowUnverifiedLAN.Enabled())
                {
                    Logger.Log(LogType.SuspiciousActivity,
                                "{0} Player was identified as connecting from LAN and allowed in.",
                                standardMessage);
                    IsVerified = true;

                }
                else if (Info.TimesVisited > 1 && Info.LastIP.Equals(IP))
                {
                    switch (nameVerificationMode)
                    {
                        case NameVerificationMode.Always:
                            Info.ProcessFailedLogin(this);
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP matched previous records for that name. " +
                                        "Player was kicked anyway because VerifyNames is set to Always.",
                                        standardMessage);
                            KickNow("Could not verify player name!", LeaveReason.UnverifiedName);
                            return false;

                        case NameVerificationMode.Balanced:
                        case NameVerificationMode.Never:
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP matched previous records for that name. Player was allowed in.",
                                        standardMessage);
                            IsVerified = true;
                            break;
                    }

                }
                else
                {
                    switch (nameVerificationMode)
                    {
                        case NameVerificationMode.Always:
                        case NameVerificationMode.Balanced:
                            Info.ProcessFailedLogin(this);
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP did not match. Player was kicked.",
                                        standardMessage);
                            KickNow("Could not verify player name!", LeaveReason.UnverifiedName);
                            return false;

                        case NameVerificationMode.Never:
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP did not match. Player was allowed in anyway because VerifyNames is set to Never.",
                                        standardMessage);
                            Message("&WYour name could not be verified.");
                            break;
                    }
                }
            }

            // Check if player is banned
            if (Info.IsBanned)
            {
                Info.ProcessFailedLogin(this);
                Logger.Log(LogType.SuspiciousActivity,
                            "Banned player {0} tried to log in from {1}",
                            Name, IP);
                string bannedMessage;
                if (Info.BannedBy != null)
                {
                    if (Info.BanReason != null)
                    {
                        bannedMessage = String.Format("Banned {0} ago by {1}: {2}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BannedBy,
                                                       Info.BanReason);
                    }
                    else
                    {
                        bannedMessage = String.Format("Banned {0} ago by {1}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BannedBy);
                    }
                }
                else
                {
                    if (Info.BanReason != null)
                    {
                        bannedMessage = String.Format("Banned {0} ago: {1}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BanReason);
                    }
                    else
                    {
                        bannedMessage = String.Format("Banned {0} ago",
                                                       Info.TimeSinceBan.ToMiniString());
                    }
                }
                KickNow(bannedMessage, LeaveReason.LoginFailed);
                return false;
            }


            // Check if player's IP is banned
            IPBanInfo ipBanInfo = IPBanList.Get(IP);
            if (ipBanInfo != null && Info.BanStatus != BanStatus.IPBanExempt)
            {
                Info.ProcessFailedLogin(this);
                ipBanInfo.ProcessAttempt(this);
                Logger.Log(LogType.SuspiciousActivity,
                            "{0} tried to log in from a banned IP.", Name);
                string bannedMessage = String.Format("IP-banned {0} ago by {1}: {2}",
                                                      DateTime.UtcNow.Subtract(ipBanInfo.BanDate).ToMiniString(),
                                                      ipBanInfo.BannedBy,
                                                      ipBanInfo.BanReason);
                KickNow(bannedMessage, LeaveReason.LoginFailed);
                return false;
            }

            // negotiate protocol extensions, if applicable  (FROM FEMTOCRAFT: http://svn.fcraft.net:8080/svn/femtocraft/ - to be compatible with CPE)
            if (Config.ProtocolExtension && magicNum == 0x42)
            {
                if (!NegotiateProtocolExtension()) return false;
            }

            // Check if max number of connections is reached for IP
            if (!Server.RegisterSession(this))
            {
                Info.ProcessFailedLogin(this);
                Logger.Log(LogType.SuspiciousActivity,
                            "Player.LoginSequence: Denied player {0}: maximum number of connections was reached for {1}",
                            givenName, IP);
                KickNow(String.Format("Max connections reached for {0}", IP), LeaveReason.LoginFailed);
                return false;
            }


            // Check if player is paid (if required)
            if (ConfigKey.PaidPlayersOnly.Enabled())
            {
                SendNow(PacketWriter.MakeHandshake(this,
                                                     ConfigKey.ServerName.GetString(),
                                                     "Please wait; Checking paid status..."));
                writer.Flush();

            }


            // Any additional security checks should be done right here
            if (RaisePlayerConnectingEvent(this)) return false;


            // ----==== beyond this point, player is considered connecting (allowed to join) ====----

            // Register player for future block updates
            if (!Server.RegisterPlayer(this))
            {
                Logger.Log(LogType.SystemActivity,
                            "Player {0} was kicked because server is full.", Name);
                string kickMessage = String.Format("Sorry, server is full ({0}/{1})",
                                        Server.Players.Length, ConfigKey.MaxPlayers.GetInt());
                KickNow(kickMessage, LeaveReason.ServerFull);
                return false;
            }
            Info.ProcessLogin(this);
            State = SessionState.LoadingMain;


            // ----==== Beyond this point, player is considered connected (authenticated and registered) ====----
            Logger.Log(LogType.UserActivity, "Player {0} connected from {1}.", Name, IP);


            // Figure out what the starting world should be
            World startingWorld = Info.Rank.MainWorld ?? WorldManager.MainWorld;
            startingWorld = RaisePlayerConnectedEvent(this, startingWorld);

            // Send server information, set motd
            string serverName = ConfigKey.ServerName.GetString();
            string motd;
            if (ConfigKey.WoMEnableEnvExtensions.Enabled())
            {
                if (usesCPE)
                {
                    if (!startingWorld.Hax)
                    {
                        motd =  ConfigKey.MOTD.GetString() + "-hax";
                    }
                    else
                    {
                        motd = ConfigKey.MOTD.GetString();
                    }
                }
                else
                {
                    if (IP.Equals(IPAddress.Loopback))
                    {
                        motd = "&0cfg=localhost:" + Server.Port + "/" + startingWorld.Name + "~motd";
                    }
                    else
                    {
                        motd = "&0cfg=" + Server.ExternalIP + ":" + Server.Port + "/" + startingWorld.Name + "~motd";
                    }
                }
            }
            else
            {
                motd = ConfigKey.MOTD.GetString();
            }
            SendNow(PacketWriter.MakeHandshake(this, serverName, motd));
          
            bool firstTime = (Info.TimesVisited == 1);

            if (!JoinWorldNow(startingWorld, true, WorldChangeReason.FirstWorld))
            {
                Logger.Log(LogType.Warning,
                            "Could not load main world ({0}) for connecting player {1} (from {2}): " +
                            "Either main world is full, or an error occured.",
                            startingWorld.Name, Name, IP);
                KickNow("Either main world is full, or an error occured.", LeaveReason.WorldFull);
                return false;
            }


            // ==== Beyond this point, player is considered ready (has a world) ====
            
            // Sets client block permissions.
            if(SupportsBlockPermissions) {
                SendBlockPermissions();
            }

            var canSee = Server.Players.CanSee(this);

            // Announce join
            if (ConfigKey.ShowConnectionMessages.Enabled())
            {
                // ReSharper disable AssignNullToNotNullAttribute
                string message = Server.MakePlayerConnectedMessage(this, firstTime, World);
                // ReSharper restore AssignNullToNotNullAttribute
                canSee.Message(message);
            }

            //reset tempDisplName if player still has
            if (Info.tempDisplayedName != null)
            {
                Info.tempDisplayedName = null;
            }

            if (Info.IsHidden)
            {
                if (Can(Permission.Hide))
                {
                    canSee.Message("&8Player {0}&8 logged in hidden. Pssst.", ClassyName);
                }
                else
                {
                    Info.IsHidden = false;
                }
            }

            // Check if other banned players logged in from this IP
            PlayerInfo[] bannedPlayerNames = PlayerDB.FindPlayers(IP, 25)
                                                     .Where(playerFromSameIP => playerFromSameIP.IsBanned)
                                                     .ToArray();
            if (bannedPlayerNames.Length > 0)
            {
                canSee.Message("&WPlayer {0}&W logged in from an IP shared by banned players: {1}",
                                ClassyName, bannedPlayerNames.JoinToClassyString());
                Logger.Log(LogType.SuspiciousActivity,
                            "Player {0} logged in from an IP shared by banned players: {1}",
                                ClassyName, bannedPlayerNames.JoinToString(info => info.Name));
            }

            // check if player is still muted
            if (Info.MutedUntil > DateTime.UtcNow)
            {
                Message("&WYou were previously muted by {0}&W, {1} left.",
                         Info.MutedByClassy, Info.TimeMutedLeft.ToMiniString());
                canSee.Message("&WPlayer {0}&W was previously muted by {1}&W, {2} left.",
                                ClassyName, Info.MutedByClassy, Info.TimeMutedLeft.ToMiniString());
            }

            // check if player is still frozen
            if (Info.IsFrozen)
            {
                if (Info.FrozenOn != DateTime.MinValue)
                {
                    Message("&WYou were previously frozen {0} ago by {1}",
                             Info.TimeSinceFrozen.ToMiniString(),
                             Info.FrozenByClassy);
                    canSee.Message("&WPlayer {0}&W was previously frozen {1} ago by {2}",
                                    ClassyName,
                                    Info.TimeSinceFrozen.ToMiniString(),
                                    Info.FrozenByClassy);
                }
                else
                {
                    Message("&WYou were previously frozen by {0}",
                             Info.FrozenByClassy);
                    canSee.Message("&WPlayer {0}&W was previously frozen by {1}",
                                    ClassyName, Info.FrozenByClassy);
                }
            }

            // Welcome message
            if (File.Exists(Paths.GreetingFileName))
            {
                string[] greetingText = File.ReadAllLines(Paths.GreetingFileName);
                foreach (string greetingLine in greetingText)
                {
                    MessageNow(Server.ReplaceTextKeywords(this, greetingLine));
                }
            }
            else
            {
                if (firstTime)
                {
                    MessageNow("Welcome to {0}", ConfigKey.ServerName.GetString());
                }
                else
                {
                    MessageNow("Welcome back to {0}", ConfigKey.ServerName.GetString());
                }

                MessageNow("Your rank is {0}&S. Type &H/Help&S for help.",
                            Info.Rank.ClassyName);
            }

            // A reminder for first-time users
            if (PlayerDB.Size == 1 && Info.Rank != RankManager.HighestRank)
            {
                Message("Type &H/Rank {0} {1}&S in console to promote yourself",
                         Name, RankManager.HighestRank.Name);
            }

            InitCopySlots();

            HasFullyConnected = true;
            State = SessionState.Online;
            Server.UpdatePlayerList();
            RaisePlayerReadyEvent(this);
            AutoRankManager.Load();
            AutoRankManager.Check(this);

            return true;
        }


        void GentlyKickBetaClients()
        {
            // This may be someone connecting with an SMP client
            int strLen = reader.ReadInt16();

            if (strLen >= 2 && strLen <= 16)
            {
                string smpPlayerName = Encoding.BigEndianUnicode.GetString(reader.ReadBytes(strLen * 2));

                Logger.Log(LogType.Warning,
                            "Player.LoginSequence: Player \"{0}\" tried connecting with Minecraft Beta client from {1}. " +
                            "LegendCraft does not support Minecraft Beta.",
                            smpPlayerName, IP);

                // send SMP KICK packet
                writer.Write((byte)255);
                byte[] stringData = Encoding.BigEndianUnicode.GetBytes(NoSmpMessage);
                writer.Write((short)NoSmpMessage.Length);
                writer.Write(stringData);
                BytesSent += (1 + stringData.Length);
                writer.Flush();

            }
            else
            {
                // Not SMP client (invalid player name length)
                Logger.Log(LogType.Error,
                            "Player.LoginSequence: Unexpected opcode in the first packet from {0}: 2.", IP);
                KickNow("Unexpected handshake message - possible protocol mismatch!", LeaveReason.ProtocolViolation);
            }
        }

        static readonly Regex HttpFirstLine = new Regex("GET /([a-zA-Z0-9_]{1,16})(~motd)? .+", RegexOptions.Compiled);
       
        void ServeCfg()
        {
            using (StreamReader textReader = new StreamReader(stream))
            {
                using (StreamWriter textWriter = new StreamWriter(stream))
                {
                    string firstLine = "G" + textReader.ReadLine();
                    var match = HttpFirstLine.Match(firstLine);
                    
                    if (match.Success)
                    {
                        string worldName = match.Groups[1].Value;
                        bool firstTime = match.Groups[2].Success;
                        World world = WorldManager.FindWorldExact(worldName);
                        if (world != null)
                        {
                            string cfg = world.GenerateWoMConfig(firstTime);
                            byte[] content = Encoding.UTF8.GetBytes(cfg);
                            textWriter.WriteLine("HTTP/1.1 200 OK");
                            textWriter.WriteLine("Date: " + DateTime.UtcNow.ToString("R"));
                            textWriter.WriteLine("Content-Type: text/plain");
                            textWriter.WriteLine("Content-Length: " + content.Length);
                            textWriter.WriteLine();
                            textWriter.WriteLine(cfg);
                        }
                        else
                        {
                            textWriter.WriteLine("HTTP/1.1 404 Not Found");
                        }
                    }
                    else
                    {
                        textWriter.WriteLine("HTTP/1.1 400 Bad Request");
                    }
                }
            }
        }



        #region Joining Worlds

        readonly object joinWorldLock = new object();

        [CanBeNull]
        World forcedWorldToJoin;
        WorldChangeReason worldChangeReason;
        Position postJoinPosition;
        bool useWorldSpawn;

        public void JoinWorld([NotNull] World newWorld, WorldChangeReason reason)
        {
            if (newWorld == null) throw new ArgumentNullException("newWorld");

            lock (joinWorldLock)
            {
                useWorldSpawn = true;
                postJoinPosition = Position.Zero;
                forcedWorldToJoin = newWorld;
                worldChangeReason = reason;
            }
        }


        public void JoinWorld([NotNull] World newWorld, WorldChangeReason reason, Position position)
        {
            if (newWorld == null) throw new ArgumentNullException("newWorld");
            if (!Enum.IsDefined(typeof(WorldChangeReason), reason))
            {
                throw new ArgumentOutOfRangeException("reason");
            }

            lock (joinWorldLock)
            {
                useWorldSpawn = false;
                postJoinPosition = position;
                forcedWorldToJoin = newWorld;
                worldChangeReason = reason;
            }
        }


        internal bool JoinWorldNow([NotNull] World newWorld, bool doUseWorldSpawn, WorldChangeReason reason)
        {
            if (newWorld == null) throw new ArgumentNullException("newWorld");
            if (!Enum.IsDefined(typeof(WorldChangeReason), reason))
            {
                throw new ArgumentOutOfRangeException("reason");
            }
            if (Thread.CurrentThread != ioThread)
            {
                throw new InvalidOperationException("Player.JoinWorldNow may only be called from player's own thread. " +
                                                     "Use Player.JoinWorld instead.");
            }

            string textLine1 = ConfigKey.ServerName.GetString();
            string textLine2 = "Loading world " + newWorld.ClassyName;
            if (!newWorld.Hax) textLine2 += " -hax";

            if (RaisePlayerJoiningWorldEvent(this, newWorld, reason, textLine1, textLine2))
            {
                Logger.Log(LogType.Warning,
                            "Player.JoinWorldNow: Player {0} was prevented from joining world {1} by an event callback.",
                            Name, newWorld.Name);
                return false;
            }

            World oldWorld = World;

            // remove player from the old world
            if (oldWorld != null && oldWorld != newWorld)
            {
                if (!oldWorld.ReleasePlayer(this))
                {
                    Logger.Log(LogType.Error,
                                "Player.JoinWorldNow: Player asked to be released from its world, " +
                                "but the world did not contain the player.");
                }
            }


            ResetVisibleEntities();

            ClearLowPriotityOutputQueue();

            Map map;

            // try to join the new world
            if (oldWorld != newWorld)
            {
                bool announce = (oldWorld != null) && (oldWorld.Name != newWorld.Name);
                map = newWorld.AcceptPlayer(this, announce);
                if (map == null)
                {
                    return false;
                }
            }
            else
            {
                map = newWorld.LoadMap();
            }
            World = newWorld;

            // Set spawn point
            if (doUseWorldSpawn)
            {
                Position = map.Spawn;
            }
            else
            {
                Position = postJoinPosition;
            }

            // Start sending over the level copy
            if (oldWorld != null)
            {
                SendNow(PacketWriter.MakeHandshake(this, textLine1, textLine2));
            }

            writer.WriteMapBegin();
            BytesSent++;

            // enable Nagle's algorithm (in case it was turned off by LowLatencyMode)
            // to avoid wasting bandwidth for map transfer
            client.NoDelay = false;

            // Fetch compressed map copy
            byte[] buffer = new byte[1024];
            int mapBytesSent = 0;
            byte[] blockData;
         
            using (MemoryStream mapStream = new MemoryStream())
            {
                using (GZipStream compressor = new GZipStream(mapStream, CompressionMode.Compress))
                {
                    int convertedBlockCount = IPAddress.HostToNetworkOrder(map.Volume);
                    compressor.Write(BitConverter.GetBytes(convertedBlockCount), 0, 4);
                    byte[] rawData = (UsesCustomBlocks ? map.Blocks : map.GetFallbackMap());
                    compressor.Write(rawData, 0, rawData.Length);
                }
                blockData = mapStream.ToArray();
            }

            Logger.Log(LogType.Debug,
                        "Player.JoinWorldNow: Sending compressed map ({0} bytes) to {1}.",
                        blockData.Length, Name);

            // Transfer the map copy
            while (mapBytesSent < blockData.Length)
            {
                int chunkSize = blockData.Length - mapBytesSent;
                if (chunkSize > 1024)
                {
                    chunkSize = 1024;
                }
                else
                {
                    // CRC fix for ManicDigger
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = 0;
                    }
                }
                Array.Copy(blockData, mapBytesSent, buffer, 0, chunkSize);
                byte progress = (byte)(100 * mapBytesSent / blockData.Length);

                // write in chunks of 1024 bytes or less
                writer.WriteMapChunk(buffer, chunkSize, progress);
                BytesSent += 1028;
                mapBytesSent += chunkSize;
            }

            // Turn off Nagel's algorithm again for LowLatencyMode
            client.NoDelay = ConfigKey.LowLatencyMode.Enabled();

            // Done sending over level copy
            writer.WriteMapEnd(map);
            BytesSent += 7;

            // Sets player's spawn point to map spawn
            writer.WriteAddEntity(255, this, map.Spawn);
            BytesSent += 74;

            // Teleport player to the target location
            // This allows preserving spawn rotation/look, and allows
            // teleporting player to a specific location (e.g. /TP or /Bring)
            writer.WriteTeleport(255, Position);
            BytesSent += 10;

            if (World.IsRealm && oldWorld == newWorld)
            {
                Message("Rejoined realm {0}", newWorld.ClassyName);
            }
            else if (World.IsRealm)
            {
                Message("Joined realm {0}", newWorld.ClassyName);
                if (World != WorldManager.MainWorld)
                {
                    World.VisitCount++;
                }
            } if (!World.IsRealm && oldWorld == newWorld)
            {
                Message("Rejoined world {0}", newWorld.ClassyName);
            }
            else if (!World.IsRealm)
            {
                Message("Joined world {0}", newWorld.ClassyName);
                if (World != WorldManager.MainWorld)
                {
                    World.VisitCount++;
                }
            }

            //revert game values if a player leaves a world
            RaisePlayerJoinedWorldEvent(this, oldWorld, reason);
            Info.tempDisplayedName = null;
            iName = null;
            GunMode = false;
            entityChanged = false;

            //FFA
            Info.isPlayingFFA = false;
            Info.gameDeathsFFA = 0;
            Info.gameKillsFFA = 0;

            //Infection
            Info.isPlayingInfection = false;
            Info.isInfected = false;

            //TDM
            Info.isPlayingTD = false;
            Info.isOnRedTeam = false;
            Info.isOnBlueTeam = false;
            Info.gameKills = 0;
            Info.gameDeaths = 0;

            //CTF
            Info.isPlayingCTF = false;
            Info.CTFRedTeam = false;
            Info.CTFBlueTeam = false;
            Info.CTFCaptures = 0;
            Info.CTFKills = 0;
            Info.hasRedFlag = false;
            Info.hasBlueFlag = false;
            Info.placingBlueFlag = false;
            Info.placingRedFlag = false;

            if (SupportsEnvMapAppearance) {
                Send(PacketWriter.MakeEnvSetMapAppearance(World.textureURL, World.SideBlock, World.EdgeBlock, World.EdgeLevel));
            }
            if (SupportsEnvWeatherType) {
                Send(PacketWriter.MakeEnvWeatherAppearance((byte)World.WeatherCC));
            }
            if (SupportsMessageTypes) {
                //reset all special messages
                Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));
                Send(PacketWriter.MakeSpecialMessage((byte)1, "&f"));
                Send(PacketWriter.MakeSpecialMessage((byte)2, "&f"));
            }
            if (SupportsEnvColors) {
                Send(PacketWriter.MakeEnvSetColor(0, World.SkyColor));
                Send(PacketWriter.MakeEnvSetColor(1, World.CloudColor));
                Send(PacketWriter.MakeEnvSetColor(2, World.FogColor));
            }
            
            if (usesCPE)
            {
                if (!World.Hax)
                {
                    //Send(PacketWriter.MakeHackControl(1, 1, 1, 1, 1, -1)); Commented out until classicube clients support hax packet
                }

                //update bots
                foreach (Bot bot in Server.Bots)
                {
                    //remove old bots
                    Send(PacketWriter.MakeRemoveEntity(bot.ID));
                    if (bot.World == World)
                    {
                        //add back bot entity if world matches
                        Send(PacketWriter.MakeAddEntity(bot.ID, bot.Name, bot.Position));
                        if (bot.Model != "humanoid" && SupportsChangeModel)
                        {
                            Send(PacketWriter.MakeChangeModel((byte)bot.ID, bot.Model));
                        }
                        if (bot.Skin != "steve")
                        {
                            Send(PacketWriter.MakeExtAddEntity((byte)bot.ID, bot.Skin, bot.Skin));
                        }
                    }
                }
            }
            
            if (SupportsSelectionCuboid) {
                //update highlights
                if (oldWorld != null)
                {
                    foreach (var highlightsToRemove in oldWorld.Highlights)
                    {
                        Send(PacketWriter.RemoveSelectionCuboid((byte)highlightsToRemove.Value.Item1));
                    }
                }
                foreach(var entry in World.Highlights)
                {
                    Send(PacketWriter.MakeSelectionCuboid((byte)entry.Value.Item1, entry.Key, entry.Value.Item2, entry.Value.Item3, entry.Value.Item4, entry.Value.Item5));
                }
            }

            // Done.
            Server.RequestGC();

            return true;
        }

        #endregion


        #region Sending

        /// <summary> Send packet to player (not thread safe, sync, immediate).
        /// Should NEVER be used from any thread other than this session's ioThread.
        /// Not thread-safe (for performance reason). </summary>
        public void SendNow(Packet packet)
        {
            if (Thread.CurrentThread != ioThread)
            {
                throw new InvalidOperationException("SendNow may only be called from player's own thread.");
            }
            writer.Write(packet.Data);
            BytesSent += packet.Data.Length;
        }


        /// <summary> Send packet (thread-safe, async, priority queue).
        /// This is used for most packets (movement, chat, etc). </summary>
        public void Send(Packet packet)
        {
            if (canQueue) priorityOutputQueue.Enqueue(packet);
        }


        /// <summary> Send packet (thread-safe, asynchronous, delayed queue).
        /// This is currently only used for block updates. </summary>
        public void SendLowPriority(Packet packet)
        {
            if (canQueue) outputQueue.Enqueue(packet);
        }

        #endregion
        

        /// <summary> Clears the low priority player queue. </summary>
        void ClearLowPriotityOutputQueue()
        {
            outputQueue = new ConcurrentQueue<Packet>();
        }


        /// <summary> Clears the priority player queue. </summary>
        void ClearPriorityOutputQueue()
        {
            priorityOutputQueue = new ConcurrentQueue<Packet>();
        }


        #region Kicking

        /// <summary> Kick (asynchronous). Immediately blocks all client input, but waits
        /// until client thread has sent the kick packet. </summary>
        public void Kick([NotNull] string message, LeaveReason leaveReason)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (!Enum.IsDefined(typeof(LeaveReason), leaveReason))
            {
                throw new ArgumentOutOfRangeException("leaveReason");
            }
            State = SessionState.PendingDisconnect;
            LeaveReason = leaveReason;

            canReceive = false;
            canQueue = false;
            Info.tempDisplayedName = null;//If player happens to be in game or using /impersonate, reset temp name

            // clear all pending output to be written to client (it won't matter after the kick)
            ClearLowPriotityOutputQueue();
            ClearPriorityOutputQueue();

            // bypassing Send() because canQueue is false
            priorityOutputQueue.Enqueue(PacketWriter.MakeDisconnect(message));
        }


        /// <summary> Kick (synchronous). Immediately sends the kick packet.
        /// Can only be used from IoThread (this is not thread-safe). </summary>
        void KickNow([NotNull] string message, LeaveReason leaveReason)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (!Enum.IsDefined(typeof(LeaveReason), leaveReason))
            {
                throw new ArgumentOutOfRangeException("leaveReason");
            }
            if (Thread.CurrentThread != ioThread)
            {
                throw new InvalidOperationException("KickNow may only be called from player's own thread.");
            }
            Info.tempDisplayedName = null;
            State = SessionState.PendingDisconnect;
            LeaveReason = leaveReason;

            canQueue = false;
            canReceive = false;
            canSend = false;
            SendNow(PacketWriter.MakeDisconnect(message));
            writer.Flush();
        }


        /// <summary> Blocks the calling thread until this session disconnects. </summary>
        public void WaitForDisconnect()
        {
            if (Thread.CurrentThread == ioThread)
            {
                throw new InvalidOperationException("Cannot call WaitForDisconnect from IoThread.");
            }
            if (ioThread != null && ioThread.IsAlive)
            {
                try
                {
                    ioThread.Join();
                }
                catch (NullReferenceException)
                {
                }
                catch (ThreadStateException) { }
            }
        }

        #endregion


        #region Movement

        // visible entities
        readonly Dictionary<Player, VisibleEntity> entities = new Dictionary<Player, VisibleEntity>();
        readonly Stack<Player> playersToRemove = new Stack<Player>(127);
        readonly Stack<sbyte> freePlayerIDs = new Stack<sbyte>(127);

        // movement optimization
        int fullUpdateCounter;
        public const int FullPositionUpdateIntervalDefault = 20;
        public static int FullPositionUpdateInterval = FullPositionUpdateIntervalDefault;
        const int SkipMovementThresholdSquared = 64,
                  SkipRotationThresholdSquared = 1500;

        // anti-speedhack vars
        int speedHackDetectionCounter;
        const int AntiSpeedMaxJumpDelta = 25, // 16 for normal client, 25 for WoM
                  AntiSpeedMaxDistanceSquared = 1024, // 32 * 32
                  AntiSpeedMaxPacketCount = 200,
                  AntiSpeedMaxPacketInterval = 6;

        // anti-speedhack vars: packet spam
        readonly Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;


        void ResetVisibleEntities()
        {
            foreach (var pos in entities.Values)
            {
                SendNow(PacketWriter.MakeRemoveEntity(pos.Id));
            }
            freePlayerIDs.Clear();
            for (int i = 1; i <= sbyte.MaxValue; i++)
            {
                freePlayerIDs.Push((sbyte)i);
            }
            playersToRemove.Clear();
            entities.Clear();
        }


        void UpdateVisibleEntities()
        {
            if (World == null) PlayerOpException.ThrowNoWorld(this);
            if (possessionPlayer != null)
            {
                if (!possessionPlayer.IsOnline || possessionPlayer.IsSpectating)
                {
                    Message("You have been released from possession");
                    possessionPlayer = null;
                }
                else
                {
                    Position sendTo = possessionPlayer.Position;
                    World possessedWorld = possessionPlayer.World;
                    if (possessedWorld == null)
                    {
                        throw new InvalidOperationException("Possess: Something weird just happened (error 404).");
                    }
                    if (possessedWorld != World)
                    {
                        if (CanJoin(possessedWorld))
                        {
                            postJoinPosition = sendTo;
                            Message("Joined {0}&S (possessed)",
                                     possessedWorld.ClassyName);
                            JoinWorldNow(possessedWorld, false, WorldChangeReason.SpectateTargetJoined);
                        }
                        else
                        {
                            possessionPlayer.Message("Stopped possessing {0}&S (they cannot join {1}&S)",
                                      ClassyName,
                                      possessedWorld.ClassyName);
                            possessionPlayer = null;
                        }
                    }
                    else if (sendTo != Position)
                    {
                        SendNow(PacketWriter.MakeSelfTeleport(sendTo));
                    }
                }
            }
            if (spectatedPlayer != null)
            {
                if (!spectatedPlayer.IsOnline || !CanSee(spectatedPlayer))
                {
                    Message("Stopped spectating {0}&S (disconnected)", spectatedPlayer.ClassyName);
                    spectatedPlayer = null;
                }
                else
                {
                    Position spectatePos = spectatedPlayer.Position;
                    World spectateWorld = spectatedPlayer.World;
                    if (spectateWorld == null)
                    {
                        throw new InvalidOperationException("Trying to spectate player without a world.");
                    }
                    if (spectateWorld != World)
                    {
                        if (CanJoin(spectateWorld))
                        {
                            postJoinPosition = spectatePos;
                            Message("Joined {0}&S to continue spectating {1}",
                                     spectateWorld.ClassyName,
                                     spectatedPlayer.ClassyName);
                            JoinWorldNow(spectateWorld, false, WorldChangeReason.SpectateTargetJoined);
                        }
                        else
                        {
                            Message("Stopped spectating {0}&S (cannot join {1}&S)",
                                     spectatedPlayer.ClassyName,
                                     spectateWorld.ClassyName);
                            spectatedPlayer = null;
                        }
                    }
                    else if (spectatePos != Position)
                    {
                        SendNow(PacketWriter.MakeSelfTeleport(spectatePos));
                    }
                }
            }

            Position pos = Position;

            //Loop through all players in the world
            foreach(Player otherPlayer in World.Players)
            {
                if (otherPlayer == this ||
                    !CanSeeMoving(otherPlayer) ||
                    spectatedPlayer == otherPlayer || possessionPlayer != null) continue;

                Position otherPos = otherPlayer.Position;
                int distance = pos.DistanceSquaredTo(otherPos);

                VisibleEntity entity;
                // if Player has a corresponding VisibleEntity
                if (entities.TryGetValue(otherPlayer, out entity))
                {
                    entity.MarkedForRetention = true;
                   
                    if (SupportsChangeModel && entity.LastKnownModel != otherPlayer.Model) 
                    {
                        SendNow(PacketWriter.MakeChangeModel((byte)entity.Id, otherPlayer.Model));
                        entity.LastKnownModel = otherPlayer.Model;
                    }
                    
                    if (entity.LastKnownRank != otherPlayer.Info.Rank)
                    {
                        ReAddEntity(entity, otherPlayer, otherPos);
                        entity.LastKnownRank = otherPlayer.Info.Rank;
                    }
                    //if entity changed for another player, add this player to the list pf players who have seen the update
                    if( otherPlayer.entityChanged && !otherPlayer.playersWhoHaveSeenEntityChanges.Contains(this))
                    {
                        ReAddEntity(entity, otherPlayer, otherPos);
                        otherPlayer.playersWhoHaveSeenEntityChanges.Add(this);
                    }
                    //if this player is already in the list (has updated the skin), continue
                    //if the player whose skin changed has updated for everyone, clear the list and set entityChanged to false
                    else if (otherPlayer.playersWhoHaveSeenEntityChanges.Count() == (World.Players.Count() - 1))
                    {
                        otherPlayer.playersWhoHaveSeenEntityChanges.Clear();
                        otherPlayer.entityChanged = false;
                    }
                    else if (entity.Hidden)
                    {
                        if (distance < entityShowingThreshold)
                        {
                            ShowEntity(entity, otherPos);
                        }

                    }
                    else
                    {
                        if (distance > entityHidingThreshold)
                        {
                            HideEntity(entity);

                        }
                        else if (entity.LastKnownPosition != otherPos)
                        {
                            MoveEntity(entity, otherPos);
                        }
                    }
                }
                else
                {
                    AddEntity(otherPlayer, otherPos);
                }
            }


            // Find entities to remove (not marked for retention).
            foreach (var pair in entities)
            {
                if (pair.Value.MarkedForRetention)
                {
                    pair.Value.MarkedForRetention = false;
                }
                else
                {
                    playersToRemove.Push(pair.Key);
                }
            }

            // Remove non-retained entities
            while (playersToRemove.Count > 0)
            {
                RemoveEntity(playersToRemove.Pop());
            }

            fullUpdateCounter++;
            if (fullUpdateCounter >= FullPositionUpdateInterval)
            {
                fullUpdateCounter = 0;
            }
        }


        void AddEntity([NotNull] Player player, Position newPos)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (freePlayerIDs.Count > 0)
            {
                var entity = new VisibleEntity(newPos, freePlayerIDs.Pop(), player.Info.Rank, player.Model);
                entities.Add(player, entity);
                SendNow(PacketWriter.MakeAddEntity(entity.Id, player.Info.Rank.Color + player.Skinname, newPos));
                
                if (usesCPE) {
                    SendNow(PacketWriter.MakeExtAddEntity((byte)entity.Id, player.ListName, player.Skinname));
                    SendNow(PacketWriter.MakeTeleport(entity.Id, newPos));
                    
                }
                if (SupportsChangeModel) {
                    SendNow(PacketWriter.MakeChangeModel((byte)entity.Id, player.Model));
                }
            }
        }


        void HideEntity([NotNull] VisibleEntity entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            entity.Hidden = true;
            entity.LastKnownPosition = VisibleEntity.HiddenPosition;
            SendNow(PacketWriter.MakeTeleport(entity.Id, VisibleEntity.HiddenPosition));
        }


        void ShowEntity([NotNull] VisibleEntity entity, Position newPos)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            entity.Hidden = false;
            entity.LastKnownPosition = newPos;
            SendNow(PacketWriter.MakeTeleport(entity.Id, newPos));
        }


        void ReAddEntity([NotNull] VisibleEntity entity, [NotNull] Player player, Position newPos)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            if (player == null) throw new ArgumentNullException("player");
            SendNow(PacketWriter.MakeRemoveEntity(entity.Id));
            if (usesCPE)
                SendNow(PacketWriter.MakeExtRemovePlayerName((short)entity.Id));
            /*if (usesCPE)
            {
                if (player.iName == null)
                    SendNow(PacketWriter.MakeExtAddEntity((byte)entities[player].Id, player.Skinname, player.Name));
                else
                    SendNow(PacketWriter.MakeExtAddEntity((byte)entities[player].Id, player.Skinname, player.iName));
               
            }*/
            SendNow(PacketWriter.MakeAddEntity(entity.Id, player.Info.Rank.Color + player.Skinname, newPos));
            if (usesCPE) {
                SendNow(PacketWriter.MakeExtAddEntity((byte)entity.Id, player.ListName, player.Skinname));
                SendNow(PacketWriter.MakeTeleport(entities[player].Id, newPos));
            }
            entity.LastKnownPosition = newPos;
        }
        

        void RemoveEntity([NotNull] Player player)
        {
            if (player == null) throw new ArgumentNullException("player");
            SendNow(PacketWriter.MakeRemoveEntity(entities[player].Id));
            freePlayerIDs.Push(entities[player].Id);
            entities.Remove(player);
        }


        void MoveEntity([NotNull] VisibleEntity entity, Position newPos)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            Position oldPos = entity.LastKnownPosition;

            // calculate difference between old and new positions
            Position delta = new Position
            {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs(newPos.R - oldPos.R),
                L = (byte)Math.Abs(newPos.L - oldPos.L)
            };

            bool posChanged = (delta.X != 0) || (delta.Y != 0) || (delta.Z != 0);
            bool rotChanged = (delta.R != 0) || (delta.L != 0);

            if (skipUpdates)
            {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // movement optimization
                if (distSquared < SkipMovementThresholdSquared &&
                    (delta.R * delta.R + delta.L * delta.L) < SkipRotationThresholdSquared &&
                    !entity.SkippedLastMove)
                {

                    entity.SkippedLastMove = true;
                    return;
                }
                entity.SkippedLastMove = false;
            }

            Packet packet;
            // create the movement packet
            if (partialUpdates && delta.FitsIntoMoveRotatePacket && fullUpdateCounter < FullPositionUpdateInterval)
            {
                if (posChanged && rotChanged)
                {
                    // incremental position + rotation update
                    packet = PacketWriter.MakeMoveRotate(entity.Id, new Position
                    {
                        X = delta.X,
                        Y = delta.Y,
                        Z = delta.Z,
                        R = newPos.R,
                        L = newPos.L
                    });

                }
                else if (posChanged)
                {
                    // incremental position update
                    packet = PacketWriter.MakeMove(entity.Id, delta);

                }
                else if (rotChanged)
                {
                    // absolute rotation update
                    packet = PacketWriter.MakeRotate(entity.Id, newPos);
                }
                else
                {
                    return;
                }

            }
            else
            {
                // full (absolute position + rotation) update
                packet = PacketWriter.MakeTeleport(entity.Id, newPos);
            }

            entity.LastKnownPosition = newPos;
            SendNow(packet);
        }


        sealed class VisibleEntity
        {
            public static readonly Position HiddenPosition = new Position(0, 0, short.MinValue);

            public VisibleEntity(Position newPos, sbyte newId, Rank newRank, string newModel)
            {
                Id = newId;
                LastKnownPosition = newPos;
                MarkedForRetention = true;
                Hidden = true;
                LastKnownRank = newRank;
                LastKnownModel = newModel;
            }

            public readonly sbyte Id;
            public Position LastKnownPosition;
            public Rank LastKnownRank;
            public string LastKnownModel;
            public bool Hidden;
            public bool MarkedForRetention;
            public bool SkippedLastMove;
        }


        Position lastValidPosition; // used in speedhack detection

        bool DetectMovementPacketSpam()
        {
            if (antiSpeedPacketLog.Count >= AntiSpeedMaxPacketCount)
            {
                DateTime oldestTime = antiSpeedPacketLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract(oldestTime).TotalSeconds;
                if (spamTimer < AntiSpeedMaxPacketInterval)
                {
                    DenyMovement();
                    return true;
                }
            }
            antiSpeedPacketLog.Enqueue(DateTime.UtcNow);
            return false;
        }


        void DenyMovement()
        {
            SendNow(PacketWriter.MakeSelfTeleport(lastValidPosition));
            if (DateTime.UtcNow.Subtract(antiSpeedLastNotification).Seconds > 1)
            {
                Message("&WYou are not allowed to speedhack.");
                antiSpeedLastNotification = DateTime.UtcNow;
            }
        }

        #endregion


        #region Bandwidth Use Tweaks

        BandwidthUseMode bandwidthUseMode;
        int entityShowingThreshold, entityHidingThreshold;
        bool partialUpdates, skipUpdates;

        DateTime lastAutorankCheck;
        DateTime lastMovementUpdate;
        TimeSpan movementUpdateInterval;


        public BandwidthUseMode BandwidthUseMode
        {
            get
            {
                return bandwidthUseMode;
            }

            set
            {
                bandwidthUseMode = value;
                BandwidthUseMode actualValue = value;
                if (value == BandwidthUseMode.Default)
                {
                    actualValue = ConfigKey.BandwidthUseMode.GetEnum<BandwidthUseMode>();
                }
                switch (actualValue)
                {
                    case BandwidthUseMode.VeryLow:
                        entityShowingThreshold = (40 * 32) * (40 * 32);
                        entityHidingThreshold = (42 * 32) * (42 * 32);
                        partialUpdates = true;
                        skipUpdates = true;
                        movementUpdateInterval = TimeSpan.FromMilliseconds(100);
                        break;

                    case BandwidthUseMode.Low:
                        entityShowingThreshold = (50 * 32) * (50 * 32);
                        entityHidingThreshold = (52 * 32) * (52 * 32);
                        partialUpdates = true;
                        skipUpdates = true;
                        movementUpdateInterval = TimeSpan.FromMilliseconds(50);
                        break;

                    case BandwidthUseMode.Normal:
                        entityShowingThreshold = (68 * 32) * (68 * 32);
                        entityHidingThreshold = (70 * 32) * (70 * 32);
                        partialUpdates = true;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds(50);
                        break;

                    case BandwidthUseMode.High:
                        entityShowingThreshold = (128 * 32) * (128 * 32);
                        entityHidingThreshold = (130 * 32) * (130 * 32);
                        partialUpdates = true;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds(50);
                        break;

                    case BandwidthUseMode.VeryHigh:
                        entityShowingThreshold = int.MaxValue;
                        entityHidingThreshold = int.MaxValue;
                        partialUpdates = false;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds(25);
                        break;
                }
            }
        }

        #endregion


        #region Bandwidth Use Metering

        DateTime lastMeasurementDate = DateTime.UtcNow;
        int lastBytesSent, lastBytesReceived;


        /// <summary> Total bytes sent (to the client) this session. </summary>
        public int BytesSent { get; private set; }

        /// <summary> Total bytes received (from the client) this session. </summary>
        public int BytesReceived { get; private set; }

        /// <summary> Bytes sent (to the client) per second, averaged over the last several seconds. </summary>
        public double BytesSentRate { get; private set; }

        /// <summary> Bytes received (from the client) per second, averaged over the last several seconds. </summary>
        public double BytesReceivedRate { get; private set; }


        void MeasureBandwidthUseRates()
        {
            int sentDelta = BytesSent - lastBytesSent;
            int receivedDelta = BytesReceived - lastBytesReceived;
            TimeSpan timeDelta = DateTime.UtcNow.Subtract(lastMeasurementDate);
            BytesSentRate = sentDelta / timeDelta.TotalSeconds;
            BytesReceivedRate = receivedDelta / timeDelta.TotalSeconds;
            lastBytesSent = BytesSent;
            lastBytesReceived = BytesReceived;
            lastMeasurementDate = DateTime.UtcNow;
        }

        #endregion
    }
}

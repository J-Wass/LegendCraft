/* Copyright (c) <2014> <LeChosenOne, DingusBungus>
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

//Todo: stats

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using fCraft.Events;

namespace fCraft
{
    class CTF
    {
        //Team Tags
        public const string redTeam = "&c-Red-";
        public const string blueTeam = "&1*Blue*";

        //CTF stats
        public static int blueScore = 0;
        public static int redScore = 0;
        public static int redTeamCount = 0;
        public static int blueTeamCount = 0;

        //Timing
        public static int timeLeft = 0;
        private static SchedulerTask task_;
        public static SchedulerTask delayTask;
        public static CTF instance;
        public static DateTime startTime;
        public static DateTime lastChecked;
        public static int timeLimit = 300;
        public static int timeDelay = 20;
        public static int totalTime = timeLimit + timeDelay;
        public static int scoreCap = 5;
        public static Stopwatch stopwatch = new Stopwatch();
        public static DateTime announced = DateTime.MaxValue;
        public static DateTime RedDisarmed = DateTime.MaxValue;
        public static DateTime BlueDisarmed = DateTime.MaxValue;
        public static DateTime RedBOFdebuff = DateTime.MaxValue;
        public static DateTime BlueBOFdebuff = DateTime.MaxValue;

        //Game Bools
        public static bool isOn = false;
        private static bool started = false;
        public static string blueFlagHolder;
        public static string redFlagHolder;

        private static World world_;

        public static CTF GetInstance(World world)
        {
            if (instance == null)
            {
                world_ = world;
                instance = new CTF();
                startTime = DateTime.Now;
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromMilliseconds(250)); //run loop every quarter second
            }
            return instance;
        }

        public static void Start()
        {
            world_.Hax = false;

            //world_.Players.Send(PacketWriter.MakeHackControl(0,0,0,0,0,-1)); Commented out until classicube clients support hax packet
            stopwatch.Reset();
            stopwatch.Start();
            world_.gameMode = GameMode.CaptureTheFlag;
            delayTask = Scheduler.NewTask(t => world_.Players.Message("&WCTF &fwill be starting in {0} seconds: &WGet ready!", (timeDelay - stopwatch.Elapsed.Seconds)));
            delayTask.RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), (int)Math.Floor((double)(timeDelay / 10)));//Start task immediately, send message every 10s
            if (stopwatch.Elapsed.Seconds > 11)
            {
                stopwatch.Stop();
            }
        }

        public static void Stop(Player p) //for stopping the game early
        {

            //unhook moving event
            Player.Moving -= PlayerMoving;

            world_.Hax = true;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }

            if (p != null && world_ != null)
            {
                world_.Players.Message("{0}&S stopped the game of CTF early on world {1}", p.ClassyName, world_.ClassyName);               
            }
            RevertGame();

            if (!delayTask.IsStopped)//if stop is called when the delayTask is still going, stop the delayTask
            {
                delayTask.Stop();
            }
            return;
        }

        #region MainInterval
        public static void Interval(SchedulerTask task)
        {
            //check to stop Interval
            if (world_ == null)
            {
                task.Stop();
                return;
            }
            if (world_.gameMode != GameMode.CaptureTheFlag)
            {
                task.Stop();
                world_ = null;
                return;
            }

            //remove announcements after 5 seconds
            if (announced != DateTime.MaxValue && (DateTime.Now - announced).TotalSeconds >= 5)
            {
                foreach (Player p in world_.Players)
                {
                    if (p.usesCPE && Heartbeat.ClassiCube())
                    {
                        p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));//super hacky way to remove announcements, simply send a color code and call it a day
                    }
                }
                announced = DateTime.MaxValue;
            }

            //remove dodge after 1m
            foreach (Player p in world_.Players)
            {
                if (p.Info.canDodge)
                {
                    if (p.Info.dodgeTime != DateTime.MaxValue && (DateTime.Now - p.Info.dodgeTime).TotalSeconds >= 60)
                    {
                        p.Info.canDodge = false;
                        p.Info.dodgeTime = DateTime.MaxValue;

                        world_.Players.Message(p.Name + " is no longer able to dodge.");
                    }
                }
            }

            //remove strengthen after 1m
            foreach (Player p in world_.Players)
            {
                if (p.Info.strengthened)
                {
                    if (p.Info.strengthTime != DateTime.MaxValue && (DateTime.Now - p.Info.strengthTime).TotalSeconds >= 60)
                    {
                        p.Info.strengthened = false;
                        p.Info.strengthTime = DateTime.MaxValue;

                        world_.Players.Message(p.Name + " is no longer dealing 2x damage.");
                    }
                }
            }

            //remove Blades of Fury after 1m
            if ((BlueBOFdebuff != DateTime.MaxValue && (DateTime.Now - BlueBOFdebuff).TotalSeconds >= 60))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.CTFBlueTeam)
                    {
                        p.Info.stabDisarmed = false;
                    }
                    else
                    {
                        p.Info.stabAnywhere = false;
                    }
                }
                BlueBOFdebuff = DateTime.MaxValue;

                world_.Players.Message("Blades of Fury has ended.");
            }

            if ((RedBOFdebuff != DateTime.MaxValue && (DateTime.Now - RedBOFdebuff).TotalSeconds >= 60))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.CTFRedTeam)
                    {
                        p.Info.stabDisarmed = false;
                    }
                    else
                    {
                        p.Info.stabAnywhere = false;
                    }
                }
                RedBOFdebuff = DateTime.MaxValue;

                world_.Players.Message("Blades of Fury has ended.");
            }

            //remove disarm after 30s
            if ((RedDisarmed != DateTime.MaxValue && (DateTime.Now - RedDisarmed).TotalSeconds >= 30))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.CTFRedTeam)
                    {
                        p.GunMode = true;
                        p.Info.gunDisarmed = false;
                    }
                }
                RedDisarmed = DateTime.MaxValue;

                world_.Players.Message("The Disarm Spell has ended.");
            }

            if ((BlueDisarmed != DateTime.MaxValue && (DateTime.Now - BlueDisarmed).TotalSeconds >= 30))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.CTFBlueTeam)
                    {
                        p.GunMode = true;
                        p.Info.gunDisarmed = false;
                    }
                }
                BlueDisarmed = DateTime.MaxValue;

                world_.Players.Message("The Disarm Spell has ended.");
            }

            if (!started)
            {
                //create a player moving event
                Player.Moving += PlayerMoving;

                if (world_.Players.Count() < 2) //in case players leave the world or disconnect during the start delay
                {
                    world_.Players.Message("&WCTF&s requires at least 2 people to play.");
                    return;
                }

                //once timedelay is up, we start
                if (startTime != null && (DateTime.Now - startTime).TotalSeconds > timeDelay)
                {
                    if (!world_.gunPhysics)
                    {
                        world_.EnableGunPhysics(Player.Console, true); //enables gun physics if they are not already on
                    }
                    foreach (Player p in world_.Players)
                    {
                        if (p.usesCPE && Heartbeat.ClassiCube())
                        {
                            //loop through each block ID
                            for (int i = 1; i < 65; i++)
                            {
                                //allow player to break glass block in order to shoot gun, disallow all other blocks except flags
                                if (i.Equals(20))
                                {
                                    p.Send(PacketWriter.MakeSetBlockPermissions((byte)20, false, true));
                                }
                                else if (i.Equals(21))
                                {
                                    p.Send(PacketWriter.MakeSetBlockPermissions((byte)21, false, true));
                                }
                                else if (i.Equals(29))
                                {
                                    p.Send(PacketWriter.MakeSetBlockPermissions((byte)29, false, true));
                                }
                                else
                                {
                                    p.Send(PacketWriter.MakeSetBlockPermissions((byte)i, false, false));
                                }
                            }
                        }

                        assignTeams(p);

                        if (p.Info.IsHidden) //unhides players automatically if hidden (cannot shoot guns while hidden)
                        {
                            p.Info.IsHidden = false;
                            Player.RaisePlayerHideChangedEvent(p);
                        }

                        if (p.Info.CTFRedTeam)
                        {
                            p.TeleportTo(world_.redCTFSpawn.ToPlayerCoords());
                        }

                        if (p.Info.CTFBlueTeam)
                        {
                            p.TeleportTo(world_.blueCTFSpawn.ToPlayerCoords());
                        }

                        p.GunMode = true;
                        GunGlassTimer timer = new GunGlassTimer(p);
                        timer.Start();

                        //send an announcement (Will be sent as a normal message to non classicube players)
                        p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&cLet the Games Begin!"));

                        if (p.usesCPE && Heartbeat.ClassiCube())
                        {
                            //set player health
                            p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f[&a--------&f]"));

                            //set game score
                            p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: 0,&1 Blue&f: 0"));
                        }
                    }

                    //check that the flags haven't been misplaced during startup
                    if (world_.Map.GetBlock(world_.redFlag) != Block.Red)
                    {
                        world_.Map.QueueUpdate(new BlockUpdate(null, world_.redFlag, Block.Red));
                    }

                    if (world_.Map.GetBlock(world_.blueFlag) != Block.Blue)
                    {
                        world_.Map.QueueUpdate(new BlockUpdate(null, world_.blueFlag, Block.Blue));
                    }

                    started = true;   //the game has officially started
                    isOn = true;
                    lastChecked = DateTime.Now;     //used for intervals
                    announced = DateTime.Now;
                    return;
                }
            }

            //update blue team and red team counts
            redTeamCount =
            (
                from red in world_.Players
                where red.Info.CTFRedTeam
                select red
            ).Count();

            blueTeamCount =
            (
                from blue in world_.Players
                where blue.Info.CTFBlueTeam
                select blue
            ).Count();



            //Announce flag holder
            if (String.IsNullOrEmpty(redFlagHolder))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.hasRedFlag && redFlagHolder == null)
                    {
                        world_.Players.Message(p.Name + " has stolen the Red flag!");
                        redFlagHolder = p.Name;
                    }
                }
            }

            //update flagholder
            else
            {
                redFlagHolder = null;
                foreach(Player p in world_.Players)
                {
                    if (p.Info.hasRedFlag)
                    {
                        redFlagHolder = p.Name;
                    }
                }
            }

            if (String.IsNullOrEmpty(blueFlagHolder))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.hasBlueFlag && blueFlagHolder == null)
                    {
                        world_.Players.Message(p.Name + " has stolen the Blue flag!");
                        blueFlagHolder = p.Name;
                    }
                }
            }

            //update flagholder
            else
            {
                blueFlagHolder = null;
                foreach (Player p in world_.Players)
                {
                    if (p.Info.hasBlueFlag)
                    {
                        blueFlagHolder = p.Name;
                    }
                }
            }

            //Check victory conditions
            if (blueScore == 5)
            {
                world_.Players.Message("&fThe blue team has won {0} to {1}!", blueScore, redScore);
                Stop(null);
                return;
            }

            if (redScore == 5)
            {
                world_.Players.Message("&fThe red team has won {1} to {0}!", blueScore, redScore);
                Stop(null);
                return;
            }

            //if time is up
            if (started && startTime != null && (DateTime.Now - startTime).TotalSeconds >= (totalTime))
            {
                if (redScore > blueScore)
                {
                    world_.Players.Message("&fThe &cRed&f Team won {0} to {1}!", redScore, blueScore);
                    Stop(null);
                    return;
                }
                if (redScore < blueScore)
                {
                    world_.Players.Message("&fThe &1Blue&f Team won {0} to {1}!", blueScore, redScore);
                    Stop(null);
                    return;
                }
                if (redScore == blueScore)
                {
                    world_.Players.Message("&fThe teams tied {0} to {0}!", blueScore);
                    Stop(null);
                    return;
                }
                if (world_.Players.Count() <= 1)
                {
                    Stop(null);
                    return;
                }
            }

            //Check for forfeits
            if (started && (DateTime.Now - lastChecked).TotalSeconds > 10)
            {
                if (blueTeamCount < 1 || redTeamCount < 1)
                {
                    if (blueTeamCount == 0)
                    {
                        if (world_.Players.Count() >= 1)
                        {
                            world_.Players.Message("&1Blue Team &fhas forfeited the game. &cRed Team &fwins!");
                        }
                        Stop(null);
                        return;
                    }
                    if (redTeamCount == 0)
                    {
                        if (world_.Players.Count() >= 1)
                        {
                            world_.Players.Message("&cRed Team &fhas forfeited the game. &1Blue Team &fwins!");
                        }
                        Stop(null);
                        return;
                    }
                    //lol, everyone left
                    else
                    {
                        Stop(null);
                        return;
                    }
                }
            }

            timeLeft = Convert.ToInt16(((timeDelay + timeLimit) - (DateTime.Now - startTime).TotalSeconds));
            //Keep the players updated about the score
            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.8 && timeLeft <= timeLimit)
            {
                if (redScore > blueScore)
                {
                    world_.Players.Message("&fThe &cRed Team&f is winning {0} to {1}.", redScore, blueScore);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                if (redScore < blueScore)
                {
                    world_.Players.Message("&fThe &1Blue Team&f is winning {0} to {1}.", blueScore, redScore);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                if (redScore == blueScore)
                {
                    world_.Players.Message("&fThe teams are tied at {0}!", blueScore);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                lastChecked = DateTime.Now;
            }
            if (timeLeft == 10)
            {
                world_.Players.Message("&WOnly 10 seconds left!");
            }
        }
        #endregion

        #region SeperateVoids
        static public void assignTeams(Player p)    //Assigns teams to all players in the world
        {
            //if there are no players assigned to any team yet
            if (redTeamCount == 0) { AssignRed(p); return; }
            //if the red team has more players and the red team has already been assigned at least one player
            if (blueTeamCount < redTeamCount) { AssignBlue(p); return; }
            //if the teams have the same number of players, by default the next player will be assigned to the red team
            if (blueTeamCount == redTeamCount) { AssignRed(p); return; }
        }

        public static void RevertGame() //Reset game bools/stats and stop timers
        {
            task_.Stop();
            world_.gameMode = GameMode.NULL;
            isOn = false;
            instance = null;
            started = false;
            if (world_.gunPhysics)
            {
                world_.DisableGunPhysics(Player.Console, true);
            }
            world_ = null;
            redScore = 0;
            blueScore = 0;
            redTeamCount = 0;
            blueTeamCount = 0;
            RevertNames();
        }       

        public static void RevertNames()    //reverts names and vars for online players. offline players get reverted upon leaving the game
        {
            List<PlayerInfo> CTFPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => (r.CTFBlueTeam || r.CTFRedTeam) && r.IsOnline).ToArray());
            for (int i = 0; i < CTFPlayers.Count(); i++)
            {
                string p1 = CTFPlayers[i].Name.ToString();
                PlayerInfo pI = PlayerDB.FindPlayerInfoExact(p1);
                Player p = pI.PlayerObject;

                if (p != null)
                {
                    p.iName = null;
                    pI.tempDisplayedName = null;
                    pI.CTFBlueTeam = false;
                    pI.CTFRedTeam = false;
                    pI.isPlayingCTF = false;
                    pI.placingBlueFlag = false;
                    pI.placingRedFlag = false;
                    pI.hasRedFlag = false;
                    pI.hasBlueFlag = false;
                    pI.CTFCaptures = 0;
                    pI.CTFKills = 0;
                    p.entityChanged = true;

                    //reset all special messages
                    if (p.usesCPE && Heartbeat.ClassiCube())
                    {
                        p.Send(PacketWriter.MakeSetBlockPermissions((byte)0, true, true));
                        p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));
                        p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f"));
                        p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&f"));
                    }

                    //undo gunmode (taken from GunHandler.cs)
                    p.GunMode = false;
                    try
                    {
                        foreach (Vector3I block in p.GunCache.Values)
                        {
                            p.Send(PacketWriter.MakeSetBlock(block.X, block.Y, block.Z, p.WorldMap.GetBlock(block)));
                            Vector3I removed;
                            p.GunCache.TryRemove(block.ToString(), out removed);
                        }
                        if (p.bluePortal.Count > 0)
                        {
                            int j = 0;
                            foreach (Vector3I block in p.bluePortal)
                            {
                                if (p.WorldMap != null && p.World.IsLoaded)
                                {
                                    p.WorldMap.QueueUpdate(new BlockUpdate(null, block, p.blueOld[j]));
                                    j++;
                                }
                            }
                            p.blueOld.Clear();
                            p.bluePortal.Clear();
                        }
                        if (p.orangePortal.Count > 0)
                        {
                            int j = 0;
                            foreach (Vector3I block in p.orangePortal)
                            {
                                if (p.WorldMap != null && p.World.IsLoaded)
                                {
                                    p.WorldMap.QueueUpdate(new BlockUpdate(null, block, p.orangeOld[j]));
                                    j++;
                                }
                            }
                            p.orangeOld.Clear();
                            p.orangePortal.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.SeriousError, "" + ex);
                    }
                    if (p.IsOnline)
                    {
                        p.Message("&aYour status has been reverted.");
                    }
                }
            }
        }

        public static void AssignRed(Player p)
        {
            p.Message("You are on the &cRed Team");
            p.iName = "TeamRed";
            p.Info.tempDisplayedName = "&f(" + redTeam + "&f) " + Color.Red + p.Name;
            p.Info.CTFRedTeam = true;
            p.Info.CTFBlueTeam = false;
            p.Info.isPlayingCTF = true;
            p.entityChanged = true;
            p.Info.CTFKills = 0;
            redTeamCount++;
            return;
        }
        public static void AssignBlue(Player p)
        {
            p.Message("You are on the &9Blue Team");
            p.iName = "TeamBlue";
            p.Info.tempDisplayedName = "&f(" + blueTeam + "&f) " + Color.Navy + p.Name;
            p.Info.CTFBlueTeam = true;
            p.Info.CTFRedTeam = false;
            p.Info.isPlayingCTF = true;
            p.entityChanged = true;
            blueTeamCount++;
            return;
        }
        #endregion

        #region PowerUps
        public static void PowerUp(Player p)
        {
            int GetPowerUp = (new Random()).Next(1, 4);
            if (GetPowerUp < 3)
            {
                return;
            }

            int choosePowerUp = (new Random()).Next(1, 19);

            //decide which powerup to use, certain powerups have a higher chance such as first aid kit and dodge as opposed to rarer ones like holy blessing
            switch (choosePowerUp)
            {
                case 1:
                case 2:
                case 3:
                    //first aid kit - heal user for 50 hp
                    world_.Players.Message("&f{0} has discovered a &aFirst Aid Kit&f!", p.Name);
                    world_.Players.Message("&f{0} has been healed for 50 hp.", p.Name);

                    //set health to 100, make sure it doesn't overflow
                    p.Info.Health += 50;
                    if (p.Info.Health > 100)
                    {
                        p.Info.Health = 100;
                    }

                    string healthBar = "&f[&a--------&f]";
                    if (p.Info.Health == 75)
                    {
                        healthBar = "&f[&a------&8--&f]";
                    }
                    else if (p.Info.Health == 50)
                    {
                        healthBar = "&f[&e----&8----&f]";
                    }
                    else if (p.Info.Health == 25)
                    {
                        healthBar = "&f[&c--&8------&f]";
                    }
                    else
                    {
                        healthBar = "&f[&8--------&f]";
                    }

                    if (p.usesCPE && Heartbeat.ClassiCube())
                    {
                        p.Send(PacketWriter.MakeSpecialMessage((byte)1, healthBar));
                    }
                    else
                    {
                        p.Message("You have " + p.Info.Health.ToString() + " health.");
                    }

                    break;
                case 4:
                case 5:
                    //penicillin - heal user for 100 hp
                    world_.Players.Message("&f{0} has discovered a &aPenicillin Case&f!", p.Name);
                    world_.Players.Message("&f{0} has been healed for 100 hp.", p.Name);
                    p.Info.Health = 100;

                    if (p.usesCPE && Heartbeat.ClassiCube())
                    {
                        p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f[&8--------&f]"));
                    }
                    else
                    {
                        p.Message("You have " + p.Info.Health.ToString() + " health.");
                    }

                    break;
                case 6:
                case 7:
                    //disarm
                    world_.Players.Message("&f{0} has discovered a &aDisarm Spell&f!", p.Name);
                    if (p.Info.CTFBlueTeam)
                    {
                        world_.Players.Message("The red team has lost all weaponry for 30 seconds!");
                        foreach (Player pl in world_.Players)
                        {
                            if (pl.Info.CTFRedTeam)
                            {
                                pl.Info.gunDisarmed = true;
                                pl.Info.stabDisarmed = true;
                                pl.GunMode = false;                             
                            }
                        }
                        RedDisarmed = DateTime.Now;
                    }
                    else
                    {
                        world_.Players.Message("The blue team has lost all weaponry for 30 seconds!");
                        foreach (Player pl in world_.Players)
                        {
                            if (pl.Info.CTFBlueTeam)
                            {
                                pl.Info.gunDisarmed = true;
                                pl.Info.stabDisarmed = true;
                                pl.GunMode = false;
                            }
                        }
                        BlueDisarmed = DateTime.Now;
                    }

                    break;
                case 8:
                case 9:
                    //blades of fury
                    world_.Players.Message("&f{0} has discovered the &aBlades of Fury&f!", p.Name);
                    if (p.Info.CTFBlueTeam)
                    {
                        world_.Players.Message("The red team is unable to backstab for 1 minute!");
                        world_.Players.Message("The blue team can now stab the red team from any angle for 1 minute!");
                        foreach (Player pl in world_.Players)
                        {
                            if (pl.Info.CTFBlueTeam)
                            {
                                pl.Info.stabAnywhere = true;
                            }
                            else
                            {
                                pl.Info.stabDisarmed = true;
                            }
                        }
                        RedBOFdebuff = DateTime.Now;
                    }
                    else
                    {
                        world_.Players.Message("The blue team is unable to backstab for 1 minute!");
                        world_.Players.Message("The red team can now stab the blue team from any angle for 1 minute!");
                        foreach (Player pl in world_.Players)
                        {
                            if (pl.Info.CTFRedTeam)
                            {
                                pl.Info.stabAnywhere = true;
                            }
                            else
                            {
                                pl.Info.stabDisarmed = true;
                            }
                        }
                        RedBOFdebuff = DateTime.Now;
                    }
                    break;
                case 10:
                case 11:
                    //war cry
                    world_.Players.Message("&f{0} has discovered their &aWar Cry&f!", p.Name);
                    if (p.Info.CTFBlueTeam)
                    {
                        world_.Players.Message("The red team has been frightened back into their spawn!");
                        foreach (Player pl in world_.Players)
                        {
                            if (pl.Info.CTFRedTeam)
                            {
                                pl.TeleportTo(world_.redCTFSpawn.ToPlayerCoords());
                            }
                        }
                    }
                    else
                    {
                        world_.Players.Message("The blue team has been frightened back into their spawn!");
                        foreach (Player pl in world_.Players)
                        {
                            if (pl.Info.CTFBlueTeam)
                            {
                                pl.TeleportTo(world_.blueCTFSpawn.ToPlayerCoords());
                            }
                        }
                    }
                    break;
                case 12:
                case 13:
                case 14:
                    //strengthen
                    world_.Players.Message("&f{0} has discovered a &aStrength Pack&f!", p.Name);
                    world_.Players.Message("&f{0}'s gun now deals twice the damage for the next minute!", p.Name);
                    p.Info.strengthened = true;
                    p.Info.strengthTime = DateTime.Now;
                    break;
                case 15:
                case 16:
                case 17:
                    //dodge
                    world_.Players.Message("&f{0} has discovered a new &aDodging Technique&f!", p.Name);
                    world_.Players.Message("&f{0}'s has a 50% chance to dodge incomming gun attacks for the next minute!", p.Name);
                    p.Info.canDodge = true;
                    p.Info.dodgeTime = DateTime.Now;
                    break;
                case 18:
                    //holy blessing (rarest and most treasured power up, yet easiest to code :P )
                    world_.Players.Message("&f{0} has discovered the rare &aHoly Blessing&f!!!", p.Name);
                    if (p.Info.CTFBlueTeam)
                    {
                        world_.Players.Message("The Blue Team has been granted 1 point.");
                        redScore++;
                    }
                    else
                    {
                        world_.Players.Message("The Red Team has been granted 1 point.");
                        blueScore++;
                    }
                    foreach (Player pl in world_.Players)
                    {
                        if (pl.usesCPE && Heartbeat.ClassiCube())
                        {
                            pl.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: " + redScore + ",&1 Blue&f: " + blueScore));
                        }
                        else
                        {
                            pl.Message("The score is now &cRed&f: {0} and &1Blue&f: {1}.", redScore, blueScore);
                        }
                    }
                    break;
                default:
                    //no power up 4 u
                    break;
            }

        }
        #endregion

        #region MovingEvent
        public static void PlayerMoving(object poo, fCraft.Events.PlayerMovingEventArgs e)
        {
            if (!started)
            {
                return; 
            }
            //If the player has the red flag (player is no the blue team)
            if (e.Player.Info.hasRedFlag)
            {
                Vector3I oldPos = e.OldPosition.ToBlockCoords(); //get positions as block coords
                Vector3I newPos = e.NewPosition.ToBlockCoords();

                if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
                {
                    //If the player is near enough to the blue spawn
                    if (e.NewPosition.DistanceSquaredTo(world_.blueCTFSpawn.ToPlayerCoords()) <= 42 * 42)
                    {
                        blueScore++;
                        world_.Players.Message("&f{0} has capped the &cred &fflag. The score is now &cRed&f: {1} and &1Blue&f: {2}.", e.Player.Name, redScore, blueScore);
                        e.Player.Info.hasRedFlag = false;
                        redFlagHolder = null;
                        e.Player.Info.CTFCaptures++;

                        //Replace red block as flag
                        BlockUpdate blockUpdate = new BlockUpdate(null, world_.redFlag, Block.Red);
                        foreach (Player p in world_.Players)
                        {
                            p.World.Map.QueueUpdate(blockUpdate);

                            //set game score
                            if (p.usesCPE && Heartbeat.ClassiCube())
                            {
                                p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: " + redScore + ",&1 Blue&f: " + blueScore));
                                p.Send(PacketWriter.MakeSpecialMessage((byte)100, e.Player.Name + " has successfully capped the &cred &fflag"));
                            }
                        }
                        world_.redFlagTaken = false;
                        announced = DateTime.Now;
                        return;
                    }
                }
            }
            //If the player has the blue flag (player must be on red team)
            else if (e.Player.Info.hasBlueFlag)
            {
                Vector3I oldPos = e.OldPosition.ToBlockCoords(); //get positions as block coords
                Vector3I newPos = e.NewPosition.ToBlockCoords();

                if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
                {
                    //If the player is near enough to the red spawn
                    if (e.NewPosition.DistanceSquaredTo(world_.redCTFSpawn.ToPlayerCoords()) <= 42 * 42)
                    {
                        redScore++;
                        world_.Players.Message("&f{0} has capped the &1blue &fflag. The score is now &cRed&f: {1} and &1Blue&f: {2}.", e.Player.Name, redScore, blueScore);
                        e.Player.Info.hasBlueFlag = false;
                        blueFlagHolder = null;
                        e.Player.Info.CTFCaptures++;

                        //Replace blue block as flag
                        BlockUpdate blockUpdate = new BlockUpdate(null, world_.blueFlag, Block.Blue);
                        foreach (Player p in world_.Players)
                        {
                            p.World.Map.QueueUpdate(blockUpdate);

                            //set game scorecboard
                            if (p.usesCPE && Heartbeat.ClassiCube())
                            {
                                p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: " + redScore + ",&1 Blue&f: " + blueScore));
                                p.Send(PacketWriter.MakeSpecialMessage((byte)100, e.Player.Name + " has successfully capped the &cred &fflag"));
                            }
                        }
                        world_.blueFlagTaken = false;
                        announced = DateTime.Now;
                        return;
                    }
                }
            }

            //backstabbing, player with a flag cannot backstab an enemy player
            else
            {
                if (e.Player.Info.stabDisarmed)
                {
                    return;
                }

                Vector3I oldPos = e.OldPosition.ToBlockCoords(); //get positions as block coords
                Vector3I newPos = e.NewPosition.ToBlockCoords();

                if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
                {
                    //loop through each player, detect if current player is "touching" another player
                    foreach (Player p in world_.Players)
                    {
                        Vector3I pos = p.Position.ToBlockCoords(); //convert to block coords                       

                        //determine if player is "touching" another player
                        if (e.NewPosition.DistanceSquaredTo(pos.ToPlayerCoords()) <= 42 * 42 && p != e.Player) 
                        {
                            if ((p.Info.CTFBlueTeam && e.Player.Info.CTFBlueTeam) || (p.Info.CTFRedTeam && e.Player.Info.CTFRedTeam))
                            {
                                //friendly fire, do not stab
                                return;
                            }

                            //create just under a 180 degree semicircle in the direction the target player is facing (90 degrees = 64 pos.R bytes)
                            short lowerLimit = (short)(p.Position.R - 63);
                            short upperLimit = (short)(p.Position.R + 63);

                            //if lower limit is -45 degrees for example, convert to 256 + (-32) = 201 bytes (-45 degrees translates to -32 bytes)
                            if (lowerLimit < 0)
                            {
                                lowerLimit = (short)(256 + lowerLimit);
                            }

                            //if upper limit is 450 degrees for example, convert to 320 - 256 = 54 bytes (450 degrees translates to 320 bytes, 360 degrees translates to 256 bytes)
                            if (upperLimit > 256)
                            {
                                upperLimit = (short)(upperLimit - 256);
                            }

                            //Logger.LogToConsole(upperLimit.ToString() + " " + lowerLimit.ToString() + " " + e.Player.Position.R.ToString() + " " + p.Position.R);

                            bool kill = false;

                            //if target's line of sight contains 0
                            if (p.Position.R > 192 && p.Position.R < 64)
                            {
                                if (Enumerable.Range(lowerLimit, 255).Contains(e.Player.Position.R) || Enumerable.Range(0, upperLimit).Contains(e.Player.Position.R))
                                {
                                    kill = true;
                                }
                            }
                            else
                            {
                                if (Enumerable.Range(lowerLimit, upperLimit).Contains(e.Player.Position.R))
                                {
                                    kill = true;
                                }
                            }

                            if (e.Player.Info.stabAnywhere)
                            {
                                kill = true;
                            }

                            if (kill)
                            {
                                p.KillCTF(world_, String.Format("&f{0}&S was backstabbed by &f{1}", p.Name, e.Player.Name));
                                e.Player.Info.CTFKills++;
                                PowerUp(e.Player);

                                if (p.Info.hasRedFlag)
                                {
                                    world_.Players.Message("The red flag has been returned.");
                                    p.Info.hasRedFlag = false;
                                    redFlagHolder = null;

                                    //Put flag back
                                    BlockUpdate blockUpdate = new BlockUpdate(null, world_.redFlag, Block.Red);
                                    foreach (Player pl in world_.Players)
                                    {
                                        pl.World.Map.QueueUpdate(blockUpdate);
                                    }
                                    world_.redFlagTaken = false;

                                }

                                if (p.Info.hasBlueFlag)
                                {
                                    world_.Players.Message("The blue flag has been returned.");
                                    p.Info.hasBlueFlag = false;
                                    blueFlagHolder = null;

                                    //Put flag back
                                    BlockUpdate blockUpdate = new BlockUpdate(null, world_.blueFlag, Block.Blue);
                                    foreach (Player pl in world_.Players)
                                    {
                                        pl.World.Map.QueueUpdate(blockUpdate);
                                    }
                                    world_.blueFlagTaken = false;
                                }
                            }                         
                            //target player can see player, do not stab
                        }
                    }
                }
            }
        }
        #endregion
    }
}

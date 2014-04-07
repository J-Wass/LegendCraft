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

//Todo: backstabs and stats

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
        public const string redTeam = "&C-Red-";
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
        public static DateTime announced;

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
            if ((DateTime.Now - announced).TotalSeconds >= 5)
            {
                foreach (Player p in world_.Players)
                {
                    p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));//super hacky way to remove announcements, simply send a color code and call it a day
                }
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
                        assignTeams(p);

                        if (p.Info.IsHidden) //unhides players automatically if hidden (cannot shoot guns while hidden)
                        {
                            p.Info.IsHidden = false;
                            Player.RaisePlayerHideChangedEvent(p);
                        }

                        Logger.LogToConsole(world_.redCTFSpawn.ToString() + " " + world_.blueCTFSpawn.ToString());
                        Logger.LogToConsole(world_.redCTFSpawn.ToPlayerCoords().ToString() + " " + world_.blueCTFSpawn.ToPlayerCoords().ToString());
                        if (p.Info.CTFRedTeam)
                        {
                            p.TeleportTo(world_.redCTFSpawn.ToPlayerCoords());
                        }

                        if (p.Info.CTFRedTeam)
                        {
                            p.TeleportTo(world_.blueCTFSpawn.ToPlayerCoords());
                        }

                        p.GunMode = true;
                        GunGlassTimer timer = new GunGlassTimer(p);
                        timer.Start();

                        //send an announcement
                        p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&cLet the Games Begin!"));

                        //set player health
                        p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f[&a--------&f]"));

                        //set game score
                        p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: 0,&1 Blue&f: 0"));
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

            //Announce flag holder
            if (String.IsNullOrEmpty(redFlagHolder))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.hasRedFlag)
                    {
                        if (p.Info.hasRedFlag)
                        {
                            world_.Players.Message(p.Name + " has stolen the Red flag!");
                            redFlagHolder = p.Name;
                        }
                    }
                }
            }

            if (String.IsNullOrEmpty(blueFlagHolder))
            {
                foreach (Player p in world_.Players)
                {
                    if (p.Info.hasBlueFlag)
                    {
                        world_.Players.Message(p.Name + " has stolen the Blue flag!");
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

            if (started && startTime != null && (DateTime.Now - startTime).TotalSeconds >= (totalTime))
            {
                if (redScore != blueScore)
                {
                    Stop(null);
                    return;
                }
                if (redScore == blueScore)
                {
                    world_.Players.Message("&fThe teams tied {0} to {1}!", blueScore, redScore);
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

        public static void RevertNames()    //reverts names for online players. offline players get reverted upon leaving the game
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
                    p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));
                    p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f"));
                    p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&f"));

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

        public static void PlayerMoving(object poo, fCraft.Events.PlayerMovingEventArgs e)
        {
            //If the player has the red flag
            if (e.Player.Info.hasRedFlag)
            {
                Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32); //get positions as block coords
                Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
                {
                    //If the player is near enough to the blue spawn
                    if (e.NewPosition.DistanceSquaredTo(world_.blueCTFSpawn.ToPlayerCoords()) <= 42 * 42)
                    {
                        blueScore++;
                        world_.Players.Message("&f{0} has successfully capped the &cred &fflag. The score is now &cRed&f:{1} and &1Blue&f:{2}.", e.Player.Name, redScore, blueScore);
                        e.Player.Info.hasRedFlag = false;
                        e.Player.Info.CTFCaptures++;

                        //Replace red block as flag
                        BlockUpdate blockUpdate = new BlockUpdate(null, world_.redFlag, Block.Red);
                        foreach (Player p in world_.Players)
                        {
                            p.World.Map.QueueUpdate(blockUpdate);

                            //set game score
                            p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: " + redScore + ",&1 Blue&f: " + blueScore));
                            p.Send(PacketWriter.MakeSpecialMessage((byte)100, e.Player.Name + " has successfully capped the &cred &fflag"));
                        }
                        announced = DateTime.Now; 
                    }
                }
            }

            //If the player has the blue flag
            if (e.Player.Info.hasBlueFlag)
            {
                Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32); //get positions as block coords
                Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
                {
                    //If the player is near enough to the red spawn
                    if (e.NewPosition.DistanceSquaredTo(world_.redCTFSpawn.ToPlayerCoords()) <= 42 * 42)
                    {
                        redScore++;
                        world_.Players.Message("&f{0} has successfully capped the &1blue &fflag. The score is now &cRed:&f {1} and &1Blue:&f {2}.", e.Player.Name, redScore, blueScore);
                        e.Player.Info.hasBlueFlag = false;
                        e.Player.Info.CTFCaptures++;

                        //Replace blue block as flag
                        BlockUpdate blockUpdate = new BlockUpdate(null, world_.blueFlag, Block.Blue);
                        foreach (Player p in world_.Players)
                        {
                            p.World.Map.QueueUpdate(blockUpdate);

                            //set game scorecb
                            p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: " + redScore + ",&1 Blue&f: " + blueScore));
                            p.Send(PacketWriter.MakeSpecialMessage((byte)100, e.Player.Name + " has successfully capped the &cred &fflag"));
                        }
                        announced = DateTime.Now; 
                    }
                }
            }
        }
    }
}

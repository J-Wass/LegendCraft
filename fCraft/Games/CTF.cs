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

//ToDo: Killing someone with a gun who has a flag removed the flag, setting flags

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    class CTF
    {
        //Team Tags
        public const string redTeam = "&C-Red-";
        public const string blueTeam = "&1*Blue*";

        //TDM stats
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
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
            }
            return instance;
        }

        public static void Start()
        {
            world_.Hax = false;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }

            world_.gameMode = GameMode.CaptureTheFlag; //set the game mode
            delayTask = Scheduler.NewTask(t => world_.Players.Message("&WCTF &fwill be starting in {0} seconds: &WGet ready!", timeDelay));
            delayTask.RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), 1);
        }

        public static void Stop(Player p) //for stopping the game early
        {

            world_.Hax = true;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }
            if (p != null && world_ != null)
            {
                world_.Players.Message("{0}&S stopped the game of CTF early on world {1}",
                    p.ClassyName, world_.ClassyName);
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
            if (world_.gameMode != GameMode.TeamDeathMatch)
            {
                task.Stop();
                world_ = null;
                return;
            }
            if (!started)
            {
                if (world_.Players.Count() < 2) //in case players leave the world or disconnect during the start delay
                {
                    world_.Players.Message("&WCTF&s requires at least 2 people to play.");
                    return;
                }
                if (startTime != null && (DateTime.Now - startTime).TotalSeconds > timeDelay)
                {
                    foreach (Player p in world_.Players)
                    {
                        if (p.Info.isOnRedTeam) { p.TeleportTo(world_.redCTFSpawn.ToPlayerCoords()); } //teleport players to the team spawn
                        if (p.Info.isOnBlueTeam) { p.TeleportTo(world_.blueCTFSpawn.ToPlayerCoords()); }

                        if (!p.GunMode)
                        {
                            p.GunMode = true; //turns on gunMode automatically if not already on
                            GunGlassTimer timer = new GunGlassTimer(p);
                            timer.Start();
                        }

                        if (p.Info.IsHidden) //unhides players automatically if hidden (cannot shoot guns while hidden)
                        {
                            p.Info.IsHidden = false;
                            Player.RaisePlayerHideChangedEvent(p);
                        }
                    }
                    started = true;   //the game has officially started
                    isOn = true;
                    if (!world_.gunPhysics)
                    {
                        world_.EnableGunPhysics(Player.Console, true); //enables gun physics if they are not already on
                    }
                    lastChecked = DateTime.Now;     //used for intervals
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
                int redCount = world_.Players.Where(p => p.Info.CTFRedTeam).ToArray().Count();
                int blueCount = world_.Players.Where(p => p.Info.CTFBlueTeam).ToArray().Count();
                if (blueCount < 1 || redCount < 1)
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
            List<PlayerInfo> TDPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => (r.isOnBlueTeam || r.isOnRedTeam) && r.IsOnline).ToArray());
            for (int i = 0; i < TDPlayers.Count(); i++)
            {
                string p1 = TDPlayers[i].Name.ToString();
                PlayerInfo pI = PlayerDB.FindPlayerInfoExact(p1);
                Player p = pI.PlayerObject;

                if (p != null)
                {
                    p.iName = null;
                    pI.tempDisplayedName = null;
                    pI.CTFBlueTeam = false;
                    pI.CTFRedTeam = false;
                    pI.isPlayingCTF = false;
                    p.entityChanged = true;

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
                        p.Message("Your status has been reverted.");
                    }
                }
            }
        }

        public static void AssignRed(Player p)
        {
            string sbName = p.Name;
            p.Message("Let the games Begin!");
            p.Message("You are on the &cRed Team");
            p.iName = "TeamRed";
            p.Info.tempDisplayedName = "&f(" + redTeam + "&f) " + Color.Red + sbName;
            p.Info.isOnRedTeam = true;
            p.Info.isOnBlueTeam = false;
            p.Info.isPlayingTD = true;
            p.entityChanged = true;
            p.Info.gameKills = 0;
            p.Info.gameDeaths = 0;
            redTeamCount++;
            return;
        }
        public static void AssignBlue(Player p)
        {
            string sbName = p.Name;
            p.Message("Let the games Begin!");
            p.Message("You are on the &1Blue Team");
            p.iName = "TeamBlue";
            p.Info.tempDisplayedName = "&f(" + blueTeam + "&f) " + Color.Navy + sbName;
            p.Info.isOnBlueTeam = true;
            p.Info.isOnRedTeam = false;
            p.Info.isPlayingTD = true;
            p.entityChanged = true;
            p.Info.gameKills = 0;
            p.Info.gameDeaths = 0;
            blueTeamCount++;
            return;
        }

    }
}

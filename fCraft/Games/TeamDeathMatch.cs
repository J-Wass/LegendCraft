//Developed by DingusBungus for use with the LegendCraft software. Template based off of 800Craft's ZombieGame.cs

/* Copyright (c) <2013-2014> <LeChosenOne, DingusBungus>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    class TeamDeathMatch
    {
        //Team Tags
        public const string redTeam = "&c-Red-";
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
        public static TeamDeathMatch instance;
        public static DateTime startTime;
        public static DateTime lastChecked;
        public static DateTime announced;

        //customization (initialized as defaults)
        public static int timeLimit = 300; 
        public static int scoreLimit = 50;
        public static int timeDelay = 20;
        public static bool manualTeams = false;
        public static int totalTime = timeLimit + timeDelay;
        public static Position redSpawn = Position.Zero;
        public static Position blueSpawn = Position.Zero;
        
        //TDM Game Bools
        public static bool isOn = false;
        private static bool started = false;

        private static World world_;

        public static TeamDeathMatch GetInstance(World world)
        {
            if (instance == null)
            {
                world_ = world;
                instance = new TeamDeathMatch();
                startTime = DateTime.Now;
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
                if (manualTeams)
                {
                    timeDelay = 30;
                }
            }
            return instance;
        }

        public static void Start()
        {
            world_.Hax = false;
            world_.gameMode = GameMode.TeamDeathMatch; //set the game mode
            delayTask = Scheduler.NewTask(t => world_.Players.Message("&WTEAM DEATHMATCH &fwill be starting in {0} seconds: &WGet ready!", timeDelay));
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
                world_.Players.Message("{0}&S stopped the game of Team Deathmatch early on world {1}",
                    p.ClassyName, world_.ClassyName);
            }
            if (world_.Players.Count() > 1)
            {
                DisplayScoreBoard();
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

            //remove announcement after 5 seconds
            if ((DateTime.Now - announced).TotalSeconds >= 5)
            {
                foreach (Player p in world_.Players)
                {
                    if (p.usesCPE && Heartbeat.ClassiCube())
                    {
                        p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));//super hacky way to remove announcement, simply send a color code and call it a day
                    }
                }
            }

            if (!started)
            {
                if (world_.Players.Count() < 2) //in case players leave the world or disconnect during the start delay
                {
                    world_.Players.Message("&WTeam DeathMatch&s requires at least 2 people to play.");
                    return;
                }
                if (startTime != null && (DateTime.Now - startTime).TotalSeconds > timeDelay)
                {
                    foreach (Player p in world_.Players)
                    {

                        if (!manualTeams)
                        {
                            assignTeams(p); //assigns teams (who knew?)
                        }
                        if (p.Info.isOnRedTeam) { p.TeleportTo(TeamDeathMatch.redSpawn); } //teleport players to the team spawn
                        if (p.Info.isOnBlueTeam) { p.TeleportTo(TeamDeathMatch.blueSpawn); }

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

                        //send an announcement
                        p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&cLet the Games Begin!"));

                        if (p.usesCPE && Heartbeat.ClassiCube())
                        {
                            //set player's health
                            p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f[&a--------&f]"));

                            //set game score
                            p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&cRed&f: 0,&1 Blue&f: 0"));
                        }
                    }
                    started = true;   //the game has officially started
                    isOn = true;
                    if (!world_.gunPhysics)
                    {
                        world_.EnableGunPhysics(Player.Console, true); //enables gun physics if they are not already on
                    }
                    lastChecked = DateTime.Now;     //used for intervals
                    announced = DateTime.Now; //set when the announcement was launched
                    return;
                }   
            }          

            //check if one of the teams have won
            if (redScore >= scoreLimit || blueScore >= scoreLimit)
            {
                Stop(null);
                return;
            }
            if (blueScore == scoreLimit && redScore == scoreLimit) //if they somehow manage to tie which I am pretty sure is impossible
            {
                world_.Players.Message("The teams tied at {0}!", redScore);
                Stop(null);
                return;
            }

            //check if time is up
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

            if (started && (DateTime.Now - lastChecked).TotalSeconds > 10) //check if players left the world, forfeits if no players of that team left
            {
                int redCount = world_.Players.Where(p => p.Info.isOnRedTeam).ToArray().Count();
                int blueCount = world_.Players.Where(p => p.Info.isOnBlueTeam).ToArray().Count();
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

        public static void DisplayScoreBoard()
        {
            List<PlayerInfo> TDPlayersRed = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => r.isPlayingTD).Where(r => r.isOnRedTeam).ToArray().OrderBy(r => r.totalKillsTDM).Reverse());
            List<PlayerInfo> TDPlayersBlue = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => r.isPlayingTD).Where(r => r.isOnBlueTeam).ToArray().OrderBy(r => r.totalKillsTDM).Reverse());
            if (redScore < blueScore)
            {
                world_.Players.Message("&1Blue Team &f{0}&1:", TeamDeathMatch.blueScore);
                for (int i = 0; i < TDPlayersBlue.Count(); i++)
                {
                    string sbNameBlue = TDPlayersBlue[i].Name;
                    world_.Players.Message("&1{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                        sbNameBlue, TDPlayersBlue[i].gameKills, TDPlayersBlue[i].gameDeaths);
                }
                world_.Players.Message("&f--------------------------------------------");
                world_.Players.Message("&CRed Team &f{0}&1:", TeamDeathMatch.redScore);
                for (int i = 0; i < TDPlayersRed.Count(); i++)
                {
                    string sbNameRed = TDPlayersRed[i].Name;
                    world_.Players.Message("&C{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                        sbNameRed, TDPlayersRed[i].gameKills, TDPlayersRed[i].gameDeaths);
                }
            }
            if (redScore >= blueScore)
            {
                world_.Players.Message("&CRed Team &f{0}&1:", TeamDeathMatch.redScore);
                for (int i = 0; i < TDPlayersRed.Count(); i++)
                {
                    string sbNameRed = TDPlayersRed[i].Name;
                    world_.Players.Message("&C{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                        sbNameRed, TDPlayersRed[i].gameKills, TDPlayersRed[i].gameDeaths);
                }
                world_.Players.Message("&f--------------------------------------------");
                world_.Players.Message("&1Blue Team &f{0}&1:", TeamDeathMatch.blueScore);
                for (int i = 0; i < TDPlayersBlue.Count(); i++)
                {
                    string sbNameBlue = TDPlayersBlue[i].Name;
                    world_.Players.Message("&1{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                        sbNameBlue, TDPlayersBlue[i].gameKills, TDPlayersBlue[i].gameDeaths);
                }              
            }
            return;
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
                    pI.isOnRedTeam = false;
                    pI.isOnBlueTeam = false;
                    pI.isPlayingTD = false;
                    pI.Health = 100;
                    p.entityChanged = true;

                    //reset all special messages
                    if (p.usesCPE && Heartbeat.ClassiCube())
                    {
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

//Developed by DingusBungus for use with the LegendCraft software. Template based off of 800Craft's ZombieGame.cs

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    class FFA
    {
        //Timing
        public static int timeLeft = 0;
        private static SchedulerTask task_;
        public static SchedulerTask delayTask;
        public static FFA instance;
        public static DateTime startTime;
        public static DateTime lastChecked;
        public static DateTime announced;

        //Customization (initialized as defaults)
        public static int timeLimit = 300;
        public static int scoreLimit = 30;
        public static int timeDelay = 20;
        public static int totalTime = timeLimit + timeDelay;
        public static bool tntAllowed = false;

        //FFA Game Bools
        private static bool started = false;

        //Values
        private static World world_;
        public static Random rand = new Random();
        public static bool stoppedEarly = false;
        public static int playerCount = 0;

        public static FFA GetInstance(World world)
        {
            if (instance == null)
            {
                world_ = world;
                instance = new FFA();
                startTime = DateTime.Now;
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1)); 
                stoppedEarly = false; //to catch bugs when there is an unexpected crash (for testing/debugging)
            }
            return instance;
        }

        public static void Start()
        {
            world_.Hax = false;
            world_.gameMode = GameMode.FFA; //set the game mode
            delayTask = Scheduler.NewTask(t => world_.Players.Message("&WFFA &fwill be starting in {0} seconds: &WGet ready!", (timeDelay - (DateTime.Now - startTime).ToSeconds())));
            delayTask.RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), (timeDelay / 10));
        }

        public static void Stop(Player p) //for stopping the game early
        {
            world_.Hax = true;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }
            //unhook event handlers
            if (world_ == null) return;                                                     
            if (world_ != null && world_.Players.Count() > 0 && stoppedEarly)               
            {
                world_.Players.Message("{0}&S stopped the game of FFA early on world {1}",
                    p.ClassyName, world_.ClassyName);
            }
            if (p != null && !stoppedEarly)
            {
                world_.Players.Message("{0} &fhas won the game with &c{1}&f points!", p.ClassyName, p.Info.gameKillsFFA);
            }
            if (world_.Players.Count() > 1 && p != null)
            {
                DisplayScoreBoard();
            }
            RevertGame();
            RevertGun();

            if (!delayTask.IsStopped)//if stop is called when the delayTask is still going, stop the delayTask
            {
                delayTask.Stop();
            }

            return;
        }
        
        public static bool isOn() //get-method for private variable 'started'
        {
            bool isOn = started;
            return isOn;
        }

        public static void Interval(SchedulerTask task)
        {
            //check to stop Interval
            if (world_ == null)
            {
                task.Stop();
                return;
            }
            if (world_.gameMode != GameMode.FFA) //bug checking
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

            if (!started)//first time running the interval
            {
                if (world_.Players.Count() < 2) //in case players leave the world or disconnect during the start delay
                {
                    world_.Players.Message("&WFFA&s requires at least 2 people to play.");
                    task.Stop();
                    return;
                }
                if (startTime != null && (DateTime.Now - startTime).TotalSeconds > timeDelay)
                {
                    foreach (Player p in world_.Players)
                    {
                        int x = rand.Next(2, world_.Map.Width);
                        int y = rand.Next(2, world_.Map.Length);
                        int z1 = 0;
                        for (int z = world_.Map.Height - 1; z > 0; z--)
                        {
                            if (world_.Map.GetBlock(x, y, z) != Block.Air)
                            {
                                z1 = z + 3;
                                break;
                            }
                        }                       
                        p.TeleportTo(new Position(x, y, z1 + 2).ToVector3I().ToPlayerCoords()); //teleport players to a random position
                        InitializePlayer(p);
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

                        //set player health
                        p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f[&a--------&f]"));

                        //set leader
                        p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&eCurrent Leader&f: None"));
                    }
                    started = true;   //the game has officially started
                    if (!world_.gunPhysics)
                    {
                        world_.EnableGunPhysics(Player.Console, true); //enables gun physics if they are not already on
                    }
                    lastChecked = DateTime.Now;     //used for intervals
                    announced = DateTime.Now; //set when the announcement was launched
                    return;
                }
            }

            //check if one of the players has won
            foreach (Player p in world_.Players)
            {
                if (started && startTime != null && (DateTime.Now - startTime).TotalSeconds >= timeDelay && p.Info.gameKillsFFA >= scoreLimit)
                {
                    Stop(p);
                    return;
                }
            }

            //check if time is up
            if (started && startTime != null && (DateTime.Now - startTime).TotalSeconds >= (totalTime))
            {
                Player winner = GetScoreList()[0];
                if (world_.Players.Count() < 2)
                {
                    Stop(winner);
                    return;
                }
                Stop(winner);
                return;
            }

            if (started && (DateTime.Now - lastChecked).TotalSeconds > 10) //check if players left the world, forfeits if no players of that team left
            {
                if (world_.Players.Count() < 2)
                {
                    Player[] players = world_.Players;
                    Stop(players[0]);
                    return;
                }
            }
            timeLeft = Convert.ToInt16(((timeDelay + timeLimit) - (DateTime.Now - startTime).ToSeconds()));
            //Keep the players updated about the score
            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.9 && timeLeft <= timeLimit)
            {
                Player leader = GetScoreList()[0];  //leader is the top of the score list
                Player secondPlace = GetScoreList()[1]; //second place is - well, second place XD
                if (isOn() && leader.Info.gameKillsFFA != secondPlace.Info.gameKillsFFA)
                {
                    world_.Players.Message("{0}&f is winning &c{1} &fto &c{2}", leader.ClassyName, leader.Info.gameKillsFFA, secondPlace.Info.gameKillsFFA);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                if (leader.Info.gameKillsFFA == secondPlace.Info.gameKillsFFA)
                {
                    world_.Players.Message("{1}&f and {2}&f are tied at &c{0}!", leader.Info.gameKillsFFA, leader.ClassyName, secondPlace.ClassyName);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                lastChecked = DateTime.Now;
            }
            if (timeLeft < 10.1)
            {
                world_.Players.Message("&WOnly 10 seconds left!");
            }
        }

        public static void RevertGame() //Reset game bools/stats and stop timers
        {
            task_.Stop();
            world_.gameMode = GameMode.NULL;
            instance = null;
            started = false;
            if (world_.gunPhysics)
            {
                world_.DisableGunPhysics(Player.Console, true);
            }
            world_ = null;
            playerCount = 0;
            stoppedEarly = false;
        }

        public static void DisplayScoreBoard()
        {
            var players = GetScoreList();
            if (world_.Players.Count() > 0)
            {
                world_.Players.Message("The final score of the game:");
                for (int i = 0; i < players.Count(); i++)
                {
                    string sbName = players[i].Info.Name;
                    string color = "&c";                //will show the top 3 players in red and the rest in white (emphasize the winners)
                    if (players.Count() > 3 && i > 2)
                    {
                        color = "&f";
                    }
                    world_.Players.Message("{3}{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                        sbName, players[i].Info.gameKillsFFA, players[i].Info.gameDeathsFFA, color);
                }
            }
            return;
        }

        public static List<Player> GetScoreList()
        {
            List<Player> FFAPlayers = new List<Player>(Server.Players.Where(p => p.Info.isPlayingFFA).Where(p => p.World == world_)
                                        .OrderBy(p => p.Info.gameKillsFFA).ToArray().Reverse());
            return (FFAPlayers);
        }
        public static void RevertGun()    //Reverts names for online players. Offline players get reverted upon leaving the game
        {
            List<PlayerInfo> FFAPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => (r.isPlayingFFA) && r.IsOnline).ToArray());
            foreach (PlayerInfo pI in FFAPlayers)
            {               
                Player p = pI.PlayerObject;
                p.JoinWorld(p.World, WorldChangeReason.Rejoin);

                //reset all special messages
                if (p.usesCPE && Heartbeat.ClassiCube())
                {
                    p.Send(PacketWriter.MakeSpecialMessage((byte)100, "&f"));
                    p.Send(PacketWriter.MakeSpecialMessage((byte)1, "&f"));
                    p.Send(PacketWriter.MakeSpecialMessage((byte)2, "&f"));
                }

                pI.isPlayingFFA = false;
                if (pI != null)
                {                    
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

        public static void InitializePlayer(Player p)
        {
            string sbName = p.Info.Name;
            if (p.Name.Contains('@'))
            {
                sbName = p.Info.Name.Split('@')[0];
            }
            p.iName = sbName;
            p.Info.isPlayingFFA = true;
            p.entityChanged = true;
            p.Info.gameKillsFFA = 0;
            p.Info.gameDeathsFFA = 0;
            return;
        }

    }
}

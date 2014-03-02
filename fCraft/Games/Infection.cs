/* Copyright (c) <2012 - 2014> <LeChosenOne, DingusBungus>
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
using System.Diagnostics;
using fCraft.Events;

namespace fCraft.Games
{
    class Infection
    {

        //Timing
        private static SchedulerTask task_;
        public static SchedulerTask delayTask;
        public static DateTime startTime;
        public static DateTime lastChecked;
        public static int timeLeft = 0;
        public static int timeLimit = 300;
        public static int timeDelay = 20;
        public static Stopwatch stopwatch = new Stopwatch();

        //Values
        public static bool isOn = false;
        public static Infection instance;
        private static World world_;
        public static Random rand = new Random();

        public static Infection GetInstance(World world)
        {
            if (instance == null)
            {
                world_ = world;
                instance = new Infection();
                startTime = DateTime.Now;
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromMilliseconds(250)); //run loop every quarter second
            }
            return instance;
        }

        public static void Start()
        {
            timeLimit = 300;//reset variables incase of a server using custom and then start
            timeDelay = 20;

            world_.Hax = false;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }
            //world_.Players.Send(PacketWriter.MakeHackControl(0,0,0,0,0,-1)); Commented out until classicube clients support hax packet
            stopwatch.Reset();
            stopwatch.Start();
            world_.gameMode = GameMode.Infection;
            delayTask = Scheduler.NewTask(t => world_.Players.Message("&WInfection &fwill be starting in {0} seconds: &WGet ready!", (timeDelay - stopwatch.Elapsed.Seconds)));
            delayTask.RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), (int)Math.Floor((double)(timeDelay/10)));//Start task immediately, send message every 10s
            if (stopwatch.Elapsed.Seconds > 11)
            {
                stopwatch.Stop();
            }
        }

        public static void Stop(Player p) //for stopping the game early
        {
            world_.Hax = true;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }
            //world_.Players.Send(PacketWriter.MakeHackControl(1, 1, 1, 1, 1, -1)); Commented out until classicube clients support hax packet
            if (p != null && world_ != null)
            {
                world_.Players.Message("{0}&S stopped the game of Infection on world {1}", p.ClassyName, world_.ClassyName);                    
            }
            stopwatch.Reset();//reset timer for next game
            RevertGame();
            if (!delayTask.IsStopped)//if stop is called when the delayTask is still going, stop the delayTask
            {
                delayTask.Stop();
            }
            return;
        }

        public static void Custom(int limit, int delay)
        {
            world_.Hax = false;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }
            // world_.Players.Send(PacketWriter.MakeHackControl(0, 0, 0, 0, 0, -1)); Commented out until classicube clients support hax packet
            timeDelay = delay;
            timeLimit = limit;

            world_.gameMode = GameMode.Infection;
            stopwatch.Reset();
            stopwatch.Start();
            Scheduler.NewTask(t => world_.Players.Message("&WInfection &fwill be starting in {0} seconds: &WGet ready!", (timeDelay - stopwatch.Elapsed.Seconds))).RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), 2);
            if (stopwatch.Elapsed.Seconds > 11)
            {
                stopwatch.Stop();
            }
            task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
        }       

        public static void Interval(SchedulerTask task)
        {
            //check to stop Interval
            if (world_ == null)
            {
                task.Stop();
                return;
            }
            if (world_.gameMode != GameMode.Infection)
            {
                world_ = null;
                task.Stop();
                return;
            }

            if (!isOn)
            {
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
                        beginGame(p);

                        Player.Moving += PlayerMoved;
                        Player.JoinedWorld += PlayerLeftWorld;
                        Player.Disconnected += PlayerLeftServer;

                        chooseInfected();
                        
                    }
                    isOn = true;
                    lastChecked = DateTime.Now;     //used for intervals
                    return;
                }
            }
            timeLeft = Convert.ToInt16(((timeDelay + timeLimit) - (DateTime.Now -  startTime).TotalSeconds));

            if (isOn) //Check for win conditions
            {
                if (world_.Players.Count(player => player.Info.isInfected) == world_.Players.Count())
                {
                    world_.Players.Message("");
                    world_.Players.Message("&cThe Zombies have won! All humans have died off!");
                    Player.Moving -= PlayerMoved;
                    Player.JoinedWorld -= PlayerLeftWorld;
                    Player.Disconnected -= PlayerLeftServer;
                    RevertGame();
                    return;
                }
                if (world_.Players.Count(player => player.Info.isInfected) == 0 && world_.Players.Count() > 0)
                {
                    world_.Players.Message("");
                    world_.Players.Message("&aThe Zombies have died off! The Humans win!");
                    Player.Moving -= PlayerMoved;
                    Player.JoinedWorld -= PlayerLeftWorld;
                    Player.Disconnected -= PlayerLeftServer;
                    RevertGame();
                    return;
                }
                if (timeLeft == 0)
                {
                    world_.Players.Message("");
                    world_.Players.Message("&aThe Zombies failed to infect everyone! The Humans win!");
                    Player.Moving -= PlayerMoved;
                    Player.JoinedWorld -= PlayerLeftWorld;
                    Player.Disconnected -= PlayerLeftServer;
                    RevertGame();
                    return;
                }

            }

            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.9 && timeLeft < timeLimit)
            {
                world_.Players.Message("&sThere are currently {0} human(s) and {1} zombie(s) left on {2}.", world_.Players.Count() - world_.Players.Count(player => player.Info.isInfected), world_.Players.Count(player => player.Info.isInfected), world_.ClassyName);
                world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                lastChecked = DateTime.Now;
            }
        }

        public static void beginGame(Player player)
        {
            player.Info.isPlayingInfection = true;
        }
       
        public static void chooseInfected()
        {
            Random randInfected = new Random();
            int randNumber = randInfected.Next(0, world_.Players.Length);
            Player infected = world_.Players[randNumber];

            if (world_.Players.Count(player => player.Info.isInfected) == 0)
            {
                world_.Players.Message("&c{0} has been infected!", infected.Name);
            }

            infected.Info.isInfected = true;
            infected.iName = "_Infected_";
            infected.Info.tempDisplayedName = "_Infected_";
            infected.entityChanged = true;
        }

        public static void Infect(Player target, Player player)
        {
            world_.Players.Message("&c{0} has infected {1}!", player.Name, target.Name);
            target.Info.isInfected = true;
            target.iName = "_Infected_";
            target.Info.tempDisplayedName = "&f_Infected_";
            target.entityChanged = true;
        }

        public static void RevertGame() //Reset game bools/stats and stop timers
        {
            world_.Hax = true;
            foreach (Player pl in world_.Players)
            {
                pl.JoinWorld(world_, WorldChangeReason.Rejoin);
            }
            //world_.Players.Send(PacketWriter.MakeHackControl(1, 1, 1, 1, 1, -1)); Commented out until classicube clients support hax packet

            foreach (Player p in world_.Players)
            {
                p.Info.isPlayingInfection = false;
                if (p.Info.isInfected)
                {
                    p.Info.isInfected = false;
                    p.iName = null;
                    p.Info.tempDisplayedName = null;
                    p.entityChanged = false;
                }
                p.Message("&aYour status has been reverted!");
                p.JoinWorld(world_, WorldChangeReason.Rejoin);
            }

            
            world_.gameMode = GameMode.NULL;
            task_.Stop();
            isOn = false;
            instance = null;
            world_ = null;
          

        }
        #region events

        //check if player tagged another player
        public static void PlayerMoved(object poo, fCraft.Events.PlayerMovingEventArgs e)
        {
            if (e.Player.Info.isInfected)
            {       
                Vector3I oldPos = new Vector3I( e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32 ); //get positions as block coords
                Vector3I newPos = new Vector3I( e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32 );

                if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
                {
                    foreach (Player p in world_.Players)
                    {
                        Vector3I pos = p.Position.ToBlockCoords(); //convert to block coords                       

                        if (e.NewPosition.DistanceSquaredTo(pos.ToPlayerCoords()) <= 42 * 42 && p != e.Player) //Get blocks on and around player, ignore instances when the target = player
                        {
                            if (!p.Info.isInfected)
                            {
                                Infect(p, e.Player);
                            }
                        }
                    }
                }
            }
        }

        //check if player left server to reset stats
        public static void PlayerLeftServer(object poo, fCraft.Events.PlayerDisconnectedEventArgs e)
        {
            e.Player.Info.isPlayingInfection = false;
            if (e.Player.Info.isInfected)
            {
                e.Player.Info.isInfected = false;
                e.Player.entityChanged = false;
                e.Player.iName = null;
            }

        }

        //check if player left world where infection is being played
        public static void PlayerLeftWorld(object poo, fCraft.Events.PlayerJoinedWorldEventArgs e)
        {

            e.Player.Info.isPlayingInfection = false;
            if (e.Player.Info.isInfected)
            {
                e.Player.Info.isInfected = false;
                e.Player.entityChanged = false;
                e.Player.iName = null;
            }
        }
        #endregion
    }
}

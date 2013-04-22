/* Copyright (c) <2012> <LeChosenOne, DingusBungus, Eeyle>
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

namespace fCraft.Games
{
    class Infection
    {

        //Timing
        private static SchedulerTask task_;
        public static DateTime startTime;
        public static DateTime lastChecked;
        public static int timeLeft = 0;
        public static int timeLimit = 300;
        public static int timeDelay = 20;

        //Values
        public static bool isOn = false;
        public static Infection instance;
        private static World world_;
        public static Random rand = new Random();
        public static List<Player> InfectionPlayers = new List<Player>();


        public Infection(World world)
        {
                world_ = world;
                startTime = DateTime.Now;
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
        }

        public static void Start(World world)
        {
            Player.Moving += PlayerMoved;
           
            world_ = world;           
            startTime = DateTime.Now;
            world_.gameMode = GameMode.Infection; 
            Scheduler.NewTask(t => world_.Players.Message("&WInfection &fwill be starting in {0} seconds: &WGet ready!", timeDelay))
                .RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(20), 2);
            task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
        }

        public static void Stop(Player p) //for stopping the game early
        {
            Player.Moving -= PlayerMoved;
            if (p != null && world_ != null)
            {
                world_.Players.Message("{0}&S stopped the game of Infection on world {1}",
                    p.ClassyName, world_.ClassyName);
            }
            RevertGame();
            return;
        }

        public static void Interval(SchedulerTask task)
        {
            //check to stop Interval
            if (world_.gameMode != GameMode.Infection || world_ == null)
            {
                world_ = null;
                task.Stop();
                return;
            }
            if (!isOn)
            {
                if (world_.Players.Count() < 2) //in case players leave the world or disconnect during the start delay
                {
                    world_.Players.Message("&WInfection&s requires at least 2 people to play.");
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
                        beginGame(p);
                        chooseInfected();
                    }
                    isOn = true;
                    lastChecked = DateTime.Now;     //used for intervals
                    return;
                }
            }
            timeLeft = Convert.ToInt16(((timeDelay + timeLimit) - (DateTime.Now -  startTime).TotalSeconds));

            if (isOn && (DateTime.Now - lastChecked).TotalSeconds > 2) //Check for win conditions
            {
                if (world_.Players.Count(player => player.Info.isInfected) == world_.Players.Count())
                {
                    world_.Players.Message("&cThe Zombies have won!!!");
                    Stop(null);
                    return;
                }
                if (world_.Players.Count(player => player.Info.isInfected) == 0 && world_.Players.Count() > 0)
                {
                    world_.Players.Message("&aThe Zombies have died off! The Humans win!");
                    Stop(null);
                    return;
                }
                if (timeLeft == 0) //if the time limit has hit 0 before all humans are turned into zombies
                {
                    world_.Players.Message("&aThe Zombies failed to infect everyone! The Humans win!");
                    Stop(null);
                    return;
                }

            }

            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.9 && timeLeft <= timeLimit)
            {
                world_.Players.Message("&sThere are currently {0} human(s) and {1} zombie(s) left on {2}", world_.Players.Count() - world_.Players.Count(player => player.Info.isInfected), world_.Players.Count(player => player.Info.isInfected), world_.ClassyName);
            }
        }

        public static void beginGame(Player player)
        {
            player.Info.isPlayingInfection = true;
            InfectionPlayers.Add(player);
        }

        //Choose a random player as the infected player
        static Random randInfected = new Random();
        static int randNumber = randInfected.Next(0, InfectionPlayers.Count());
        static Player infected = InfectionPlayers[randNumber];
       
        public static void chooseInfected()
        {
            world_.Players.Message("&c{0} has been infected!", infected.Name);
            infected.Info.isInfected = true;
            infected.Info.oldname =  infected.Info.DisplayedName;
            infected.Info.DisplayedName = "&cINFECTED";
            infected.iName = "&cINFECTED";
            infected.entityChanged = true;
        }

        //rather primitive, but yolo
        public static bool tagged(Player player, Player target)
        {
            //center center
            if (player.Position == target.Position)
            {
                return true;
            }
            //bottom left
            if ((player.Position.X - 1) == (target.Position.X) && (player.Position.Y - 1) == (target.Position.Y))
            {
                return true;
            }
            //center left
            if ((player.Position.X - 1) == (target.Position.X) && (player.Position.Y) == (target.Position.Y))
            {
                return true;
            }
            //top left
            if ((player.Position.X - 1) == (target.Position.X) && (player.Position.Y + 1) == (target.Position.Y))
            {
                return true;
            }
            //top center
            if ((player.Position.X) == (target.Position.X) && (player.Position.Y + 1) == (target.Position.Y))
            {
                return true;
            }
            //top right
            if ((player.Position.X + 1) == (target.Position.X) && (player.Position.Y + 1) == (target.Position.Y))
            {
                return true;
            }
            //center right
            if ((player.Position.X + 1) == (target.Position.X) && (player.Position.Y) == (target.Position.Y))
            {
                return true;
            }
            //bottom right
            if ((player.Position.X + 1) == (target.Position.X) && (player.Position.Y - 1) == (target.Position.Y))
            {
                return true;
            }
            //bottom center
            if ((player.Position.X) == (target.Position.X) && (player.Position.Y - 1) == (target.Position.Y))
            {
                return true;
            }
            return false;
        }

        public static void RevertGame() //Reset game bools/stats and stop timers
        {
            world_.gameMode = GameMode.NULL;
            task_.Stop();
            isOn = false;
            instance = null;
            world_ = null;
          
            foreach (Player p in InfectionPlayers)
            {
                p.Info.isPlayingInfection = false;
                if (p.Info.isInfected)
                {
                    p.Info.isInfected = false;
                    p.Info.DisplayedName = p.Info.oldname;
                    p.iName = null;
                    p.entityChanged = false;
                }
                p.Message("&aYour status has been reverted!");
            }

        }
        #region events

        //check if player tagged another player
        public static void PlayerMoved(object poo, fCraft.Events.PlayerMovingEventArgs e)
        {
            if (e.Player.Info.isInfected)
            {
                foreach (Player target in InfectionPlayers)
                {
                    if (tagged(e.Player, target) && !target.Info.isInfected)
                    {
                        world_.Players.Message("&c{0} has infected {1}!", e.Player.Name, target.Name);
                        target.Info.isInfected = true;
                        target.Info.oldname = target.Info.DisplayedName;
                        target.Info.DisplayedName = "&cINFECTED";
                        target.iName = "&cINFECTED";
                        target.entityChanged = true;
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
                e.Player.Info.DisplayedName = e.Player.Info.oldname;
                e.Player.entityChanged = false;
                e.Player.iName = null;
            }

        }

        //check if player left world where infection is being played
        public static void PlayerLeftWorld(object poo, fCraft.Events.PlayerJoinedWorldEventArgs e)
        {
            //kinda rusty, if the player left the world and joined a different world, prevents /rejoin from breaking the game
            if(e.Player.World.Name != world_.ToString())
            {
                e.Player.Info.isPlayingInfection = false;
                if (e.Player.Info.isInfected)
                {
                    e.Player.Info.isInfected = false;
                    e.Player.Info.DisplayedName = e.Player.Info.oldname;
                    e.Player.entityChanged = false;
                    e.Player.iName = null;
                }
            }
        }
        #endregion
    }
}

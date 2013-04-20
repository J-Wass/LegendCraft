using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public static Infection GetInstance(World world)
        {
            if (instance == null)
            {
                world_ = world;
                instance = new Infection();
                startTime = DateTime.Now;
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
            }
            return instance;
        }

        public static void Start()
        {
            world_.gameMode = GameMode.Infection; //set the game mode
            Scheduler.NewTask(t => world_.Players.Message("&WInfection &fwill be starting in {0} seconds: &WGet ready!", timeDelay))
                .RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), 1);
        }

        public static void Stop(Player p) //for stopping the game early
        {
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
            if (isOn && (DateTime.Now - lastChecked).TotalSeconds > 10) //check if players left the world, forfeits if no players of that team left
            {
                if (world_.Players.Count(player => player.Info.isInfected) == world_.Players.Count())
                {
                    world_.Players.Message("The Zombies have won!!!");
                    for (int i = 0; i < InfectionPlayers.Count(); i++)
                    {
                        string p1 = InfectionPlayers[i].Name.ToString();
                        PlayerInfo pI = PlayerDB.FindPlayerInfoExact(p1);
                        Player p = pI.PlayerObject;

                        if (p != null)
                        {
                            Stop(p);
                        }
                    }
                    return;
                }
                if (world_.Players.Count(player => player.Info.isInfected) == 0 && world_.Players.Count() > 0)
                {
                    world_.Players.Message("The Zombies have died off! The Humans win!");
                }

            }
            timeLeft = Convert.ToInt16(((timeDelay + timeLimit) - (DateTime.Now - startTime).TotalSeconds));

            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.9 && timeLeft <= timeLimit)
            {
                world_.Players.Message("There are currently {0} humans and {1} zombies left on {2}", world_.Players.Count() - world_.Players.Count(player => player.Info.isInfected), world_.Players.Count(player => player.Info.isInfected), world_.ClassyName);
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
            infected.Info.isInfected = true;
            infected.Info.oldname =  infected.Info.DisplayedName;
            infected.Info.DisplayedName = "&cINFECTED";
        }

        public static void RevertGame() //Reset game bools/stats and stop timers
        {
            world_.gameMode = GameMode.NULL;
            task_.Stop();
            isOn = false;
            instance = null;
            world_ = null;

        }
    }
}

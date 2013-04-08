using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    class TeamDeathMatch
    {
        public const string redTeam = "&C-Red-";
        public const string blueTeam = "&1*Blue*";
        private static World world_;

        public static int blueScore = 0;
        public static int redScore = 0;
        private static int playerCount = 0;
        public static int redTeamCount = 0;
        public static int blueTeamCount = 0;
        public static int timeLeft = 0;

        private static SchedulerTask task_;
        public static TeamDeathMatch instance;
        public static DateTime startTime;
        public static DateTime lastChecked;
        
        public static bool isOn = false;
        private static bool started = false;

        private TeamDeathMatch()
        {

        }

        public static TeamDeathMatch GetInstance(World world)
        {
            if (instance == null)
            {
                world_ = world;
                instance = new TeamDeathMatch();
                startTime = DateTime.Now;
                playerCount = world_.Players.Count();
                task_ = new SchedulerTask(Interval, true).RunForever(TimeSpan.FromSeconds(1));
            }
            return instance;
        }

        public static void Start()
        {
            if (world_.Players.Count() < 2)
            {
                world_.Players.Message("You cannot player a game with less then 2 people!");
                return;
            }
            world_.gameMode = GameMode.TeamDeathMatch; //set the game mode
            List<Player> TDPlayers = new List<Player>(Server.Players.Where(p => p.World.gameMode == GameMode.TeamDeathMatch).ToArray());
            playerCount = TDPlayers.Count(); //count all players
            Scheduler.NewTask(t => world_.Players.Message("&WTEAM DEATHMATCH &fwill be starting soon: &WGet ready!"))
                .RunRepeating(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), 1);
        }

        public static void Stop(Player p) //for stopping the game early
        {
            if (p != null && world_ != null)
            {
                world_.Players.Message("{0}&S stopped the game of Team Deathmatch on world {1}",
                    p.ClassyName, world_.ClassyName);
            }
            RevertGame();          
            return;
        }

        public static Random rand = new Random();
        public static void Interval(SchedulerTask task)
        {
            //check to stop Interval
            if (world_.gameMode != GameMode.TeamDeathMatch || world_ == null)
            {
                world_ = null;
                task.Stop();
                return;
            }
            if (!started)
            {
                if (startTime != null && (DateTime.Now - startTime).TotalSeconds > 20)
                {
                    /*if (world_.Players.Length < 3){
                        world_.Players.Message("&WThe game failed to start: 2 or more players need to be in the world");
                        Stop(null);
                        return;
                    }*/
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
                        assignTeams(p);
                    }
                    started = true;
                    isOn = true;
                    if (!world_.gunPhysics)
                    {
                        world_.EnableGunPhysics(Player.Console, true);
                    }
                    lastChecked = DateTime.Now;
                    return;
                }
            }

            //check if one of the teams have won
            if (redScore >= 50)
            {
                world_.Players.Message("&sThe &cRed Team&s has won {1} to {0}.", blueScore, redScore);
                Stop(null);
                return;
            }
            if (blueScore >= 50)
            {
                world_.Players.Message("&sThe &1Blue Team&s has won {1} to {0}.", redScore, blueScore);
                Stop(null);
                return;
            }
            if (blueScore == 50 && redScore == 50) //if they somehow manage to tie which I am pretty sure is impossible
            {
                world_.Players.Message("&sThe teams have tied!");
                Stop(null);
                return;
            }

            //check if 2 minutes is up (300s from start - 20s waiting time = 320)
            if (started && startTime != null && (DateTime.Now - startTime).TotalSeconds >= 320)
            {
                if (redScore > blueScore)
                {
                    world_.Players.Message("&sThe &cRed Team&s has won {0} to {1}.", redScore, blueScore);
                    Stop(null);
                    return;
                }
                if (redScore < blueScore)
                {
                    world_.Players.Message("&sThe &1Blue Team&s has won {0} to {1}.", blueScore, redScore);
                    Stop(null);
                    return;
                }
                if (redScore == blueScore)
                {
                    world_.Players.Message("&sThe teams tied {0} to {1}!", blueScore, redScore);
                    Stop(null);
                    return;
                }
                if (world_.Players.Count() <= 1)
                {
                    Stop(null);
                    return;
                }
            }

            if (started && (DateTime.Now - lastChecked).TotalSeconds > 10)
            {
                int redCount = world_.Players.Where(p => p.Info.isOnRedTeam).ToArray().Count();
                int blueCount = world_.Players.Where(p => p.Info.isOnBlueTeam).ToArray().Count();
                if (blueCount < 1 || redCount < 1)
                {
                    if (blueTeamCount == 0)
                    {
                        if (world_.Players.Count() >= 1)
                        {
                            world_.Players.Message("&1Blue Team &shas forfeited the game. &cRed Team &swins!");
                        }
                        Stop(null);
                        return;
                    }
                    if (redTeamCount == 0)
                    {
                        if (world_.Players.Count() >= 1)
                        {
                            world_.Players.Message("&cRed Team &shas forfeited the game. &1Blue Team &swins!");
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
            timeLeft = Convert.ToInt16((320 - (DateTime.Now - startTime).TotalSeconds));
            //Keep the players updated about the score
            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.9 && timeLeft <= 300)
            {               
                if (redScore > blueScore)
                {
                    world_.Players.Message("&sThe &cRed Team&s is winning {0} to {1}.", redScore, blueScore);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                if (redScore < blueScore)
                {
                    world_.Players.Message("&sThe &1Blue Team&s is winning {0} to {1}.", blueScore, redScore);
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                if (redScore == blueScore)
                {
                    world_.Players.Message("&sThe teams are tied at {0}!", blueScore); 
                    world_.Players.Message("&fThere are &W{0}&f seconds left in the game.", timeLeft);
                }
                lastChecked = DateTime.Now;
            }
            if (timeLeft == 10)
            {
                world_.Players.Message("&WOnly 10 seconds left!");
            }
        }

        static public void assignTeams(Player p)
        {
            if (redTeamCount == 0) //if there are no players assigned to any team yet
            {
                p.Message("Let the games Begin! Type &H/Gun"); //start message
                p.Message("You are on the &cRed Team"); //notifies the player of their team
                p.iName = Color.Red + p.Name;   //the name above their head will be colored blue or red depending on team
                p.Info.oldname = p.Info.DisplayedName;
                p.Info.DisplayedName = "&f(" + redTeam + "&f) " + Color.Red + p.Name;   //changes their displayed name to include a little tag and their real mc username colored their team color
                p.Info.isOnRedTeam = true;      //assigns to red team
                p.Info.isOnBlueTeam = false;
                p.Info.isPlayingTD = true;      //they are now officially "playing TD"
                p.entityChanged = true;         //reloads skin and name above head to become the new ones
                p.Info.gameKills = 0;
                p.Info.gameDeaths = 0;
                redTeamCount++;                 //add 1 to the red or blue team player count
                return;
            }
            if (blueTeamCount < redTeamCount) //if the red team has more players and the red team has already been assigned at least one player
            {
                p.Message("Let the games Begin! Type &H/Gun");
                p.Message("You are on the &1Blue Team");
                p.iName = Color.Navy + p.Name;
                p.Info.oldname = p.Info.DisplayedName;
                p.Info.DisplayedName = "&f(" + blueTeam + "&f) " + Color.Navy + p.Name;
                p.Info.isOnBlueTeam = true;
                p.Info.isOnRedTeam = false;
                p.Info.isPlayingTD = true;
                p.entityChanged = true;
                p.Info.gameKills = 0;
                p.Info.gameDeaths = 0;
                blueTeamCount++;
                return;
            }
            if (blueTeamCount > redTeamCount)   //if the blue team has more players than the red team (shouldn't be necessary but in case of bugs)
            {
                p.Message("Let the games Begin! Type &H/Gun");
                p.Message("You are on the &cRed Team"); //notifies the player of their team
                p.iName = Color.Red + p.Name;   //the name above their head will be colored blue or red depending on team
                p.Info.oldname = p.Info.DisplayedName;
                p.Info.DisplayedName = "&f(" + redTeam + "&f) " + Color.Red + p.Name;   //changes their displayed name to include a little tag and their real mc username colored their team color
                p.Info.isOnRedTeam = true;      //assigns to red team
                p.Info.isOnBlueTeam = false;
                p.Info.isPlayingTD = true;      //they are now officially "playing TD"
                p.entityChanged = true;         //reloads skin and name above head to become the new ones
                p.Info.gameKills = 0;           //resets their kills from last game
                p.Info.gameDeaths = 0;          //resets their deaths from last game
                redTeamCount++;                 //add 1 to the red or blue team player count
                return;
            }
            if (blueTeamCount == redTeamCount)  //if the teams have the same number of players, by default the next player will be assigned to the red team
            {
                p.Message("Let the games Begin! Type &H/Gun");
                p.Message("You are on the &1Blue Team");
                p.iName = Color.Navy + p.Name;
                p.Info.oldname = p.Info.DisplayedName;
                p.Info.DisplayedName = "&f(" + blueTeam + "&f) " + Color.Navy + p.Name;
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

        public static void RevertGame()
        {
            world_.gameMode = GameMode.NULL;
            if (world_.gunPhysics)
            {
                world_.DisableGunPhysics(Player.Console, true);
            }
            task_ = null;
            isOn = false;
            instance = null;
            started = false;
            redScore = 0;
            blueScore = 0;
            playerCount = 0;
            redTeamCount = 0;
            blueTeamCount = 0;
            RevertNames();
        }
        
        public static void RevertNames()
        {
            List<PlayerInfo> TDPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => r.isOnBlueTeam || r.isOnRedTeam).ToArray());
            for (int i = 0; i < TDPlayers.Count(); i++)
            {
                string p1 = TDPlayers[i].Name.ToString();
                Player p = Server.FindPlayers(p1, false)[0];

                p.iName = null;
                p.Info.DisplayedName = p.Info.oldname;
                p.Info.isOnRedTeam = false;
                p.Info.isOnBlueTeam = false;
                p.Info.isPlayingTD = false;
                p.entityChanged = true;
                if (p.IsOnline)
                {
                    p.Message("Your status has been reverted.");
                }
            }
        }
    }
}

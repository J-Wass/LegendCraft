//This game was created by DingusBungus 2013 for use with LegendCraft servers. The code was based on the template used in ZombieGame.cs 
//written by Jonty800. Thanks to everyone who helped me test the game and work out the bugs. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft.Games
{
    public class TeamDeathMatch
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
        public static TeamDeathMatch instance;
        public static DateTime startTime;
        public static DateTime lastChecked;

        //customization (initialized as defaults)
        public static int timeLimit = 300; 
        public static int scoreLimit = 50;
        public static int timeDelay = 20;
        
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
            }
            return instance;
        }

        public static void Start()
        {
            world_.gameMode = GameMode.TeamDeathMatch; //set the game mode
            Scheduler.NewTask(t => world_.Players.Message("&WTEAM DEATHMATCH &fwill be starting in {0} seconds: &WGet ready!", timeDelay))
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
                if (world_.Players.Count() < 2) //in case players leave the world or disconnect during the start delay
                {
                    world_.Players.Message("&WTeam DeathMatch&s requires at least 2 people to play.");
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
                        assignTeams(p); //assigns teams (who knew?)
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

            //check if one of the teams have won
            if (redScore >= scoreLimit)
            {
                world_.Players.Message("&fThe &cRed Team&f has won {1} to {0}!", blueScore, redScore);
                Stop(null);
                return;
            }
            if (blueScore >= scoreLimit)
            {
                world_.Players.Message("&fThe &1Blue Team&f has won {1} to {0}!", redScore, blueScore);
                Stop(null);
                return;
            }
            if (blueScore == scoreLimit && redScore == scoreLimit) //if they somehow manage to tie which I am pretty sure is impossible
            {
                world_.Players.Message("&fThe teams have tied!");
                Stop(null);
                return;
            }

            //check if time is up (delay time + start time)
            if (started && startTime != null && (DateTime.Now - startTime).TotalSeconds >= (timeDelay + timeLimit))
            {
                if (redScore > blueScore)
                {
                    world_.Players.Message("&fThe &cRed Team&f has won {0} to {1}.", redScore, blueScore);
                    Stop(null);
                    return;
                }
                if (redScore < blueScore)
                {
                    world_.Players.Message("&fThe &1Blue Team&f has won {0} to {1}.", blueScore, redScore);
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
            if (lastChecked != null && (DateTime.Now - lastChecked).TotalSeconds > 29.9 && timeLeft <= timeLimit)
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
            if (redTeamCount == 0) //if there are no players assigned to any team yet
            {
                p.Message("Let the games Begin! Type &H/Gun"); 
                p.Message("You are on the &cRed Team"); 
                p.iName = Color.Red + p.Name;
                p.Info.tdmOldName = p.Info.DisplayedName;
                p.Info.DisplayedName = "&f(" + redTeam + "&f) " + Color.Red + p.Name;   
                p.Info.isOnRedTeam = true;      
                p.Info.isOnBlueTeam = false;
                p.Info.isPlayingTD = true;      
                p.entityChanged = true;         
                p.Info.gameKills = 0;
                p.Info.gameDeaths = 0;
                redTeamCount++;                 
                return;
            }
            if (blueTeamCount < redTeamCount) //if the red team has more players and the red team has already been assigned at least one player
            {
                p.Message("Let the games Begin! Type &H/Gun");
                p.Message("You are on the &1Blue Team");
                p.iName = Color.Navy + p.Name;
                p.Info.tdmOldName = p.Info.DisplayedName;
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
                p.Info.tdmOldName = p.Info.DisplayedName;
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
                p.Info.tdmOldName = p.Info.DisplayedName;
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

        public static void RevertGame() //Reset game bools/stats and stop timers
        {
            world_.gameMode = GameMode.NULL;
            if (world_.gunPhysics)
            {
                world_.DisableGunPhysics(Player.Console, true);
            }
            task_.Stop();
            isOn = false;
            instance = null;
            started = false;
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
                    pI.DisplayedName = pI.tdmOldName;
                    pI.isOnRedTeam = false;
                    pI.isOnBlueTeam = false;
                    pI.isPlayingTD = false;
                    p.entityChanged = true;
                    if (p.IsOnline)
                    {
                        p.Message("Your status has been reverted.");
                    }
                }
            }
        }
    }
}

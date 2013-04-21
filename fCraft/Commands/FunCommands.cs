//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AIMLbot;
using RandomMaze;

namespace fCraft
{
    internal static class FunCommands
    {
        internal static void Init()
        {
            CommandManager.RegisterCommand(CdRandomMaze);
            CommandManager.RegisterCommand(CdMazeCuboid);
            CommandManager.RegisterCommand(CdFirework);
            CommandManager.RegisterCommand(CdLife);
            CommandManager.RegisterCommand(CdPossess);
            CommandManager.RegisterCommand(CdUnpossess);
            CommandManager.RegisterCommand(CdThrow);
            CommandManager.RegisterCommand(CdInsult);
            CommandManager.RegisterCommand(CdStatistics);
            CommandManager.RegisterCommand(CdTeamDeathMatch);
            CommandManager.RegisterCommand(CdInfection);
            Player.Moving += PlayerMoved;
        }

        public static void PlayerMoved(object sender, fCraft.Events.PlayerMovingEventArgs e)
        {
            if (e.Player.Info.IsFrozen || e.Player.SpectatedPlayer != null || !e.Player.SpeedMode)
                return;
            Vector3I oldPos = e.OldPosition.ToBlockCoords();
            Vector3I newPos = e.NewPosition.ToBlockCoords();
            //check if has moved 1 whole block
            if (newPos.X == oldPos.X + 1 || newPos.X == oldPos.X-1 || newPos.Y == oldPos.Y + 1 || newPos.Y == oldPos.Y-1)
            {
                Server.Players.Message("Old: " + newPos.ToString());
                Vector3I move = newPos - oldPos;
                int AccelerationFactor = 4;
                Vector3I acceleratedNewPos = oldPos + move * AccelerationFactor;
                //do not forget to check for all the null pointers here - TODO
                Map m = e.Player.World.Map;
                //check if can move through all the blocks along the path
                Vector3F normal = move.Normalize();
                Vector3I prevBlockPos = e.OldPosition.ToBlockCoords();
                for (int i = 1; i <= AccelerationFactor * move.Length; ++i)
                {
                    Vector3I pos = (oldPos + i * normal).Round();
                    if (prevBlockPos == pos) //didnt yet hit the next block
                        continue;
                    if (!m.InBounds(pos) || m.GetBlock(pos) != Block.Air) //got out of bounds or some solid block
                    {
                        acceleratedNewPos = (oldPos + normal * (i - 1)).Round();
                        break;
                    }
                    prevBlockPos = pos;
                }
                //teleport keeping the same orientation
                Server.Players.Message("New: "+ acceleratedNewPos.ToString());
                e.Player.Send(PacketWriter.MakeSelfTeleport(new Position((short)(acceleratedNewPos.X * 32), (short)(acceleratedNewPos.Y * 32), e.Player.Position.Z, e.NewPosition.R, e.NewPosition.L)));
            }
        }

        static readonly CommandDescriptor CdInfection = new CommandDescriptor
        {
            Name = "Infection",            
            Aliases = new[] { "ZombieSurvival", "zs" },
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/Infection [start | stop]",
            Help = "Manage the Infection Gamemode!",
            Handler = InfectionHandler
        };

        private static void InfectionHandler(Player player, Command cmd)       
        {
            string Option = cmd.Next();
            World world = player.World;

            if (string.IsNullOrEmpty(Option))
            {
                CdInfection.PrintUsage(player);
                return;
            }
            if (Option.ToLower() == "start")    
            {
                if (world == WorldManager.MainWorld)
                {
                    player.Message("&SInfection games cannot be played on the main world");
                    return;
                }
                if (world.gameMode != GameMode.NULL)
                {
                    player.Message("&SThere is already a game going on");
                    return;
                }
                if (player.World.CountPlayers(true) < 2)
                {
                    player.Message("&SThere must be at least &W2&S players on this world to play Infection");
                    return;
                }
                else
                {
                    try
                    {
                        fCraft.Games.Infection.GetInstance(player.World);
                    }
                    catch(Exception e)
                    {
                        Logger.Log(LogType.Warning, "Found Exception:" + e);
                    }
                    fCraft.Games.Infection.Start();
                    return;
                }
            }
            if (Option.ToLower() == "stop")
            {
                if (world.gameMode == GameMode.Infection)
                {
                    fCraft.Games.Infection.Stop(player);
                    return;
                }
                else
                {
                    player.Message("&SNo games of Infection are going on.");
                    return;
                }
            }
            else
            {
                CdInfection.PrintUsage(player);
                return;
            }
        }
        static readonly CommandDescriptor CdTeamDeathMatch = new CommandDescriptor
        {
            Name = "TeamDeathMatch",            //I think I resolved all of the bugs...
            Aliases = new[] { "td", "tdm" },
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/TeamDeathMatch [Start | Stop | Time | Score | ScoreLimit | TimeLimit | TimeDelay | About | Help]",
            Help = "Manage the TDM Gamemode!",
            Handler = TDHandler
        };

        private static void TDHandler(Player player, Command cmd)       //For TDM Game: starting/ending game, customizing game options, viewing score, etc.
        {
            string Option = cmd.Next();
            World world = player.World;

            if (string.IsNullOrEmpty(Option))
            {
                CdTeamDeathMatch.PrintUsage(player);
                return;
            }
            if (Option.ToLower() == "start" || Option.ToLower() == "on")    //starts the game
            {
                if (world == WorldManager.MainWorld)
                {
                    player.Message("TDM games cannot be played on the main world");
                    return;
                }
                if (world.gameMode != GameMode.NULL)
                {
                    player.Message("There is already a game going on");
                    return;
                }
                if (player.World.CountPlayers(true) < 2)
                {
                    player.Message("There needs to be at least &W2&S players to play TDM");
                    return;
                }
                else
                {
                    fCraft.Games.TeamDeathMatch.GetInstance(player.World);
                    fCraft.Games.TeamDeathMatch.Start();
                    return;
                }
            }
            if (Option.ToLower() == "stop" || Option.ToLower() == "off") //stops the game
            {
                if (fCraft.Games.TeamDeathMatch.isOn)
                {
                    fCraft.Games.TeamDeathMatch.Stop(player);
                    return;
                }
                else
                {
                    player.Message("No games of Team DeathMatch are going on");
                    return;
                }
            }
            if (!fCraft.Games.TeamDeathMatch.isOn && (Option.ToLower() == "timelimit" || Option.ToLower() == "scorelimit" || Option.ToLower() == "timedelay"))
            {
                if (Option.ToLower() == "timelimit")    //option to change the length of the game (5m default)
                {
                    string time = cmd.Next();
                    if (time == null)
                    {
                        player.Message("Use the syntax: /TD timelimit (whole number of minutes)\n&HNote: The acceptable times are from 1-20 minutes");
                        return;
                    }
                    int timeLimit = 0;
                    bool parsed = Int32.TryParse(time, out timeLimit);
                    if (!parsed)
                    {
                        player.Message("Enter a whole number of minutes. For example: /TD timelimit 5");
                        return;
                    }
                    if (timeLimit < 1 || timeLimit > 20)
                    {
                        player.Message("The accepted times are between 1 and 20 minutes");
                        return;
                    }
                    else
                    {
                        fCraft.Games.TeamDeathMatch.timeLimit = (timeLimit * 60);
                        player.Message("The time limit has been changed to &W{0}&S minutes", timeLimit);
                        return;
                    }
                }
                if (Option.ToLower() == "timedelay")    //option to set the time delay for TDM games (20s default)
                {
                    string time = cmd.Next();
                    if (time == null)
                    {
                        player.Message("Use the syntax: /TD timedelay (whole number of seconds)\n&HNote: The acceptable times incriment by 10 from 10 to 60");
                        return;
                    }
                    int timeDelay = 0;
                    bool parsed = Int32.TryParse(time, out timeDelay);
                    if (!parsed)
                    {
                        player.Message("Enter a whole number of minutes. For example: /TD timedelay 20");
                        return;
                    }
                    if (timeDelay != 10 && timeDelay != 20 && timeDelay != 30 && timeDelay != 40 && timeDelay != 50 && timeDelay != 60)
                    {
                        player.Message("The accepted times are 10, 20, 30, 40, 50, and 60 seconds");
                        return;
                    }
                    else
                    {
                        fCraft.Games.TeamDeathMatch.timeDelay = timeDelay;
                        player.Message("The time delay has been changed to &W{0}&s seconds", timeDelay);
                        return;
                    }
                }
                if (Option.ToLower() == "scorelimit")       //changes the score limit
                {
                    string score = cmd.Next();
                    if (score == null)
                    {
                        player.Message("Use the syntax: /TD scorelimit (whole number)\n&HNote: The acceptable scores are from 5-300 points");
                        return;
                    }
                    int scoreLimit = 0;
                    bool parsed = Int32.TryParse(score, out scoreLimit);
                    if (!parsed)
                    {
                        player.Message("Enter a whole number score. For example: /TD scorelimit 50");
                        return;
                    }
                    if (scoreLimit < 5 || scoreLimit > 300)
                    {
                        player.Message("The accepted scores are from 5-300 points");
                        return;
                    }
                    else
                    {
                        fCraft.Games.TeamDeathMatch.scoreLimit = scoreLimit;
                        player.Message("The score limit has been changed to &W{0}&s points", scoreLimit);
                        return;
                    }
                }
            }
            if (fCraft.Games.TeamDeathMatch.isOn && (Option.ToLower() == "timelimit" || Option.ToLower() == "scorelimit" || Option.ToLower() == "timedelay"))
            {
                player.Message("You cannot adjust game settings while a game is going on");
                return;
            }
            if (Option.ToLower() == "score")       //scoreboard for the matchs, different messages for when the game has ended. //td score
            {
                int red = fCraft.Games.TeamDeathMatch.redScore;
                int blue = fCraft.Games.TeamDeathMatch.blueScore;

                if (red > blue)
                {
                    if (player.Info.isOnRedTeam)
                    {
                        player.Message("&sYour team is winning {0} to {1}", red, blue);
                        return;
                    }
                    if (player.Info.isOnBlueTeam)
                    {
                        player.Message("&sYour team is losing {0} to {1}", red, blue);
                        return;
                    }
                    else
                    {
                        player.Message("&sThe &cRed Team&s won {0} to {1}", red, blue);
                        return;
                    }
                }
                if (red < blue)
                {
                    if (player.Info.isOnBlueTeam)
                    {
                        player.Message("&sYour team is winning {0} to {1}", blue, red);
                        return;
                    }
                    if (player.Info.isOnRedTeam)
                    {
                        player.Message("&sYour team is losing {0} to {1}", blue, red);
                        return;
                    }
                    else
                    {
                        player.Message("&sThe &1Blue Team&s won {0} to {1}", blue, red);
                        return;
                    }
                }
                if (red == blue)
                {
                    if (player.Info.isPlayingTD)
                    {
                        player.Message("&sThe teams are tied at {0}!", blue);
                        return;
                    }
                    else
                    {
                        player.Message("&sThe teams tied at {0}!", blue);
                        return;
                    }
                }
            }
            if (Option.ToLower() == "about")    //td about
            {
                player.Message("&cTeam Deathmatch&S is a team game where all players are assigned to a red or blue team. Players cannot shoot players on their own team. The game will start the gun physics for you. The game keeps score and notifications come up about the score and time left every 30 seconds. The Score Limit, Time Delay and Time Limit are customizable. Detailed help is on &H/TD Help"
                + "\n&SDeveloped for &5Legend&WCraft&S by &fDingus&0Bungus&S 2013 - Based on the template of ZombieGame.cs written by Jonty800.");
                return;
            }
            if (Option.ToLower() == "settings") //shows the current settings for the game (time limit, time delay, score limit)
            {
                player.Message("The Current Settings For TDM: Time Delay: &c{0}&ss | Time Limit: &c{1}&sm | Score Limit: &c{2}&s points",
                    fCraft.Games.TeamDeathMatch.timeDelay, (fCraft.Games.TeamDeathMatch.timeLimit / 60), fCraft.Games.TeamDeathMatch.scoreLimit);
                return;
            }
            if (Option.ToLower() == "help") //detailed help for the cmd
            {
                player.Message("Showing Option Descriptions for /TD (Option):\n&HTime &f- Tells how much time left in the game"
                + "\n&HScore &f- Tells the score of the current game(or last game played)"
                + "\n&HScoreLimit [number(5-300)] &f- Sets the score at which the game will end (Enter Whole Numbers from 5-300)"
                + "\n&HTimeLimit [time(m)] &f- Sets the time at which the game will end (Enter whole minutes from 1-15)"
                + "\n&HTimeDelay [time(s)] &f- Sets the time delay at the beginning of the match (Enter 10 second incriments from 10-60)"
                + "\n&HSettings&f - Shows the current TDM settings"
                + "\n&HAbout &f- General Game Description and Credits"
                + "\n&HDefaults&f: TimeDelay: 20s, TimeLimit: 5m, ScoreLimit 50");
                return;
            }
            if (Option.ToLower() == "time" || Option.ToLower() == "timeleft")
            {
                if (player.Info.isPlayingTD)
                {
                    player.Message("&fThere are &W{0}&f seconds left in the game.", fCraft.Games.TeamDeathMatch.timeLeft);
                    return;
                }
                else
                {
                    player.Message("&fThere are no games of Team DeathMatch going on.");
                    return;
                }
            }
            else
            {
                CdTeamDeathMatch.PrintUsage(player);
                return;
            }
        }

        static readonly CommandDescriptor CdStatistics = new CommandDescriptor
        {
            Name = "Statistics",
            Aliases = new[] { "stats" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = false,
            Usage = "/Stats (AllTime|Top10Kills|Top10Deaths|Help)\n&HNote: Leave Blank For Current Game Stats.",
            Handler = StatisticsHandler
        };

        private static void StatisticsHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            double TDMKills = player.Info.gameKills;    //for use in division (for precision)
            double TDMDeaths = player.Info.gameDeaths;

            if (string.IsNullOrEmpty(option)) //user does /stats
            {
                double gameKDR = 0;
                if (player.Info.gameDeaths == 0 && player.Info.gameKills == 0)
                {
                    gameKDR = 0;
                }
                else if (player.Info.gameKills == 0 && player.Info.gameDeaths > 0)
                {
                    gameKDR = 0;
                }
                else if (player.Info.gameDeaths == 0 && player.Info.gameKills > 0)
                {
                    gameKDR = player.Info.gameKills;
                }
                else if (player.Info.gameDeaths > 0 && player.Info.gameKills > 0)
                {
                    gameKDR = TDMKills / TDMDeaths;
                }
                if (player.Info.isPlayingTD)
                {
                    player.Message("&sYou have &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio is &W{2:0.00}&s.", player.Info.gameKills, player.Info.gameDeaths, gameKDR);
                }
                else
                {
                    player.Message("&sYou had &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio was &W{2:0.00}&s.", player.Info.gameKills, player.Info.gameDeaths, gameKDR);
                }
                return;
            }
            else
            {
                switch (option.ToLower())
                {
                    default:
                        CdStatistics.PrintUsage(player);
                        return;

                    case "alltime": //user does /stats alltime

                        double allKills = player.Info.totalKillsTDM;
                        double allDeaths = player.Info.totalDeathsTDM; //for use in the division for KDR (int / int = int, so no precision), why we convert to double here
                        double totalKDR = 0;

                        if (player.Info.totalDeathsTDM == 0 && player.Info.totalKillsTDM == 0)
                        {
                            totalKDR = 0;
                        }
                        else if (player.Info.totalDeathsTDM == 0 && player.Info.totalKillsTDM > 0)
                        {
                            totalKDR = player.Info.totalKillsTDM;
                        }
                        else if (player.Info.totalKillsTDM == 0 && player.Info.totalDeathsTDM > 0)
                        {
                            totalKDR = 0;
                        }
                        else if (player.Info.totalKillsTDM > 0 && player.Info.totalDeathsTDM > 0)
                        {
                            totalKDR = allKills / allDeaths;
                        }
                        player.Message("&sIn all &WTeam Deathmatch&S games you have played, you have gotten: &W{0}&S Kills and &W{1}&s Deaths giving you a Kill/Death ratio of &W{2:0.00}&S.",
                                        player.Info.totalKillsTDM, player.Info.totalDeathsTDM, totalKDR);
                        return;

                    case "topkills": //user does /stats topkills
                        List<PlayerInfo> TDPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.ToArray().OrderBy(r => r.totalKillsTDM).Reverse());
                        player.Message("&HShowing the players with the most all-time TDM Kills:");
                        if (TDPlayers.Count() < 10)
                        {
                            for (int i = 0; i < TDPlayers.Count(); i++)
                            {
                                player.Message("{0}&s - {1} Kills", TDPlayers[i].ClassyName, TDPlayers[i].totalKillsTDM);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                player.Message("{0}&s - {1} Kills", TDPlayers[i].ClassyName, TDPlayers[i].totalKillsTDM);
                            }
                        }
                        return;

                    case "topdeaths": //user does /stats topdeaths
                        List<PlayerInfo> TDPlayers2 = new List<PlayerInfo>(PlayerDB.PlayerInfoList.ToArray().OrderBy(r => r.totalDeathsTDM).Reverse());
                        player.Message("&HShowing the players with the most all-time TDM Deaths:");
                        if (TDPlayers2.Count() < 10)
                        {
                            for (int i = 0; i < TDPlayers2.Count(); i++)
                            {
                                player.Message("{0}&s - {1} Deaths", TDPlayers2[i].ClassyName, TDPlayers2[i].totalDeathsTDM);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                player.Message("{0}&s - {1} Deaths", TDPlayers2[i].ClassyName, TDPlayers2[i].totalDeathsTDM);
                            }
                        }
                        return;

                    case "help": //user does /stats help
                        player.Message("&HDetailed help for the /Stats (options):");
                        player.Message("&HAllTime&S - Shows your all time TDM stats.");
                        player.Message("&HTop10Kills&S - Starts a game of Team Deathmatch");
                        player.Message("&HTop10Deaths&S - show the players with the all time most Kills and Deaths.");
                        player.Message("&HNote: Leave Blank For Current Game Stats");
                        return;
                }
            }
        }



        static readonly CommandDescriptor CdInsult = new CommandDescriptor
        {
            Name = "Insult",
            Aliases = new string[] { "MakeFun", "MF" },
            Category = CommandCategory.Chat | CommandCategory.Fun,
            Permissions = new Permission[] { Permission.HighFive },
            IsConsoleSafe = true,
            Usage = "/Insult playername",
            Help = "Takes a random insult from a list and insults a player.",
            NotRepeatable = true,
            Handler = InsultHandler,
        };

        static void InsultHandler(Player player, Command cmd)
        {
            List<String> insults;
            string name = cmd.Next();
            Random randomizer = new Random();

            insults = new List<String>()
            {
                "{0}&s shit on {1}&s's mom's face.",
                "{0}&s spit in {1}&s's drink.",
                "{0}&s threw a chair at {1}&s.",
                "{0}&s rubbed their ass on {1}&s",
                "{0}&s flicked off {1}&s.",
                "{0}&s pulled down their pants and flashed {1}&s.",
                "{0}&s went into {1}&s's house on their birthday, locked them in the closet, and ate their birthday dinner.",
                "{0}&s went up to {1}&s and said 'mama, mama, mama, mama, mommy, mommy, mommy, mommy, ma, ma, ma, ma, mum, mum, mum, mum. Hi! hehehehe'",
                "{0}&s asked {1}&s if they were single, just to hear them say a painful 'yes'...",
                "{0}&s shoved a pineapple up {1}&s's ass",
                "{0}&s beat {1}&s with a cane.",
                "{0}&s put {1}&s in a boiling pot and started chanting.",
                "{0}&s ate cheetos then wiped their hands all over {1}&s's white clothes",
                "{0}&s sprayed {1}&s's crotch with water, then pointed and laughed.",
                "{0}&s tied up {1}&s and ate their last candy bar right in front of them.",
                "{0}&s gave {1}&s a wet willy.",
                "{0}&s gave {1}&s a wedgie.",
                "{0}&s gave {1}&s counterfeit money and then called the Secret Service on them.",
                "{0}&s beats {1}&s with a statue of Dingus.",
                "{0}&s shot {1}&s in the knee with an arrow.",
                "{0}&s called {1}&s a disfigured, bearded clam.",
                "{0}&s flipped a table onto {1}&s.",
                "{0}&s smashed {1}&s over the head with their vintage record.",
                "{0}&s dropped a piano on {1}&s.",
                "{0}&s burned {1}&s with a cigarette.",
                "{0}&s incinerated {1}&s with a Kamehameha!"
            };

            int index = randomizer.Next(0, insults.Count); 
            double time = (DateTime.Now - player.Info.LastUsedInsult).TotalSeconds;

            if (name == null || name.Length < 1)
            { 
                player.Message("/Insult (PlayerName)");
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null)
                return;
            if (target == player)
            {
                player.Message("You cannot /insult yourself.");
                return;
            }
            double timeLeft = Math.Round(20 - time);
            if (time < 20)
            {
                player.Message("You cannot use this command for another " + timeLeft + " second(s).");
                return;
            }
            else
            {
                Server.Message(insults[index], player.ClassyName, target.ClassyName);
                player.Info.LastUsedInsult = DateTime.Now;
                return;
            }
            
        }

      


        static readonly CommandDescriptor CdThrow = new CommandDescriptor
            {
                Name = "Throw",
                Aliases = new string[] { "Toss" },
                Category = CommandCategory.Chat | CommandCategory.Fun,
                Permissions = new Permission[] { Permission.Mute },
                IsConsoleSafe = true,
                Usage = "/Throw playername",
                Help = "Throw's a player.",
                NotRepeatable = true,
                Handler = ThrowHandler,
            };
  
        static void ThrowHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            string item = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;
            if (target.Immortal)
            {
                player.Message("&SYou failed to throw {0}&S, they are immortal", target.ClassyName);
                return;
            }
            if (target == player)
            {
                player.Message("&sYou can't throw yourself... It's just physically impossible...");
                return;
            }
            double time = (DateTime.Now - player.Info.LastUsedSlap).TotalSeconds;
            if (time < 10)
            {
                player.Message("&WYou can use /Throw again in " + Math.Round(10 - time) + " seconds.");
                return;
            }
            Random random = new Random();
            int randomNumber = random.Next(1, 4);
           

                         if (randomNumber == 1)
                            if (player.Can(Permission.Slap, target.Info.Rank))
                            {
                                Position slap = new Position(target.Position.Z, target.Position.X, (target.World.Map.Bounds.YMax) * 32);
                                target.TeleportTo(slap);
                                Server.Players.CanSee(target).Except(target).Message("&SPlayer {0}&S was &eThrown&s by {1}&S.", target.ClassyName, player.ClassyName);
                                IRC.PlayerSomethingMessage(player, "thrown", target, null);
                                target.Message("&sYou were &eThrown&s by {0}&s.", player.ClassyName);
                                return;
                            }
                            else
                            {
                                player.Message("&sYou can only Throw players ranked {0}&S or lower",
                                               player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                            }
                       
                    
                       
                         if (randomNumber == 2)
                            if (player.Can(Permission.Slap, target.Info.Rank))
                            {
                                Position slap = new Position(target.Position.X, target.Position.Z, (target.World.Map.Bounds.YMax) * 32);
                                target.TeleportTo(slap);
                                Server.Players.CanSee(target).Except(target).Message("&sPlayer {0}&s was &eThrown&s by {1}&s.", target.ClassyName, player.ClassyName);
                                IRC.PlayerSomethingMessage(player, "thrown", target, null);
                                target.Message("&sYou were &eThrown&s by {0}&s.", player.ClassyName);
                                return;
                            }
                            else
                            {
                                player.Message("&sYou can only Throw players ranked {0}&S or lower",
                                               player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                            }

                         if (randomNumber == 3)
                            if (player.Can(Permission.Slap, target.Info.Rank))
                            {
                                Position slap = new Position(target.Position.Z, target.Position.Y, (target.World.Map.Bounds.XMax) * 32);
                                target.TeleportTo(slap);
                                Server.Players.CanSee(target).Except(target).Message("&sPlayer {0}&s was &eThrown&s by {1}&s.", target.ClassyName, player.ClassyName);
                                IRC.PlayerSomethingMessage(player, "thrown", target, null);
                                target.Message("&sYou were &eThrown&s by {0}&s.", player.ClassyName);
                                return;
                            }
                            else
                            {
                                player.Message("&sYou can only Throw players ranked {0}&S or lower",
                                               player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                            }
                        
                          if (randomNumber == 4)
                             if (player.Can(Permission.Slap, target.Info.Rank))
                             {
                                 Position slap = new Position(target.Position.Y, target.Position.Z, (target.World.Map.Bounds.XMax) * 32);
                                 target.TeleportTo(slap);
                                 Server.Players.CanSee(target).Except(target).Message("&sPlayer {0}&s was &eThrown&s by {1}&s.", target.ClassyName, player.ClassyName);
                                 IRC.PlayerSomethingMessage(player, "thrown", target, null);
                                 target.Message("&sYou were &eThrown&s by {0}&s.", player.ClassyName);
                                 return;
                             }
                             else
                             {
                                 player.Message("&sYou can only Throw players ranked {0}&S or lower",
                                                player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                                 player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                             }
                }
            
     

        #region Possess / UnPossess

        static readonly CommandDescriptor CdPossess = new CommandDescriptor
        {
            Name = "Possess",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Possess },
            Usage = "/Possess PlayerName",
            Handler = PossessHandler
        };

        static void PossessHandler(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdPossess.PrintUsage(player);
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
            if (target == null) return;
            if (target.Immortal)
            {
                player.Message("You cannot possess {0}&S, they are immortal", target.ClassyName);
                return;
            }
            if (target == player)
            {
                player.Message("You cannot possess yourself.");
                return;
            }

            if (!player.Can(Permission.Possess, target.Info.Rank))
            {
                player.Message("You may only possess players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit(Permission.Possess).ClassyName);
                player.Message("{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName);
                return;
            }

            if (!player.Possess(target))
            {
                player.Message("Already possessing {0}", target.ClassyName);
            }
        }


        static readonly CommandDescriptor CdUnpossess = new CommandDescriptor
        {
            Name = "unpossess",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Possess },
            NotRepeatable = true,
            Usage = "/Unpossess target",
            Handler = UnpossessHandler
        };

        static void UnpossessHandler(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdUnpossess.PrintUsage(player);
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, targetName, true, true);
            if (target == null) return;

            if (!player.StopPossessing(target))
            {
                player.Message("You are not currently possessing anyone.");
            }
        }

        #endregion

        static readonly CommandDescriptor CdLife = new CommandDescriptor
        {
            Name = "Life",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Life <command> [params]",
            Help = "&SGoogle \"Conwey's Game of Life\"\n'&H/Life help'&S for more usage info\n(c) 2012 LaoTszy",
            UsableByFrozenPlayers = false,
            Handler = LifeHandlerFunc,
        };
        
        static readonly CommandDescriptor CdFirework = new CommandDescriptor
        {
            Name = "Firework",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Fireworks },
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/Firework",
            Help = "&SToggles Firework Mode on/off for yourself. " +
            "All Gold blocks will be replaced with fireworks if " +
            "firework physics are enabled for the current world.",
            UsableByFrozenPlayers = false,
            Handler = FireworkHandler
        };

        static void FireworkHandler(Player player, Command cmd)
        {
            if (player.fireworkMode)
            {
                player.fireworkMode = false;
                player.Message("Firework Mode has been turned off.");
                return;
            }
            else
            {
                player.fireworkMode = true;
                player.Message("Firework Mode has been turned on. " +
                    "All Gold blocks are now being replaced with Fireworks.");
            }
        }


        static readonly CommandDescriptor CdRandomMaze = new CommandDescriptor
        {
            Name = "RandomMaze",
            Aliases = new string[] { "3dmaze" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Choose the size (width, length and height) and it will draw a random maze at the chosen point. " +
                "Optional parameters tell if the lifts are to be drawn and if hint blocks (log) are to be added. \n(C) 2012 Lao Tszy",
            Usage = "/randommaze <width> <length> <height> [nolifts] [hints]",
            Handler = MazeHandler
        };
        static readonly CommandDescriptor CdMazeCuboid = new CommandDescriptor
        {
            Name = "MazeCuboid",
            Aliases = new string[] { "Mc", "Mz", "Maze" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a cuboid with the current brush and with a random maze inside.(C) 2012 Lao Tszy",
            Usage = "/MazeCuboid [block type]",
            Handler = MazeCuboidHandler,
        };

        private static void MazeHandler(Player p, Command cmd)
        {
            try
            {
                RandomMazeDrawOperation op = new RandomMazeDrawOperation(p, cmd);
                BuildingCommands.DrawOperationBegin(p, cmd, op);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error: " + e.Message);
            }
        }
        private static void MazeCuboidHandler(Player p, Command cmd)
        {
            try
            {
                MazeCuboidDrawOperation op = new MazeCuboidDrawOperation(p);
                BuildingCommands.DrawOperationBegin(p, cmd, op);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error: " + e.Message);
            }
        }
        private static void LifeHandlerFunc(Player p, Command cmd)
        {
        	try
        	{
                if (!cmd.HasNext)
                {
                    p.Message("&H/Life <command> <params>. Commands are Help, Create, Delete, Start, Stop, Set, List, Print");
                    p.Message("Type /Life help <command> for more information");
                    return;
                }
				LifeHandler.ProcessCommand(p, cmd);
        	}
        	catch (Exception e)
        	{
				p.Message("Error: " + e.Message);
        	}
        }

        
    }
}

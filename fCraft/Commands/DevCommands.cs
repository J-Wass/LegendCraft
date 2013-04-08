using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibNbt;
using LibNbt.Exceptions;
using LibNbt.Queries;
using LibNbt.Tags;

namespace fCraft
{
    static class DevCommands {

        public static void Init()
        {
            //CommandManager.RegisterCommand(CdDrawScheme);

            //CommandManager.RegisterCommand(CdSpell);
           // CommandManager.RegisterCommand(CdGame);
        }

        static readonly CommandDescriptor CdGame = new CommandDescriptor
        {
            Name = "Game",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Physics },
            IsConsoleSafe = false,
            Usage = "/Game TD (Start|Stop|Score|Time)\nFor Stats in TD Games, Type &H/Stats (Help|Top10Kills|Top10Deaths|AllTime)",
            Handler = GameHandler
        };

        private static void GameHandler(Player player, Command cmd)
        {
            string gameMode = cmd.Next();
            string Option = cmd.Next();
            World world = player.World;
            if (world == WorldManager.MainWorld)
            {
                player.Message("/Game cannot be used on the main world");
                return;
            }

            /*      if (GameMode.ToLower() == "zombie")
                  {
                      if (Option.ToLower() == "start")
                      {
                          ZombieGame.GetInstance(player.World);
                          ZombieGame.Start();
                          return;
                      }
                      else
                      {
                          CdGame.PrintUsage(player);
                          return;
                      }
                  }
                  if (GameMode.ToLower() == "minefield")
                  {
                      if (Option.ToLower() == "start")
                      {
                          if (WorldManager.FindWorldExact("Minefield") != null)
                          {
                              player.Message("&WA game of Minefield is currently running and must first be stopped");
                              return;
                          }
                          MineField.GetInstance();
                          MineField.Start(player);
                          return;
                      }
                      else if (Option.ToLower() == "stop")
                      {
                          if (WorldManager.FindWorldExact("Minefield") == null)
                          {
                              player.Message("&WA game of Minefield is currently not running");
                              return;
                          }
                          MineField.Stop(player, false);
                          return;
                      }
                      else
                      {
                          CdGame.PrintUsage(player);
                          return;
                      }
                  } */
            if (gameMode.ToLower() == "teamdeathmatch" || gameMode.ToLower() == "td")
            {
                if (Option.ToLower() == "start")
                {
                    if (player.World == null)
                    {
                        player.Message("You must be in a world to use this command.");
                        return;
                    }
                    if (TeamDeathMatch.isOn)
                    {
                        player.Message("There is already a Team DeathMatch game going on.");
                        return;
                    }
                    else
                    {
                        TeamDeathMatch.GetInstance(player.World);
                        TeamDeathMatch.Start();
                        return;
                    }
                }
                if (Option.ToLower() == "stop")
                {
                    TeamDeathMatch.Stop(player);
                    return;
                }
                if (Option.ToLower() == "blue" && player.World.gameMode == GameMode.TeamDeathMatch) //FOR TESTING ONLY (Will remove later)
                {

                    player.Message("You are now on the &1Blue Team"); //notifies the player of their team
                    player.iName = Color.Navy + player.Name;   //the name above their head will be colored blue or red depending on team
                    player.Info.oldname = player.Info.DisplayedName;
                    player.Info.DisplayedName = "&f(" + TeamDeathMatch.blueTeam + "&f) " + Color.Navy + player.Name;   //changes their displayed name to include a little tag and their real mc username colored their team color
                    player.Info.isOnBlueTeam = true;     //assigns to red team
                    player.Info.isOnRedTeam = false;     //a bug fix (maybe unnecessary at this point)
                    player.Info.isPlayingTD = true;      //they are now officially "playing TD"
                    player.entityChanged = true;         //reloads skin and name above head to become the new ones
                    TeamDeathMatch.blueTeamCount++;                 //add 1 to  the red team player count
                    return;
                }
                if (Option.ToLower() == "red" && player.World.gameMode == GameMode.TeamDeathMatch)  //FOR TESTING ONLY (Will remove later)
                {

                    player.Message("You are now on the &cRed Team"); //notifies the player of their team
                    player.iName = Color.Red + player.Name;   //the name above their head will be colored blue or red depending on team
                    player.Info.oldname = player.Info.DisplayedName;
                    player.Info.DisplayedName = "&f(" + TeamDeathMatch.redTeam + "&f) " + Color.Red + player.Name;   //changes their displayed name to include a little tag and their real mc username colored their team color
                    player.Info.isOnRedTeam = true;      //assigns to red team
                    player.Info.isOnBlueTeam = false;    //a bug fix (maybe unnecessary at this point)
                    player.Info.isPlayingTD = true;      //they are now officially "playing TD"
                    player.entityChanged = true;         //reloads skin and name above head to become the new ones
                    TeamDeathMatch.redTeamCount++;       //add 1 to  the red team player counter
                    return;
                }
                if (Option.ToLower() == "score")       //scoreboard for the matchs, different messages for when the game has ended. //game td score
                {
                    int red = TeamDeathMatch.redScore;
                    int blue = TeamDeathMatch.blueScore;

                    if (red > blue)
                    {
                        if (player.Info.isOnRedTeam)
                        {
                            player.Message("&sYour team is winning {0} to {1}.", red, blue);
                            return;
                        }
                        if (player.Info.isOnBlueTeam)
                        {
                            player.Message("&sYour team is losing {0} to {1}.", red, blue);
                            return;
                        }
                        else
                        {
                            player.Message("&sThe &cRed Team&s won {0} to {1}.", red, blue);
                            return;
                        }
                    }
                    if (red < blue)
                    {
                        if (player.Info.isOnBlueTeam)
                        {
                            player.Message("&sYour team is winning {0} to {1}.", blue, red);
                            return;
                        }
                        if (player.Info.isOnRedTeam)
                        {
                            player.Message("&sYour team is losing {0} to {1}.", blue, red);
                            return;
                        }
                        else
                        {
                            player.Message("&sThe &1Blue Team&s won {0} to {1}.", blue, red);
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
                    else
                    {
                        CdGame.PrintUsage(player);
                        return;
                    }
                }
                if (Option.ToLower() == "about")    //game td about
                {
                    player.Message("&cTeam Deathmatch&S is a team game where all players are assigned to a red or blue team. Players cannot shoot players on their own team. The game starts 20 seconds after the game is initiated. the game will start the gun physics for you. The game keeps score and notifications come up about the score and time left every 30 seconds. The Game ends after 5 minutes or when one team gets 50 points. Detailed help for stats is on &H/Game TD Stats Help");
                    player.Message("&SDeveloped for &%Legend&WCraft&S by &fDingus&0Bungus&S 2013");
                    return;
                }
                if (Option.ToLower() == "time")
                {
                    if (player.Info.isPlayingTD)
                    {
                        player.Message("&fThere are &W{0}&f seconds left in the game.", TeamDeathMatch.timeLeft);
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
                    CdGame.PrintUsage(player);
                    return;
                }
            }
            else
            {
                CdGame.PrintUsage(player);
                return;
            }
        }


        static readonly CommandDescriptor CdStatistics = new CommandDescriptor
        {
            Name = "Statistics",            //buggy?
            Aliases = new[] { "stats" },
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = false,
            Usage = "/Stats (AllTime|Top10Kills|Top10Deaths|Help)\n&HNote: Leave Blank For Current Game Stats.",
            Handler = StatisticsHandler
        };

        private static void StatisticsHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            int gameKDR = 0;
            if (option == null || !cmd.HasNext)
            {
                if (player.Info.gameDeaths == 0)
                {
                    gameKDR = player.Info.gameKills;
                }
                else
                {
                    gameKDR = (player.Info.gameKills / player.Info.gameDeaths);
                }
                if (player.Info.isPlayingTD)
                {
                    player.Message("&sYou have &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio is &W{2}&s.", player.Info.gameKills, player.Info.gameDeaths, gameKDR);
                }
                else
                {
                    player.Message("&sYou had &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio was &W{2}&s.", player.Info.gameKills, player.Info.gameDeaths, gameKDR);
                }
                return;
            }
            if (cmd.HasNext)
            {
                string param = cmd.Next().ToLower();
                if (param == "alltime")     //personal alltime stats //game td stats alltime
                {
                    int tDKDR = (player.Info.totalKillsTDM / player.Info.totalDeathsTDM);
                    if (player.Info.totalDeathsTDM == 0)
                    {
                        tDKDR = player.Info.totalKillsTDM;
                    }
                    player.Message("&sIn all &WTeam Deathmatch&S games you have played, you have gotten: &W{0}&S Kills and &W{1}&s Deaths giving you a Kill/Death ratio of &W{2}&S.",
                                    player.Info.totalKillsTDM, player.Info.totalDeathsTDM, tDKDR);
                    return;
                }
                if (param == "top10kills")      //10 players with the most kills of all time //game td stats top10kills
                {
                    List<PlayerInfo> TDPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.ToArray().OrderBy(r => r.totalKillsTDM));
                    for (int i = 0; i < 10; i++)
                    {
                        player.Message("{0}&s - {1} Kills", TDPlayers[i].ClassyName, player.Info.totalKillsTDM);
                    }
                    return;
                }
                if (param == "top10deaths")     //10 players with the most deaths all time //game ts stats top10deaths
                {
                    List<PlayerInfo> TDPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.ToArray().OrderBy(r => r.totalKillsTDM));
                    for (int i = 0; i < 10; i++)
                    {
                        player.Message("{0}&s - {1} Deaths", TDPlayers[i].ClassyName, player.Info.totalDeathsTDM);
                    }
                    return;
                }
                /*   if (param == "top10kdr") unfinished
                     {
 
                     }   */
                if (param == "help")    //game td stats help
                {
                    player.Message("&HDetailed help for the /Game TD stats (options):");
                    player.Message("AllTime     - Shows your all time TDM stats.");
                    player.Message("Top10Kills  - Starts a game of Team Deathmatch");
                    player.Message("Top10Deaths - show the players with the all time most Kills and Deaths.");
                    player.Message("&HNote: Leave Blank For Current Game Stats");
                    return;
                }
                else
                {
                    player.Message("&SThe choices for &H/Game TD Stats (choice)&S are: alltime, top10kills, top10deaths, and help. (More coming soon...)");
                    return;
                }
            }
            else
            {
                player.Message("&SThe choices for &H/Game TD Stats (choice)&S are: alltime, top10kills, top10deaths, and help. (More coming soon...)");
                return;
            }

        }

      

        static readonly CommandDescriptor CdSpell = new CommandDescriptor
        {
            Name = "Spell",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Spell",
            Help = "Penis",
            UsableByFrozenPlayers = false,
            Handler = SpellHandler,
        };
        public static SpellStartBehavior particleBehavior = new SpellStartBehavior();
        internal static void SpellHandler(Player player, Command cmd)
        {
            World world = player.World;
            Vector3I pos1 = player.Position.ToBlockCoords();
            Random _r = new Random();
            int n = _r.Next(8, 12);
            for (int i = 0; i < n; ++i)
            {
                double phi = -_r.NextDouble() + -player.Position.L * 2 * Math.PI;
                double ksi = -_r.NextDouble() + player.Position.R * Math.PI - Math.PI / 2.0;

                Vector3F direction = (new Vector3F((float)(Math.Cos(phi) * Math.Cos(ksi)), (float)(Math.Sin(phi) * Math.Cos(ksi)), (float)Math.Sin(ksi))).Normalize();
                world.AddPhysicsTask(new Particle(world, (pos1 + 2 * direction).Round(), direction, player, Block.Obsidian, particleBehavior), 0);
            }
        }

        static readonly CommandDescriptor CdDrawScheme = new CommandDescriptor
        {
            Name = "DrawScheme",
            Aliases = new[] { "drs" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceAdmincrete },
            Help = "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            Handler = test
        };
        public static void test(Player player, Command cmd)
        {
            if (!File.Exists("C:/users/jb509/desktop/1.schematic"))
            {
                player.Message("Nop"); return;
            }
            NbtFile file = new NbtFile("C:/users/jb509/desktop/1.schematic");
            file.RootTag = new NbtCompound("Schematic");
            file.LoadFile();
            bool notClassic = false;
            short width = file.RootTag.Query<NbtShort>("/Schematic/Width").Value;
            short height = file.RootTag.Query<NbtShort>("/Schematic/Height").Value;
            short length = file.RootTag.Query<NbtShort>("/Schematic/Length").Value;
            Byte[] blocks = file.RootTag.Query<NbtByteArray>("/Schematic/Blocks").Value;

            Vector3I pos = player.Position.ToBlockCoords();
            int i = 0;
            player.Message("&SDrawing Schematic ({0}x{1}x{2})", length, width, height);
            for (int x = pos.X; x < width + pos.X; x++)
            {
                for (int y = pos.Y; y < length + pos.Y; y++)
                {
                    for (int z = pos.Z; z < height + pos.Z; z++)
                    {
                        if (Enum.Parse(typeof(Block), ((Block)blocks[i]).ToString(), true) != null)
                        {
                            if (!notClassic && blocks[i] > 49)
                            {
                                notClassic = true;
                                player.Message("&WSchematic used is not designed for Minecraft Classic;" +
                                                " Converting all unsupported blocks with air");
                            }
                            if (blocks[i] < 50)
                            {
                                player.WorldMap.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, (Block)blocks[i]));
                            }
                        }
                        i++;
                    }
                }
            }
            file.Dispose();
        }
    }
}

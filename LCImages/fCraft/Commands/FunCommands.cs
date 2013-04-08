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
using RandomMaze;
using AIMLbot;
using System.Threading;

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
                "{0}&s shot {1}&s in the knee with an arrow.",
                "{0}&s called {1}&s a disfigured, bearded clam.",
                "{0}&s flipped a table onto {1}&s.",
                "{0}&s smashed {1}&s over the head with their vintage record.",
                "{0}&s dropped a piano on {1}&s.",
                "{0}&s burned {1}&s with a cigarette.",
                "{0}&s incinerated {1}&s with a Kamehameha!"
            };

            int index = randomizer.Next(0, insults.Count); // (0, 18)
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
                IsConsoleSafe = false,
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

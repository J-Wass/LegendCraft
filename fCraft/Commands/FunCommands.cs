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

            CommandManager.RegisterCommand(CdFFAStatistics);
            CommandManager.RegisterCommand(CdFreeForAll);
            CommandManager.RegisterCommand(CdTDStatistics);
            CommandManager.RegisterCommand(CdTeamDeathMatch);
            CommandManager.RegisterCommand(CdInfection);
            CommandManager.RegisterCommand(CdSetModel);
            CommandManager.RegisterCommand(CdBot);
            CommandManager.RegisterCommand(CdCTF);

            Player.Moving += PlayerMoved;
        }

            public static string[] validEntities = 
            {
                "chicken",
                "creeper",
                "croc",
                "humanoid",
                "human",
                "pig",
                "printer",
                "sheep",
                "skeleton",
                "spider",
                "zombie"
                                     };

        public static void PlayerMoved(object sender, fCraft.Events.PlayerMovingEventArgs e)
        {
            if (e.Player.Info.IsFrozen || e.Player.SpectatedPlayer != null || !e.Player.SpeedMode)
                return;
            Vector3I oldPos = e.OldPosition.ToBlockCoords();
            Vector3I newPos = e.NewPosition.ToBlockCoords();
            //check if has moved 1 whole block
            if (newPos.X == oldPos.X + 1 || newPos.X == oldPos.X - 1 || newPos.Y == oldPos.Y + 1 || newPos.Y == oldPos.Y - 1)
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
                //Server.Players.Message("New: "+ acceleratedNewPos.ToString());
                e.Player.Send(PacketWriter.MakeSelfTeleport(new Position((short)(acceleratedNewPos.X * 32), (short)(acceleratedNewPos.Y * 32), e.Player.Position.Z, e.NewPosition.R, e.NewPosition.L)));
            }
        }
        #region LegendCraft
        /* Copyright (c) <2012-2014> <LeChosenOne, DingusBungus>
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

        static readonly CommandDescriptor CdBot = new CommandDescriptor
        {
            Name = "Bot",
            Permissions = new Permission[] { Permission.Bots },
            Category = CommandCategory.Fun,
            IsConsoleSafe = false,
            Usage = "/Bot <create / remove / removeAll / model / clone / explode / list / summon / stop / move>",
            Help = "Commands for manipulating bots. For help and usage for the individual options, use /help bot <option>.",
            HelpSections = new Dictionary<string, string>{
                { "create", "&H/Bot create <botname> <model>\n&S" +
                                "Creates a new bot with the given name. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie." },
                { "remove", "&H/Bot remove <botname>\n&S" +
                                "Removes the given bot." },
                { "removeall", "&H/Bot removeAll\n&S" +
                                "Removes all bots from the server."},  
                { "model", "&H/Bot model <bot name> <model>\n&S" +
                                "Changes the model of a bot to the given model. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie."},  
                { "clone", "&H/Bot clone <bot> <player>\n&S" +
                                "Changes the skin of a bot to the skin of the given player. Leave the player parameter blank to reset the skin. Bot's model must be human. Use /bot changeModel to change the bot's model."},  
                { "explode", "&H/Bot explode <bot>\n&S" +
                                "Epically explodes a bot, removing it from the server."},  
                { "list", "&H/Bot list\n&S" +
                                "Prints out a list of all the bots on the server."},             
                { "summon", "&H/Bot summon <botname>\n&S" +
                                "Summons a bot from anywhere to your current position."},
                { "move", "&H/Bot move <botname> <player>\n&S" +
                                "Moves the bot to a specific player."},
                { "stop", "&H/Bot stop <botname>\n&S" +
                                "Stops the bot from doing any of its movement actions."}
            },
            Handler = BotHandler,
        };

        static void BotHandler(Player player, Command cmd)
        {
            string option = cmd.Next(); //take in the option arg
            if (string.IsNullOrEmpty(option)) //empty? return, otherwise continue
            {
                CdBot.PrintUsage(player);
                return;
            }

            //certain options that take in specific params are in here, the rest are in the switch-case
            if (option.ToLower() == "list")
            {
                player.Message("_Bots on {0}_", ConfigKey.ServerName.GetString());
                foreach (Bot botCheck in Server.Bots)
                {
                    player.Message(botCheck.Name + " on " + botCheck.World.Name);
                }
                return;
            }
            else if (option.ToLower() == "removeall")
            {

            rewipe:
                Server.Bots.ForEach(botToRemove =>
                {
                    botToRemove.removeBot();
                });

                if (Server.Bots.Count != 0)
                {
                    goto rewipe;
                }

                player.Message("All bots removed from the server.");
                return;
            }
            else if (option.ToLower() == "move")
            {
                string targetBot = cmd.Next();
                if (string.IsNullOrEmpty(targetBot))
                {
                    CdBot.PrintUsage(player);
                    return;
                }
                string targetPlayer = cmd.Next();
                if (string.IsNullOrEmpty(targetPlayer))
                {
                    CdBot.PrintUsage(player);
                    return;
                }

                Bot targetB = player.World.FindBot(targetBot);
                Player targetP = player.World.FindPlayerExact(targetPlayer);

                if (targetP == null)
                {
                    player.Message("Could not find {0} on {1}! Please make sure you spelled their name correctly.", targetPlayer, player.World);
                    return;
                }
                if (targetB == null)
                {
                    player.Message("Could not find {0} on {1}! Please make sure you spelled their name correctly.", targetBot, player.World);
                    return;
                }

                player.Message("{0} is now moving!", targetB.Name);
                targetB.isMoving = true;
                targetB.NewPosition = targetP.Position;
                targetB.OldPosition = targetB.Position;
                targetB.timeCheck.Start();
                return;
            }
            else if (option.ToLower() == "follow")
            {
                return; // not used for now

                string targetBot = cmd.Next();
                if (string.IsNullOrEmpty(targetBot))
                {
                    CdBot.PrintUsage(player);
                    return;
                }
                string targetPlayer = cmd.Next();
                if (string.IsNullOrEmpty(targetPlayer))
                {
                    CdBot.PrintUsage(player);
                    return;
                }

                Bot targetB = player.World.FindBot(targetBot);
                Player targetP = player.World.FindPlayerExact(targetPlayer);

                if (targetP == null)
                {
                    player.Message("Could not find {0} on {1}! Please make sure you spelled their name correctly.", targetPlayer, player.World);
                    return;
                }
                if (targetB == null)
                {
                    player.Message("Could not find {0} on {1}! Please make sure you spelled their name correctly.", targetBot, player.World);
                    return;
                }

                player.Message("{0} is now following {1}!", targetB.Name, targetP.Name);
                targetB.isMoving = true;
                targetB.followTarget = targetP;
                targetB.OldPosition = targetB.Position;
                targetB.timeCheck.Start();
                return;
            }

            //finally away from the special cases
            string botName = cmd.Next(); //take in bot name arg
            if (string.IsNullOrEmpty(botName)) //null check
            {
                CdBot.PrintUsage(player);
                return;
            }

            Bot bot = new Bot(); 
            if (option != "create")//since the bot wouldn't exist for "create", we must check the bot for all cases other than "create"
            {
                bot = Server.FindBot(botName.ToLower()); //Find the bot and assign to bot var

                if (bot == null) //If null, return and yell at user
                {
                    player.Message("Could not find {0}! Please make sure you spelled the bot's name correctly. To view all the bots, type /Bot list.", botName);
                    return;
                }
            }

            //now to the cases - additional args should be taken in at the individual cases
            switch (option.ToLower())
            {
                case "create":
                    string requestedModel = cmd.Next();
                    if (string.IsNullOrEmpty(requestedModel))
                    {
                        player.Message("Usage is /Bot create <bot name> <bot model>. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie.");
                        return;
                    }

                    if (!validEntities.Contains(requestedModel))
                    {
                        player.Message("That wasn't a valid bot model! Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie.");
                        return;
                    }

                    //if a botname has already been chosen, ask player for a new name
                    var matchingNames = from b in Server.Bots
                                   where b.Name.ToLower() == botName.ToLower()
                                   select b;

                    if (matchingNames.Count() > 0)
                    {
                        player.Message("A bot with that name already exists! To view all bots, type /bot list.");
                        return;
                    }

                    player.Message("Successfully created a bot.");
                    Bot botCreate = new Bot();
                    botCreate.setBot(botName, player.World, player.Position, LegendCraft.getNewID());
                    botCreate.createBot();
                    botCreate.changeBotModel(requestedModel);
                    break;
                case "remove":
                    player.Message("{0} was removed from the server.", bot.Name);
                    bot.removeBot();
                    break;
                case "fly":

                    if (bot.isFlying)
                    {
                        player.Message("{0} can no longer fly.", bot.Name);
                        bot.isFlying = false;
                        break;
                    }

                    player.Message("{0} can now fly!", bot.Name);
                    bot.isFlying = true;
                    break;
                case "model":
                    
                    if (bot.Skin != "steve")
                    {
                        player.Message("Bots cannot change model with a skin! Use '/bot clone' to reset a bot's skin.");
                        return;
                    }

                    string model = cmd.Next();
                    if (string.IsNullOrEmpty(model))
                    {
                        player.Message("Usage is /Bot model <bot> <model>. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie.");
                        break;
                    }

                    if(model == "human")//lazy parse
                    {
                        model = "humanoid";
                    }
                    if (!validEntities.Contains(model))
                    {
                        player.Message("Please use a valid model name! Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie.");
                        break;
                    }

                    player.Message("Changed bot model to {0}.", model);
                    bot.changeBotModel(model);

                    break;
                case "clone":

                    if (bot.Model != "humanoid")
                    {
                        player.Message("A bot must be a human in order to have a skin. Use '/bot model <bot> <model>' to change a bot's model.");
                        return;
                    }

                    string playerToClone = cmd.Next();
                    if (string.IsNullOrEmpty(playerToClone))
                    {
                        player.Message("{0}'s skin was reset!", bot.Name);
                        bot.Clone("steve");
                        break;
                    }
                    PlayerInfo targetPlayer = PlayerDB.FindPlayerInfoExact(playerToClone);
                    if (targetPlayer == null)
                    {
                        player.Message("That player doesn't exists! Please use a valid playername for the skin of the bot.");
                        break;
                    }

                    player.Message("{0}'s skin was updated!", bot.Name);
                    bot.Clone(playerToClone);
                    break;
                case "explode":

                    Server.Message("{0} exploded!", bot.Name);
                    bot.explodeBot(player);
                    break;
                case "summon":

                    if (player.World != bot.World)
                    {
                        bot.tempRemoveBot(); //remove the entity
                        bot.World = player.World; //update variables
                        bot.Position = player.Position;
                        bot.updateBotPosition(); //replace the entity
                    }
                    else
                    {
                        bot.Position = player.Position;
                        bot.teleportBot(player.Position);
                    }

                    if (bot.Model != "human")
                    {
                        bot.changeBotModel(bot.Model); //replace the model, if the model is set
                    }
                    if (bot.Skin != "steve")
                    {
                        bot.Clone(bot.Skin); //replace the skin, if a skin is set
                    }
                    break;
                case "stop":

                    player.Message("{0} is no longer moving.", bot.Name);
                    bot.isMoving = false;
                    bot.timeCheck.Stop();
                    bot.timeCheck.Reset();
                    bot.followTarget = null;
                    break;
                default:
                    CdBot.PrintUsage(player);
                    break;
            }
        }

        static readonly CommandDescriptor CdSetModel = new CommandDescriptor
        {
            Name = "SetModel",
            Permissions = new Permission[] { Permission.Troll },
            Category = CommandCategory.Fun,
            IsConsoleSafe = false,
            Usage = "/SetModel [Player] [Model]",
            Help = "Changes the model of a target player Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie. If the model is empty, the player's model will reset.",
            Handler = ModelHandler,
        };

        static void ModelHandler(Player player, Command cmd)
        {
            string target = cmd.Next();
            if(string.IsNullOrEmpty(target))
            {
                CdSetModel.PrintUsage(player);
                return;
            }

            Player targetPlayer = Server.FindPlayerOrPrintMatches(player, target, false, true);
            if (targetPlayer == null)
            {
                return;
            }

            string model = cmd.Next();
            if (string.IsNullOrEmpty(model))
            {
                player.Message("Reset the model for {0}.", targetPlayer.Name);
                targetPlayer.Model = player.Name; //reset the model to the player's name
                return;
            }

            if (model == "human")//execute super lazy parse
            {
                model = "humanoid";
            }
            if (!validEntities.Contains(model))
            {
                player.Message("Please choose a valid model! Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, or zombie.");
                return;
            }

            player.Message("{0} has been changed into a {1}!", targetPlayer.Name, model);
            targetPlayer.Model = model;
            return;
        }

        static readonly CommandDescriptor CdTroll = new CommandDescriptor //Troll is an old command from 800craft that i have rehashed because of its popularity
        {                                                                 //The original command and the idea for the command were done by Jonty800 and Rebelliousdude.
            Name = "Troll",
            Permissions = new Permission[] { Permission.Moderation },
            Category = CommandCategory.Chat | CommandCategory.Fun,
            IsConsoleSafe = true,
            Usage = "/Troll (playername) (message-type) (message)",
            Help = "Allows you impersonate others in the chat. Available chat types are msg, st, ac, pm, rq, and leave.",
            NotRepeatable = true,
            Handler = Troll,
        };

        static void Troll(Player p, Command c)
        {
            string name = c.Next();
            string chatType = c.Next();
            string msg = c.NextAll();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(chatType))
            {
                p.Message("Use like : /Troll (playername) (chat-type) (message)");
                return;
            }
            Player tgt = Server.FindPlayerOrPrintMatches(p, name, false, true);
            if (tgt == null) { return; }
            switch (chatType)
            {
                default:
                    p.Message("The available chat types are: st, ac, pm, msg, rq and leave");
                    break;
                case "leave":
                    Server.Message("&SPlayer {0}&S left the server.", tgt.ClassyName);
                    break;
                case "ragequit":
                case "rq":
                    string quitMsg = "";
                    if (msg.Length > 1) { quitMsg = ": &C" + msg; }
                    Server.Message("{0}&4 Ragequit from the server{1}", tgt.ClassyName, quitMsg);
                    Server.Message("&SPlayer {0}&S left the server.", tgt.ClassyName);
                    break;
                case "st":
                case "staffchat":
                case "staff":
                    if (string.IsNullOrEmpty(msg))
                    {
                        p.Message("The st option requires a message: /troll (player) st (message)");
                        return;
                    }
                    Server.Message("&P(staff){0}&P: {1}", tgt.ClassyName, msg);
                    break;
                case "pm":
                case "privatemessage":
                case "private":
                case "whisper":
                    if (string.IsNullOrEmpty(msg))
                    {
                        p.Message("The pm option requires a message: /troll (player) pm (message)");
                        return;
                    }
                    Server.Message("&Pfrom {0}: {1}", tgt.Name, msg);
                    break;
                case "ac":
                case "adminchat":
                case "admin":
                    if (string.IsNullOrEmpty(msg))
                    {
                        p.Message("The ac option requires a message: /troll (player) ac (message)");
                        return;
                    }
                    Server.Message("&9(Admin){0}&P: {1}", tgt.ClassyName, msg);
                    break;
                case "msg":
                case "message":
                case "global":
                case "server":
                    if (string.IsNullOrEmpty(msg))
                    {
                        p.Message("The msg option requires a message: /troll (player) msg (message)");
                        return;
                    }
                    Server.Message("{0}&f: {1}", tgt.ClassyName, msg);
                    break;
            }
            return;
        }

        #region Gamemodes

        static readonly CommandDescriptor CdCTF = new CommandDescriptor
        {
            Name = "CTF",
            Aliases = new[] { "CaptureTheFlag" },
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/CTF [Start | Stop | SetSpawn | SetFlag | Help]",
            Help = "Manage the CTF Game!",
            Handler = CTFHandler
        };

        private static void CTFHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            if (String.IsNullOrEmpty(option))
            {
                player.Message("Please select an option in the CTF menu!");
                return;
            }

            World world = player.World;

            switch (option.ToLower())
            {
                case "begin":
                case "start":

                    if (world.blueCTFSpawn == new Vector3I(0, 0, 0) || world.redCTFSpawn == new Vector3I(0, 0, 0))
                    {
                        player.Message("&cYou must assign spawn points before the game starts! Use /CTF SetSpawn <red | blue>");
                        return;
                    }
                    if (world.blueFlag == new Vector3I(0, 0, 0) || world.redFlag == new Vector3I(0, 0, 0))
                    {
                        player.Message("&cYou must set the flags before play! Use /CTF SetFlag <red | blue>");
                        return;
                    }

                    if (world.Players.Count() < 2)
                    {
                        player.Message("&cYou need at least 2 players to play CTF");
                        return;
                    }

                    try
                    {
                        foreach (Player p in player.World.Players)
                        {
                            p.JoinWorld(player.World, WorldChangeReason.Rejoin);
                        }

                        CTF.GetInstance(world);
                        CTF.Start();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, "Error: " + e + e.Message);
                    }
                    break;
                case "end":
                case "stop":
                    if (world.gameMode != GameMode.CaptureTheFlag)
                    {
                        player.Message("&cThere is no game of CTF currently going on!");
                        break;
                    }

                    CTF.Stop(player);
                    break;
                case "setspawn":
                    if (world.gameMode != GameMode.NULL)
                    {
                        player.Message("&cYou cannot change spawns during the game!");
                        return;
                    }

                    string team = cmd.Next();
                    if (String.IsNullOrEmpty(team))
                    {
                        player.Message("&cPlease select a team to set a spawn for!");
                        break;
                    }

                    if (team.ToLower() == "red")
                    {
                        world.redCTFSpawn = new Vector3I(player.Position.ToBlockCoords().X, player.Position.ToBlockCoords().Y, player.Position.ToBlockCoords().Z + 2);
                        player.Message("&aRed team spawn set.");
                        break;
                    }
                    else if (team.ToLower() == "blue")
                    {
                        world.blueCTFSpawn = new Vector3I(player.Position.ToBlockCoords().X, player.Position.ToBlockCoords().Y, player.Position.ToBlockCoords().Z + 2);
                        player.Message("&aBlue team spawn set.");
                        break;
                    }
                    else
                    {
                        player.Message("&cYou may only select the 'Blue' or 'Red' team!");
                        break;
                    }
                case "setflag":
                    if (world.gameMode != GameMode.NULL)
                    {
                        player.Message("&cYou cannot change flags during the game!");
                        return;
                    }

                    string flag = cmd.Next();
                    if (String.IsNullOrEmpty(flag))
                    {
                        player.Message("&cPlease select a flag color to set!");
                        break;
                    }

                    if (flag.ToLower() == "red")
                    {
                        //select red flag
                        player.Message("&fPlease select where you wish to place the &cred&f flag. The &cred&f flag must be red wool.");
                        player.Info.placingRedFlag = true;
                        break;
                    }
                    else if (flag.ToLower() == "blue")
                    {
                        player.Message("&fPlease select where you wish to place the &9blue&f flag. The &9blue&f flag must be blue wool.");
                        player.Info.placingBlueFlag = true;
                        break;
                    }
                    else
                    {
                        player.Message("&cYou may only select a 'Blue' or 'Red' colored flag!");
                        break;
                    }
                case "help":
                case "rules":
                    player.Message("Start: Starts the CTF game.");
                    player.Message("Stop: Ends the CTF game.");
                    player.Message("SetSpawn: Usage is /CTF SetSpawn <Red|Blue>. The spawn for each team will be set at your feet. Both spawns must be set for the game to begin.");
                    player.Message("SetFlag: Usage is /CTF SetFlag <Red|Blue>. The next block clicked will be the corresponding team's flag. The blue flag must be a blue wool block, and the red flag must be a red wool block.");
                    break;
                default:
                    CdCTF.PrintUsage(player);
                    break;
            }
        }

        static readonly CommandDescriptor CdInfection = new CommandDescriptor
        {
            Name = "Infection",
            Aliases = new[] { "ZombieSurvival", "zs" },
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/Infection [start | stop | custom | help]",
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
                        player.World.Hax = false;
                        foreach (Player p in player.World.Players)
                        {
                            p.JoinWorld(player.World, WorldChangeReason.Rejoin);
                        }

                        fCraft.Games.Infection.GetInstance(world);
                        fCraft.Games.Infection.Start();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, "Error: " + e + e.Message);
                    }
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
            if (Option.ToLower() == "custom")
            {
                string stringLimit = cmd.Next();
                string stringDelay = cmd.Next();
                int intLimit, intDelay;

                if (String.IsNullOrEmpty(stringLimit) || String.IsNullOrEmpty(stringDelay))
                {
                    player.Message("Usage for '/infection custom' is '/infection custom TimeLimit TimeDelay'.");
                }

                if (!int.TryParse(stringLimit, out intLimit))
                {
                    player.Message("Please select a number for the time limit.");
                    return;
                }

                if (!int.TryParse(stringDelay, out intDelay))
                {
                    player.Message("Please select a number for the time delay.");
                    return;
                }

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
                if (intLimit > 900 || intLimit < 60)
                {
                    player.Message("&SThe game must be between 60 and 900 seconds! (1 and 15 minutes)");
                    return;
                }
                if (intDelay > 60 || intDelay < 11)
                {
                    player.Message("&SThe game delay must be greater than 10 seconds, but less than 60 seconds!");
                    return;
                }
                else
                {
                    try
                    {
                        player.World.Hax = false;
                        foreach (Player p in player.World.Players)
                        {
                            p.JoinWorld(player.World, WorldChangeReason.Rejoin);
                        }

                        fCraft.Games.Infection.GetInstance(world);
                        fCraft.Games.Infection.Custom(intLimit, intDelay);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, "Error: " + e + e.Message);
                    }
                    return;
                }
            }
            if (Option.ToLower() == "help")
            {
                player.Message("&SStart: Will begin a game of infection on the current world.\n" +
                    "&SStop: Will end a game of infection on the current world.\n" +
                    "&SCustom: Determines factors in the next Infection game. Factors are TimeLimit and TimeDelay. TimeDelay must be greater than 10.\n" +
                    "&fExample: '/Infection Custom 100 12' would start an Infection game with a game length of 100 seconds, and it will begin in 12 seconds.\n"
                    );

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
            Usage = "/TeamDeathMatch [Start | Stop | Time | Score | ScoreLimit | TimeLimit | TimeDelay | Settings | Red | Blue | ManualTeams | About | Help]",
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
                if (TeamDeathMatch.blueSpawn == Position.Zero || TeamDeathMatch.redSpawn == Position.Zero)
                {
                    player.Message("You must first assign the team's spawn points with &H/TD SetSpawn (Red/Blue)");
                    return;
                }
                else
                {
                    player.World.Hax = false;
                    foreach (Player p in player.World.Players)
                    {
                        p.JoinWorld(player.World, WorldChangeReason.Rejoin);
                    }
                    TeamDeathMatch.GetInstance(player.World);
                    TeamDeathMatch.Start();
                    return;
                }
            }
            if (Option.ToLower() == "stop" || Option.ToLower() == "off") //stops the game
            {
                if (TeamDeathMatch.isOn)
                {
                    TeamDeathMatch.Stop(player);
                    return;
                }
                else
                {
                    player.Message("No games of Team DeathMatch are going on");
                    return;
                }
            }
            if (Option.ToLower() == "manualteams")
            {
                string option = cmd.Next();
                if (string.IsNullOrEmpty(option) || option.Length < 2 || option.Length > 9)
                {
                    player.Message("Use like: /TD ManualTeams (On/Off)");
                    return;
                }
                if (option.ToLower() == "off" || option.ToLower() == "auto" || option.ToLower() == "automatic")
                {
                    if (!TeamDeathMatch.manualTeams)
                    {
                        player.Message("The team assign option is already set to &wAuto");
                        return;
                    }
                    TeamDeathMatch.manualTeams = false;
                    player.Message("The team assign option has been set to &WAuto&s.");
                    return;
                }
                if (option.ToLower() == "on" || option.ToLower() == "manual")
                {
                    if (TeamDeathMatch.manualTeams)
                    {
                        player.Message("The team assign option is already set to &wManual");
                        return;
                    }
                    TeamDeathMatch.manualTeams = true;
                    player.Message("The team assign option has been set to &WManual&s.");
                    return;
                }
            }
            if (TeamDeathMatch.isOn && (Option.ToLower() == "timelimit" || Option.ToLower() == "scorelimit" || Option.ToLower() == "timedelay"))
            {
                player.Message("You cannot adjust game settings while a game is going on");
                return;
            }
            if (!TeamDeathMatch.isOn && (Option.ToLower() == "timelimit" || Option.ToLower() == "scorelimit" || Option.ToLower() == "timedelay"))
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
                        TeamDeathMatch.timeLimit = (timeLimit * 60);
                        player.Message("The time limit has been changed to &W{0}&S minutes", timeLimit);
                        return;
                    }
                }
                if (Option.ToLower() == "timedelay")    //option to set the time delay for TDM games (20s default)
                {
                    if (TeamDeathMatch.manualTeams)
                    {
                        player.Message("The manual team assign option is enabled so the delay is 30 seconds to enable team assigning");
                        return;
                    }
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
                        TeamDeathMatch.timeDelay = timeDelay;
                        player.Message("The time delay has been changed to &W{0}&s seconds", timeDelay);
                        return;
                    }
                }
                if (Option.ToLower() == "scorelimit")       //changes the score limit (30 default)
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
                        TeamDeathMatch.scoreLimit = scoreLimit;
                        player.Message("The score limit has been changed to &W{0}&s points", scoreLimit);
                        return;
                    }
                }
            }
            if (Option.ToLower() == "red")
            {
                string target = cmd.Next();
                if (target == null)
                {
                    player.Message("Use like: /TD Red <PlayerName>");
                    return;
                }
                Player targetP = Server.FindPlayerOrPrintMatches(player, target, true, true);
                if (targetP == null) return;
                if (player.World.gameMode == GameMode.TeamDeathMatch && !TeamDeathMatch.isOn)
                {
                    TeamDeathMatch.AssignRed(targetP);
                    return;
                }
                else
                {
                    player.Message("You can only assign teams during the delay of a Team DeathMatch Game.");
                    return;
                }
            }
            if (Option.ToLower() == "blue")
            {
                string target = cmd.Next();
                if (target == null)
                {
                    player.Message("Use like: /TD Blue <PlayerName>");
                    return;
                }
                Player targetP = Server.FindPlayerOrPrintMatches(player, target, true, true);
                if (targetP == null) return;
                if (player.World.gameMode == GameMode.TeamDeathMatch && !TeamDeathMatch.isOn)
                {
                    TeamDeathMatch.AssignBlue(targetP);
                    return;
                }
                else
                {
                    player.Message("You can only assign teams during the delay of a Team DeathMatch Game.");
                    return;
                }
            }
            if (Option.ToLower() == "setspawn")
            {
                string team = cmd.Next();
                if (string.IsNullOrEmpty(team) || team.Length < 1)
                {
                    player.Message("Use like: /TD SetSpawn (Red/Blue)");
                    return;
                }
                if (TeamDeathMatch.isOn)
                {
                    player.Message("You cannot change the spawn during the game!");
                    return;
                }
                if (!TeamDeathMatch.isOn && player.World != WorldManager.MainWorld)
                {
                    switch (team.ToLower())
                    {
                        default:
                            player.Message("Use like: /TD SetSpawn (Red/Blue)");
                            return;
                        case "red":
                            TeamDeathMatch.redSpawn = player.Position;
                            player.Message("&SSpawn for the &cRed&S team set.");
                            return;
                        case "blue":
                            TeamDeathMatch.blueSpawn = player.Position;
                            player.Message("&SSpawn for the &1Blue&S team set.");
                            return;
                    }
                }
                else
                {
                    if (player.World == WorldManager.MainWorld) { player.Message("You cannot play TDM on the main world"); return; }
                    else if (TeamDeathMatch.isOn)
                    {
                        player.Message("You can only set the team spawns during the delay or before the game");
                        return;
                    }
                }
            }
            if (Option.ToLower() == "score")       //scoreboard for the matchs, different messages for when the game has ended. //td score
            {
                int red = TeamDeathMatch.redScore;
                int blue = TeamDeathMatch.blueScore;

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
                player.Message("&cTeam Deathmatch&S is a team game where all players are assigned to a red or blue team. Players cannot shoot players on their own team. The game will start the gun physics and do /gun for you. The game keeps score and notifications come up about the score and time left every 30 seconds. The Score Limit, Time Delay, Time Limit, and Team Assigning are customizable. Detailed help is on &H/TD Help"
                + "\n&SDeveloped for &5Legend&WCraft&S by &fDingus&0Bungus&S 2013 - Based on the template of ZombieGame.cs written by Jonty800.");
                return;
            }
            if (Option.ToLower() == "settings") //shows the current settings for the game (time limit, time delay, score limit, team assigning)
            {
                if (!TeamDeathMatch.manualTeams)
                {
                    player.Message("The Current Settings For TDM: Auto Team Assign: &cOn&s | Time Delay: &c{0}&ss | Time Limit: &c{1}&sm | Score Limit: &c{2}&s points",
                        TeamDeathMatch.timeDelay, (TeamDeathMatch.timeLimit / 60), TeamDeathMatch.scoreLimit);
                    return;
                }
                if (TeamDeathMatch.manualTeams)
                {
                    player.Message("The Current Settings For TDM: Auto Team Assign: &cOff&s | Time Delay: &c30&ss | Time Limit: &c{1}&sm | Score Limit: &c{2}&s points",
                        TeamDeathMatch.timeDelay, (TeamDeathMatch.timeLimit / 60), TeamDeathMatch.scoreLimit);
                    return;
                }
            }
            if (Option.ToLower() == "help") //detailed help for the cmd
            {
                player.Message("Showing Option Descriptions for /TD (Option):"
                + "\n&HTime &f- Tells how much time left in the game"
                + "\n&HScore &f- Tells the score of the game (or last game played)"
                + "\n&HSetSpawn [Red/Blue] &f- Sets the teams spawns"
                + "\n&HScoreLimit [number] &f- Sets score limit (Whole Numbers from 5-300)"
                + "\n&HTimeLimit [time(m)] &f- Sets end time (Whole minutes from 1-15)"
                + "\n&HTimeDelay [time(s)] &f- Sets start delay (10 second incriments from 10-60)"
                + "\n&HManualTeams [On/Off] &f- Create teams manually/automatically"
                + "\n&HRed [PlayerName] &f- Assigns the given player to the red team"
                + "\n&HBlue [PlayerName] &f - Assigns the given player to the blue team"
                + "\n&HSettings&f - Shows the current TDM settings"
                + "\n&HAbout&f - General description and credits");
                return;
            }
            if (Option.ToLower() == "time" || Option.ToLower() == "timeleft")
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
                CdTeamDeathMatch.PrintUsage(player);
                return;
            }
        }

        static readonly CommandDescriptor CdFreeForAll = new CommandDescriptor
        {
            Name = "FreeForAll",            //I think I resolved all of the bugs...
            Aliases = new[] { "ffa" },
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/FFA [Start | Stop | Time | Score | ScoreLimit | TimeLimit | TimeDelay | TNT | Settings | About | Help]",
            Help = "Manage the Free-For-All Gamemode!",
            Handler = FFAHandler
        };

        private static void FFAHandler(Player player, Command cmd)       //For FFA Game: starting/ending game, customizing game options, viewing score, etc.
        {
            string Option = cmd.Next();
            World world = player.World;

            if (string.IsNullOrEmpty(Option))
            {
                CdFreeForAll.PrintUsage(player);
                return;
            }
            if (Option.ToLower() == "start" || Option.ToLower() == "on")    //starts the game
            {
                if (world == WorldManager.MainWorld)
                {
                    player.Message("FFA games cannot be played on the main world");
                    return;
                }
                if (world.gameMode != GameMode.NULL)
                {
                    player.Message("There is already a game of FFA going on");
                    return;
                }
                if (player.World.CountPlayers(true) < 2)
                {
                    player.Message("There needs to be at least &W2&S players to play FFA");
                    return;
                }
                else
                {
                    //restart, without hax
                    player.World.Hax = false;
                    foreach (Player p in player.World.Players)
                    {
                        p.JoinWorld(player.World, WorldChangeReason.Rejoin);
                    }

                    FFA.GetInstance(player.World);
                    FFA.Start();
                    return;
                }
            }
            if (Option.ToLower() == "stop" || Option.ToLower() == "off") //stops the game
            {
                if (FFA.isOn())
                {
                    FFA.stoppedEarly = true;
                    FFA.Stop(player);
                    return;
                }
                else
                {
                    player.Message("No games of FFA are going on");
                    return;
                }
            }
            if (FFA.isOn() && (Option.ToLower() == "timelimit" || Option.ToLower() == "scorelimit" || Option.ToLower() == "timedelay" || Option.ToLower() == "tnt"))
            {
                player.Message("You cannot adjust game settings while a game is going on");
                return;
            }
            if (!FFA.isOn() && (Option.ToLower() == "timelimit" || Option.ToLower() == "scorelimit" || Option.ToLower() == "timedelay" || Option.ToLower() == "tnt"))
            {
                if (Option.ToLower() == "timelimit")    //option to change the length of the game (5m default)
                {
                    string time = cmd.Next();
                    if (time == null)
                    {
                        player.Message("Use the syntax: /FFA timelimit (whole number of minutes)\n&HNote: The acceptable times are from 1-20 minutes");
                        return;
                    }
                    int timeLimit = 0;
                    bool parsed = Int32.TryParse(time, out timeLimit);
                    if (!parsed)
                    {
                        player.Message("Enter a whole number of minutes. For example: /FFA timelimit 5");
                        return;
                    }
                    if (timeLimit < 1 || timeLimit > 20)
                    {
                        player.Message("The accepted times are between 1 and 20 minutes");
                        return;
                    }
                    else
                    {
                        FFA.timeLimit = (timeLimit * 60);
                        player.Message("The time limit has been changed to &W{0}&S minutes", timeLimit);
                        return;
                    }
                }
                if (Option.ToLower() == "timedelay")    //option to set the time delay for TDM games (20s default)
                {
                    string time = cmd.Next();
                    if (time == null)
                    {
                        player.Message("Use the syntax: /FFA timedelay (whole number of seconds)\n&HNote: The acceptable times incriment by 10 from 10 to 60");
                        return;
                    }
                    int timeDelay = 0;
                    bool parsed = Int32.TryParse(time, out timeDelay);
                    if (!parsed)
                    {
                        player.Message("Enter a whole number of minutes. For example: /FFA timedelay 20");
                        return;
                    }
                    if (timeDelay != 10 && timeDelay != 20 && timeDelay != 30 && timeDelay != 40 && timeDelay != 50 && timeDelay != 60)
                    {
                        player.Message("The accepted times are 10, 20, 30, 40, 50, and 60 seconds");
                        return;
                    }
                    else
                    {
                        FFA.timeDelay = timeDelay;
                        player.Message("The time delay has been changed to &W{0}&s seconds", timeDelay);
                        return;
                    }
                }
                if (Option.ToLower() == "scorelimit")       //changes the score limit (30 default)
                {
                    string score = cmd.Next();
                    if (score == null)
                    {
                        player.Message("Use the syntax: /FFA scorelimit (whole number)\n&HNote: The acceptable scores are from 5-300 points");
                        return;
                    }
                    int scoreLimit = 0;
                    bool parsed = Int32.TryParse(score, out scoreLimit);
                    if (!parsed)
                    {
                        player.Message("Enter a whole number score. For example: /FFA scorelimit 50");
                        return;
                    }
                    if (scoreLimit < 5 || scoreLimit > 300)
                    {
                        player.Message("The accepted scores are from 5-300 points");
                        return;
                    }
                    else
                    {
                        FFA.scoreLimit = scoreLimit;
                        player.Message("The score limit has been changed to &W{0}&s points", scoreLimit);
                        return;
                    }
                }
                if (Option.ToLower() == "tnt")
                {
                    if (!FFA.tntAllowed)
                    {
                        player.Message("&WTNT blasts are now scored in FFA");
                        FFA.tntAllowed = true;
                        return;
                    }
                    player.Message("&WTNT blasts are no longer scored in FFA");
                    FFA.tntAllowed = false;
                    return;
                }
            }
            if (Option.ToLower() == "score")       //score for the matches
            {
                Player leader = FFA.GetScoreList()[0];
                int leadScore = leader.Info.gameKillsFFA;
                int secondScore = FFA.GetScoreList()[1].Info.gameKillsFFA;

                player.Message("&f{0}&f is winning &c{1}&f to &c{2}.", leader.ClassyName, leadScore, secondScore);
                return;
            }
            if (Option.ToLower() == "about")    //FFA about
            {
                player.Message("&cFree For All&S is a gun game where it is everyone versus everyone. The game will start the gun physics and do /gun for you (also unhides players). The game keeps score and notifications come up about the score and time left every 30 seconds. The Score Limit, Time Delay, and Time Limit are customizable. Detailed help is on &H/FFA Help"
                + "\n&SDeveloped for &5Legend&WCraft&S by &fDingus&0Bungus&S 2013 - Based on the template of ZombieGame.cs written by Jonty800.");
                return;
            }
            if (Option.ToLower() == "settings") //shows the current settings for the game (time limit, time delay, score limit)
            {

                player.Message("The Current Settings For FFA: Time Delay: &c{0}&ss | Time Limit: &c{1}&sm | Score Limit: &c{2}&s points",
                    FFA.timeDelay, (FFA.timeLimit / 60), FFA.scoreLimit);
                return;

            }
            if (Option.ToLower() == "help") //detailed help for the cmd
            {
                player.Message("Showing Option Descriptions for /FFA (Option):"
                + "\n&HStart &f- Starts a game of FFA on the current world"
                + "\n&HStop &f- Stops a game of FFA"
                + "\n&HTime &f- Tells how much time left in the game"
                + "\n&HScoreLimit [number] &f- Sets score limit (Whole Numbers from 5-300)"
                + "\n&HTimeLimit [time(m)] &f- Sets end time (Whole minutes from 1-15)"
                + "\n&HTimeDelay [time(s)] &f- Sets start delay (10 second incriments from 10-60)"
                + "\n&HSettings&f - Shows the current game settings"
                + "\n&HAbout&f - General description and credits");
                return;
            }
            if (Option.ToLower() == "time" || Option.ToLower() == "timeleft")
            {
                if (player.Info.isPlayingFFA)
                {
                    player.Message("&fThere are &W{0}&f seconds left in the game.", FFA.timeLeft);
                    return;
                }
                else
                {
                    player.Message("&fThere are no games of FFA going on.");
                    return;
                }
            }
            else
            {
                CdFreeForAll.PrintUsage(player);
                return;
            }
        }

        static readonly CommandDescriptor CdTDStatistics = new CommandDescriptor
        {
            Name = "TDStatistics",
            Aliases = new[] { "tdstats" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = false,
            Usage = "/TDStats (AllTime|TopKills|TopDeaths|Help)\n&HNote: Leave Blank For Current Game Stats.",
            Handler = TDStatisticsHandler
        };

        private static void TDStatisticsHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            double TDMKills = player.Info.gameKills;    //for use in division (for precision)
            double TDMDeaths = player.Info.gameDeaths;

            if (string.IsNullOrEmpty(option)) //user does /TDstats
            {
                double gameKDR = 0;
                if (TDMDeaths == 0 && TDMKills == 0)
                {
                    gameKDR = 0;
                }
                else if (TDMKills == 0 && TDMDeaths > 0)
                {
                    gameKDR = 0;
                }
                else if (TDMDeaths == 0 && TDMKills > 0)
                {
                    gameKDR = player.Info.gameKills;
                }
                else if (TDMDeaths > 0 && TDMKills > 0)
                {
                    gameKDR = TDMKills / TDMDeaths;
                }
                if (player.Info.isPlayingTD)
                {
                    player.Message("&sYou have &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio is &W{2:0.00}&s.", player.Info.gameDeaths, player.Info.gameDeaths, gameKDR);
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
                        if (option.Length < 2)
                        {
                            player.Message("Invalid /TDStats option. Do &H/TDStats Help&s for all /TDStats options.");
                            return;
                        }
                        PlayerInfo tar = PlayerDB.FindPlayerInfoOrPrintMatches(player, option);
                        if (tar == null) { return; }
                        else
                        {
                            double tarKills = tar.gameKills;
                            double tarDeaths = tar.gameDeaths;    //for use in division (for precision)
                            double gameKDR = 0;
                            string opt = cmd.Next();
                            if (opt == null)
                            {
                                player.Message("By default, checking TDM game stats for {0}", tar.ClassyName);
                                if (tarKills == 0)
                                {
                                    gameKDR = 0;
                                }
                                else if (tarDeaths == 0 && tarKills > 0)
                                {
                                    gameKDR = tarKills;
                                }
                                else if (tarDeaths > 0 && tarKills > 0)
                                {
                                    gameKDR = tarKills / tarDeaths;
                                }
                                if (tar.isPlayingTD)
                                {
                                    player.Message("&s{0}&S has &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio is &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                }
                                else
                                {
                                    player.Message("&s{0}&S had &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio was &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                }
                                return;
                            }
                            switch (opt.ToLower())
                            {
                                default:
                                    player.Message("By default, checking game stats for {0}", tar.ClassyName);
                                    if (tarKills == 0)
                                    {
                                        gameKDR = 0;
                                    }
                                    else if (tarDeaths == 0 && tarKills > 0)
                                    {
                                        gameKDR = tarKills;
                                    }
                                    else if (tarDeaths > 0 && tarKills > 0)
                                    {
                                        gameKDR = tarKills / tarDeaths;
                                    }
                                    if (tar.isPlayingTD)
                                    {
                                        player.Message("&s{0}&S has &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio is &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                    }
                                    else
                                    {
                                        player.Message("&s{0}&S had &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio was &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                    }
                                    break;

                                case "alltime":

                                    double alltimeKDR = 0;
                                    double dubKills = tar.totalKillsTDM;
                                    double dubDeaths = tar.totalDeathsTDM;
                                    if (tar.totalKillsTDM == 0)
                                    {
                                        alltimeKDR = 0;
                                    }
                                    else if (dubDeaths == 0 && dubKills > 0)
                                    {
                                        alltimeKDR = dubKills;
                                    }
                                    else if (dubDeaths > 0 && dubKills > 0)
                                    {
                                        alltimeKDR = dubKills / dubDeaths;
                                    }
                                    player.Message("&sIn all &WTeam Deathmatch&S games {0}&S has played, they have gotten: &W{1}&S Kills and &W{2}&s Deaths giving them a Kill/Death ratio of &W{3:0.00}&S.",
                                        tar.ClassyName, dubKills, dubDeaths, alltimeKDR);
                                    break;
                            }
                        }
                        return;

                    case "alltime": //user does /TDstats alltime

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

                    case "topkills": //user does /TDstats topkills
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

                    case "topdeaths": //user does /TDstats topdeaths
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

                    case "scoreboard": //user does /TDstats scoreboard
                        List<PlayerInfo> TDPlayersRed = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => r.isPlayingTD).Where(r => r.isOnRedTeam).ToArray().OrderBy(r => r.totalKillsTDM).Reverse());
                        List<PlayerInfo> TDPlayersBlue = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => r.isPlayingTD).Where(r => r.isOnBlueTeam).ToArray().OrderBy(r => r.totalKillsTDM).Reverse());
                        if (TeamDeathMatch.redScore >= TeamDeathMatch.blueScore)
                        {
                            player.Message("&CRed Team &f{0}&1:", TeamDeathMatch.redScore);
                            for (int i = 0; i < TDPlayersRed.Count(); i++)
                            {
                                string sbNameRed = TDPlayersRed[i].Name;
                                if (TDPlayersRed[i].Name.Contains('@'))
                                {
                                    sbNameRed = TDPlayersRed[i].Name.Split('@')[0];
                                }
                                player.Message("&C{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                                    sbNameRed, TDPlayersRed[i].gameKills, TDPlayersRed[i].gameDeaths);
                            }
                            player.Message("&f--------------------------------------------");
                            player.Message("&1Blue Team &f{0}&1:", TeamDeathMatch.blueScore);
                            for (int i = 0; i < TDPlayersBlue.Count(); i++)
                            {
                                string sbNameBlue = TDPlayersBlue[i].Name;
                                if (TDPlayersBlue[i].Name.Contains('@'))
                                {
                                    sbNameBlue = TDPlayersBlue[i].Name.Split('@')[0];
                                }
                                player.Message("&1{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                                    sbNameBlue, TDPlayersBlue[i].gameKills, TDPlayersBlue[i].gameDeaths);
                            }
                        }
                        if (TeamDeathMatch.redScore < TeamDeathMatch.blueScore)
                        {
                            player.Message("&1Blue Team &f{0}&1:", TeamDeathMatch.blueScore);
                            for (int i = 0; i < TDPlayersBlue.Count(); i++)
                            {
                                string sbNameBlue = TDPlayersBlue[i].Name;
                                if (TDPlayersBlue[i].Name.Contains('@'))
                                {
                                    sbNameBlue = TDPlayersBlue[i].Name.Split('@')[0];
                                }
                                player.Message("&1{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                                    sbNameBlue, TDPlayersBlue[i].gameKills, TDPlayersBlue[i].gameDeaths);
                            }
                            player.Message("&f--------------------------------------------");
                            player.Message("&CRed Team &f{0}&1:", TeamDeathMatch.redScore);
                            for (int i = 0; i < TDPlayersRed.Count(); i++)
                            {
                                string sbNameRed = TDPlayersRed[i].Name;
                                if (TDPlayersRed[i].Name.Contains('@'))
                                {
                                    sbNameRed = TDPlayersRed[i].Name.Split('@')[0];
                                }
                                player.Message("&C{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                                    sbNameRed, TDPlayersRed[i].gameKills, TDPlayersRed[i].gameDeaths);
                            }
                        }
                        return;

                    case "help": //user does /TDstats help
                        player.Message("&HDetailed help for the /TDStats (options):");
                        player.Message("&HAllTime&S - Shows your all time TD stats.");
                        player.Message("&HTopKills&S - Show the players with the most all time kills");
                        player.Message("&HTopDeaths&S - Show the players with the most all time Deaths.");
                        player.Message("&HScoreBoard&S - Shows a scoreboard of the current TDM game.");
                        player.Message("&H(PlayerName) (Alltime|Game) - Shows the alltime or game stats of a player");
                        player.Message("&HNote: Leave Blank For Current Game Stats");
                        return;
                }
            }
        }

        static readonly CommandDescriptor CdFFAStatistics = new CommandDescriptor
        {
            Name = "FFAStatistics",
            Aliases = new[] { "ffastats" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = false,
            Usage = "/FFAStats (AllTime|TopKills|TopDeaths|ScoreBoard|Help)\n&HNote: Leave Blank For Current Game Stats. Can look up others stats "
            + " by using /FFAStats (PlayerName) (AllTime) where alltime is Optional (left blank gives current game stats).",
            Handler = FFAStatsHandler
        };

        private static void FFAStatsHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            double FFAKills = player.Info.gameKillsFFA;    //for use in division (for precision)
            double FFADeaths = player.Info.gameDeathsFFA;

            if (string.IsNullOrEmpty(option)) //user does /FFAstats
            {
                double gameKDR = 0;
                if (FFAKills == 0 && FFAKills == 0)
                {
                    gameKDR = 0;
                }
                else if (FFAKills == 0 && FFADeaths > 0)
                {
                    gameKDR = 0;
                }
                else if (FFADeaths == 0 && FFAKills > 0)
                {
                    gameKDR = player.Info.gameKillsFFA;
                }
                else if (FFADeaths > 0 && FFAKills > 0)
                {
                    gameKDR = FFAKills / FFADeaths;
                }
                if (player.Info.isPlayingFFA)
                {
                    player.Message("&sYou have &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio is &W{2:0.00}&s.", player.Info.gameKillsFFA, player.Info.gameDeathsFFA, gameKDR); //use int forms for display
                }
                else
                {
                    player.Message("&sYou had &W{0}&s Kills and &W{1}&s Deaths. Your Kill/Death Ratio was &W{2:0.00}&s.", player.Info.gameKillsFFA, player.Info.gameDeathsFFA, gameKDR);
                }
                return;
            }
            else
            {
                switch (option.ToLower())
                {
                    default:
                        if (option.Length < 2)
                        {
                            player.Message("Invalid /FFAStats option. Do &H/FFAStats Help&s for all /FFAStats options.");
                            return;
                        }
                        PlayerInfo tar = PlayerDB.FindPlayerInfoOrPrintMatches(player, option);
                        if (tar == null) { return; }
                        else
                        {
                            double tarKills = tar.gameKillsFFA;
                            double tarDeaths = tar.gameDeathsFFA;    //for use in division (for precision)
                            double gameKDR = 0;
                            string opt = cmd.Next();
                            if (opt == null)
                            {
                                player.Message("By default, checking FFA game stats for {0}", tar.ClassyName);
                                if (tarKills == 0)
                                {
                                    gameKDR = 0;
                                }
                                else if (tarDeaths == 0 && tarKills > 0)
                                {
                                    gameKDR = tarKills;
                                }
                                else if (tarDeaths > 0 && tarKills > 0)
                                {
                                    gameKDR = tarKills / tarDeaths;
                                }
                                if (tar.isPlayingFFA)
                                {
                                    player.Message("&s{0}&S has &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio is &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                }
                                else
                                {
                                    player.Message("&s{0}&S had &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio was &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                }
                                return;
                            }
                            switch (opt.ToLower())
                            {
                                default:
                                    player.Message("By default, checking game stats for {0}", tar.ClassyName);
                                    if (tarKills == 0)
                                    {
                                        gameKDR = 0;
                                    }
                                    else if (tarDeaths == 0 && tarKills > 0)
                                    {
                                        gameKDR = tarKills;
                                    }
                                    else if (tarDeaths > 0 && tarKills > 0)
                                    {
                                        gameKDR = tarKills / tarDeaths;
                                    }
                                    if (tar.isPlayingFFA)
                                    {
                                        player.Message("&s{0}&S has &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio is &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                    }
                                    else
                                    {
                                        player.Message("&s{0}&S had &W{1}&s Kills and &W{2}&s Deaths. Their Kill/Death Ratio was &W{3:0.00}&s.", tar.ClassyName, tarKills, tarDeaths, gameKDR);
                                    }
                                    break;

                                case "alltime":

                                    double alltimeKDR = 0;
                                    double dubKills = tar.totalKillsFFA;
                                    double dubDeaths = tar.totalDeathsFFA;
                                    if (dubKills == 0)
                                    {
                                        alltimeKDR = 0;
                                    }
                                    else if (dubDeaths == 0 && dubKills > 0)
                                    {
                                        alltimeKDR = dubKills;
                                    }
                                    else if (dubDeaths > 0 && dubKills > 0)
                                    {
                                        alltimeKDR = dubKills / dubDeaths;
                                    }
                                    player.Message("&sIn all &WFree For All&S games {0}&S has played, they have gotten: &W{1}&S Kills and &W{2}&s Deaths giving them a Kill/Death ratio of &W{3:0.00}&S.",
                                        tar.ClassyName, dubKills, dubDeaths, alltimeKDR);
                                    break;
                            }
                        }
                        return;

                    case "alltime": //user does /FFAstats alltime

                        double allKills = player.Info.totalKillsFFA;
                        double allDeaths = player.Info.totalDeathsFFA; //for use in the division for KDR (int / int = int, so no precision), why we convert to double here
                        double totalKDR = 0;

                        if (allDeaths == 0 && allKills == 0)
                        {
                            totalKDR = 0;
                        }
                        else if (allDeaths == 0 && allKills > 0)
                        {
                            totalKDR = player.Info.totalKillsTDM;
                        }
                        else if (allKills == 0 && allDeaths > 0)
                        {
                            totalKDR = 0;
                        }
                        else if (allKills > 0 && allDeaths > 0)
                        {
                            totalKDR = allKills / allDeaths;
                        }
                        player.Message("&sIn all &WFree For All&S games you have played, you have gotten: &W{0}&S Kills and &W{1}&s Deaths giving you a Kill/Death ratio of &W{2:0.00}&S.",
                                        allKills, allDeaths, totalKDR);
                        return;

                    case "topkills": //user does /FFAstats topkills
                        List<PlayerInfo> FFAPlayers = new List<PlayerInfo>(PlayerDB.PlayerInfoList.ToArray().OrderBy(r => r.totalKillsFFA).Reverse());
                        player.Message("&HShowing the players with the most all-time FFA Kills:");
                        if (FFAPlayers.Count() < 10)
                        {
                            for (int i = 0; i < FFAPlayers.Count(); i++)
                            {
                                player.Message("{0}&s - {1} Kills", FFAPlayers[i].ClassyName, FFAPlayers[i].totalKillsFFA);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                player.Message("{0}&s - {1} Kills", FFAPlayers[i].ClassyName, FFAPlayers[i].totalKillsFFA);
                            }
                        }
                        return;

                    case "topdeaths": //user does /FFAstats topdeaths
                        List<PlayerInfo> FFAPlayers2 = new List<PlayerInfo>(PlayerDB.PlayerInfoList.ToArray().OrderBy(r => r.totalDeathsFFA).Reverse());
                        player.Message("&HShowing the players with the most all-time TDM Deaths:");
                        if (FFAPlayers2.Count() < 10)
                        {
                            for (int i = 0; i < FFAPlayers2.Count(); i++)
                            {
                                player.Message("{0}&s - {1} Deaths", FFAPlayers2[i].ClassyName, FFAPlayers2[i].totalDeathsFFA);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                player.Message("{0}&s - {1} Deaths", FFAPlayers2[i].ClassyName, FFAPlayers2[i].totalDeathsFFA);
                            }
                        }
                        return;

                    case "scoreboard": //user does /FFAstats scoreboard
                        if (!FFA.isOn())
                        {
                            player.Message("There are no games of FFA going on");
                            return;
                        }
                        List<PlayerInfo> FFAScoreBoard = new List<PlayerInfo>(PlayerDB.PlayerInfoList.Where(r => r.isPlayingFFA).ToArray().OrderBy(r => r.gameKillsFFA).Reverse());
                        for (int i = 0; i < FFAScoreBoard.Count(); i++)
                        {
                            string sbName = FFAScoreBoard[i].Name;
                            string color = "&c";
                            if (FFAScoreBoard[i].Name.Contains('@'))
                            {
                                sbName = FFAScoreBoard[i].Name.Split('@')[0];
                            }
                            if (FFAScoreBoard.Count() > 3 && i > 2)
                            {
                                color = "&f";
                            }
                            player.Message("{3}{0} &f| &c{1} &fKills - &c{2} &fDeaths",
                                sbName, FFAScoreBoard[i].gameKillsFFA, FFAScoreBoard[i].gameDeathsFFA, color);
                        }
                        return;

                    case "help": //user does /FFAstats help
                        player.Message("&HDetailed help for the /FFAStats (options):");
                        player.Message("&HAllTime&S - Shows your all time FFA stats");
                        player.Message("&HTopKills&S - Show the players with the most all time kills");
                        player.Message("&HTopDeaths&S - Show the players with the most all time Deaths");
                        player.Message("&HScoreBoard&S - Shows a scoreboard of the current FFA game");
                        player.Message("&H(PlayerName) (Alltime|Game) - Shows the alltime or game stats of a player");
                        player.Message("&HNote: Leave Blank For Current Game Stats");
                        return;
                }
            }
        }

        #endregion

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
            double time = (DateTime.Now - player.Info.LastUsedThrow).TotalSeconds;
            if (time < 10)
            {
                player.Message("&WYou can use /Throw again in " + Math.Round(10 - time) + " seconds.");
                return;
            }
            Random random = new Random();
            int randomNumber = random.Next(1, 4);
            player.Info.LastUsedThrow = DateTime.Now;

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
        #endregion



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



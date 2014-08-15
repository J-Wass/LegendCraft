// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    static class ModerationCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/BanInfo";

        internal static void Init() {
            CdBan.Help += BanCommonHelp;
            CdBanIP.Help += BanCommonHelp;
            CdBanAll.Help += BanCommonHelp;
            CdUnban.Help += BanCommonHelp;
            CdUnbanIP.Help += BanCommonHelp;
            CdUnbanAll.Help += BanCommonHelp;

            CommandManager.RegisterCommand( CdBan );
            CommandManager.RegisterCommand( CdBanIP );
            CommandManager.RegisterCommand( CdUnban );
            CommandManager.RegisterCommand( CdUnbanIP );
            CommandManager.RegisterCommand( CdUnbanAll );

            CommandManager.RegisterCommand( CdBanEx );

            CommandManager.RegisterCommand( CdKick );

            CommandManager.RegisterCommand( CdRank );

            CommandManager.RegisterCommand( CdHide );
            CommandManager.RegisterCommand( CdUnhide );

            CommandManager.RegisterCommand( CdSetSpawn );

            CommandManager.RegisterCommand( CdFreeze );
            CommandManager.RegisterCommand( CdUnfreeze );

            CommandManager.RegisterCommand( CdTP );
            CommandManager.RegisterCommand( CdBring );
            CommandManager.RegisterCommand( CdWorldBring );
            CommandManager.RegisterCommand( CdBringAll );

            CommandManager.RegisterCommand( CdPatrol );
            CommandManager.RegisterCommand( CdSpecPatrol );

            CommandManager.RegisterCommand( CdMute );
            CommandManager.RegisterCommand( CdUnmute );

            CommandManager.RegisterCommand( CdSpectate );
            CommandManager.RegisterCommand( CdUnspectate );

            CommandManager.RegisterCommand( CdSlap );
            CommandManager.RegisterCommand( CdTPZone );
            CommandManager.RegisterCommand( CdBasscannon );
            CommandManager.RegisterCommand( CdKill );            
            CommandManager.RegisterCommand( CdTempBan );
            CommandManager.RegisterCommand( CdWarn );
            CommandManager.RegisterCommand( CdUnWarn );
            CommandManager.RegisterCommand( CdDisconnect );            
            CommandManager.RegisterCommand( CdImpersonate );
            CommandManager.RegisterCommand( CdImmortal );
            CommandManager.RegisterCommand( CdTitle );
           
            CommandManager.RegisterCommand( CdMuteAll );
            CommandManager.RegisterCommand( CdAssassinate );
            CommandManager.RegisterCommand( CdPunch );
            CommandManager.RegisterCommand( CdBanAll );
            CommandManager.RegisterCommand( CdEconomy );
            CommandManager.RegisterCommand( CdPay );
            CommandManager.RegisterCommand(CdBanGrief);
            CommandManager.RegisterCommand(CdStealthKick);
            CommandManager.RegisterCommand(CdBeatDown);
            CommandManager.RegisterCommand(CdLastCommand);
            CommandManager.RegisterCommand(CdFreezeBring);
            CommandManager.RegisterCommand(CdTempRank);
            CommandManager.RegisterCommand(CdSetClickDistance);
            CommandManager.RegisterCommand(CdAutoRank);
            CommandManager.RegisterCommand(CdAnnounce);
            CommandManager.RegisterCommand(CdForceHold);
            CommandManager.RegisterCommand(CdGetBlock);
            //CommandManager.RegisterCommand(CdTPA);

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

        public static SchedulerTask task;

        static readonly CommandDescriptor CdGetBlock = new CommandDescriptor
        {
            Name = "GetBlock",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Help = "&SReturns the block that the player is currently holding.",
            Usage = "&S/GetBlock [player]",
            Handler = GetBlockHandler
        };

        private static void GetBlockHandler(Player player, Command cmd)
        {
            string target = cmd.Next();
            if (String.IsNullOrEmpty(target))
            {
                CdGetBlock.PrintUsage(player);
                return;
            }

            Player targetPlayer = Server.FindPlayerOrPrintMatches(player, target, false, true);
            if (targetPlayer == null)
            {
                return;
            }

            if (!targetPlayer.usesCPE)
            {
                player.Message("You can only use /GetBlock on ClassiCube players!");
            }

            player.Message("{0} is currently holding {1}.", targetPlayer.Name, targetPlayer.Info.HeldBlock.ToString() );
        }

        static readonly CommandDescriptor CdForceHold = new CommandDescriptor 
        {
            Name = "ForceHold",
            IsConsoleSafe = false,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Help = "&SForces a player to hold a certain block. Only use when needed!",
            Usage = "&S/ForceHold [player] [block]",
            Handler = FHHandler
        };

        private static void FHHandler(Player player, Command cmd)
        {
            if (!Heartbeat.ClassiCube() || !player.usesCPE)
            {
                player.Message("This is a ClassiCube only command!");
                return;
            }
            string target = cmd.Next();
            if (String.IsNullOrEmpty(target))
            {
                CdForceHold.PrintUsage(player);
                return;
            }

            Player p = Server.FindPlayerOrPrintMatches(player, target, false, true);
            if (p == null)
            {
                return;
            }

            if (!p.usesCPE)
            {
                player.Message("You can only use /ForceHold on ClassiCube players!");
            }

            string blockStr = cmd.Next();
            if (String.IsNullOrEmpty(blockStr))
            {
                CdForceHold.PrintUsage(player);
                return;
            }

            //format blockname to be "Stone" instead of "STONE" or "stone"
            blockStr = blockStr.Substring(0, 1).ToUpper() + blockStr.Substring(1).ToLower();
            Block block;
            try
            {
                block = (Block)Enum.Parse(typeof(Block), blockStr);
            }
            catch
            {
                player.Message("Sorry, that was not a valid block name!");
                return;
            }

            p.Send(PacketWriter.MakeHoldThis((byte)block, false));


        }
        static World resetWorld;
        static readonly CommandDescriptor CdAnnounce = new CommandDescriptor //todo: make this work lol
        {
            Name = "Announce",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Say },
            Help = "&SAnnounces a message at the top of every player's screen on a specified world. To send a server-wide announcement, use /Announce all [message]. If duration is blank, announcement will last for 7 seconds.",
            Usage = "&S/Announce [world] [message] [duration]",
            Handler = AnnounceHandler
        };

        public static void AnnounceHandler(Player player, Command cmd)
        {
            if (!Heartbeat.ClassiCube() || !player.usesCPE)
            {
                player.Message("This is a ClassiCube only command!");
                return;
            }

            Player[] targetPlayers;
            string world = cmd.Next();
            if(string.IsNullOrEmpty(world))
            {
                CdAnnounce.PrintUsage(player);
                return;
            }

            if (world == "all")
            {
                targetPlayers = Server.Players;
            }
            else
            {
                World[] targetWorlds = WorldManager.FindWorlds(player, world);
                if (targetWorlds.Length > 1)
                {
                    player.MessageManyMatches("world", targetWorlds);
                    return;
                }
                else if (targetWorlds.Length == 1)
                {
                    targetPlayers = targetWorlds[0].Players;
                }
                else
                {
                    player.Message("No worlds found matching {0}! Please check your spelling.", world);
                    return;
                }
            }

            string message = cmd.Next();
            if (string.IsNullOrEmpty(message))
            {
                CdAnnounce.PrintUsage(player);
                return;
            }

            double reset = 7;
            if (cmd.HasNext)
            {
                string resetStr = cmd.Next();
                Double.TryParse(resetStr, out reset);
            }

            Packet packet = PacketWriter.MakeSpecialMessage(100, message);
            foreach (Player p in targetPlayers)
            {
                if (p.usesCPE)
                {
                    p.Send(packet);
                }
            }

            task.Stop();

            resetWorld = player.World;
            task = Scheduler.NewTask(resetAnnouncement);
            task.RunOnce(TimeSpan.FromSeconds(reset));

        }

        //reset announcements
        static void resetAnnouncement(SchedulerTask task)
        {
            foreach (Player p in resetWorld.Players)
            {
                p.Send(PacketWriter.MakeSpecialMessage(100, "&f"));
            }
        }
         static readonly CommandDescriptor CdAutoRank = new CommandDescriptor 
        {
            Name = "AutoRank",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "&SManagement commands for running Autorank. Type '/help AutoRank [option]' for details.",
            Usage = "&S/AutoRank [enable/disable/force/exempt/unexempt]",
            HelpSections = new Dictionary<string, string>{
                { "enable", "&H/AutoRank enable\n&S" +
                                "Enables the autorank system on the server." },
                { "disable", "&H/AutoRank disable\n&S" +
                                "Disables the autorank system on the server." },
                { "force", "&H/AutoRank force\n&S" +
                                "Forces the server to check all online players if they are due for an autorank. Works even if autorank is disabled."},
                { "exempt", "&H/AutoRank exempt [player]\n&S" +
                                "Exempts a player from autorank. Even if the player is due for an autorank promotion or demotion, they will not be ranked."},
                { "unexempt", "&H/AutoRank unexempt [player]\n&S" +
                                "Lifts the exemption on the target player. Target player can now be autoranked." },               
            },
            Handler = AutoRankHandler
        };

         private static void AutoRankHandler(Player player, Command cmd)
         {
             string option = cmd.Next();
             if (string.IsNullOrEmpty(option))
             {
                 CdAutoRank.PrintUsage(player);
                 return;
             }

             switch (option)
             {
                 case "enable":
                     if (Server.AutoRankEnabled)
                     {
                         player.Message("AutoRank is already enabled!");
                         break;
                     }
                     Server.AutoRankEnabled = true;
                     player.Message("AutoRank is now enabled on {0}!", ConfigKey.ServerName.GetString());
                     break;

                 case "disable":
                     if (!Server.AutoRankEnabled)
                     {
                         player.Message("AutoRank is already disabled!");
                         break;
                     }
                     Server.AutoRankEnabled = false;
                     player.Message("AutoRank is now disabled on {0}!", ConfigKey.ServerName.GetString());
                     break;

                 case "force":
                     player.Message("Checking for online players to force autorank...");

                     //refresh xml incase host updated autorank script, and then check each player
                     AutoRank.AutoRankManager.Load();
                     foreach (Player p in Server.Players)
                     {
                         AutoRank.AutoRankManager.Check(p);
                     }

                     player.Message("AutoRank force check finished.");
                     break;

                 case "exempt":
                     string target = cmd.Next();
                     if (string.IsNullOrEmpty(target))
                     {
                         CdAutoRank.PrintUsage(player);
                         break;
                     }

                     Player targetPlayer = Server.FindPlayerOrPrintMatches(player, target, false, true);
                     if (targetPlayer == null)
                     {
                         return;
                     }

                     targetPlayer.IsAutoRankExempt = true;
                     player.Message("{0} is now exempt from autorank rank changes.", targetPlayer.Name);
                     break;

                 case "unexempt":
                     string target_ = cmd.Next();
                     if (string.IsNullOrEmpty(target_))
                     {
                         CdAutoRank.PrintUsage(player);
                         break;
                     }

                     Player targetPlayer_ = Server.FindPlayerOrPrintMatches(player, target_, false, true);
                     if (targetPlayer_ == null)
                     {
                         return;
                     }

                     targetPlayer_.IsAutoRankExempt = true;
                     player.Message("{0} is now unexempt from autorank rank changes.", targetPlayer_.Name);
                     break;
                 default:
                     CdAutoRank.PrintUsage(player);
                     break;
             }
         }
        static readonly CommandDescriptor CdSetClickDistance = new CommandDescriptor 
        {
            Name = "SetClickDistance",
            Aliases = new[] { "setclick", "clickdistance", "click", "scd" },
            IsConsoleSafe = false,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote },
            Help = "&SSets the clicking distance of a player. NOTE: Block distances are approximate (+/- 1)",
            Usage = "&S/SetClick (number of blocks)",
            Handler = SetClickHandler
        };

        private static void SetClickHandler(Player player, Command cmd)
        {
            if (!Heartbeat.ClassiCube())
            {
                player.Message("This command can only be used on ClassiCube server!");
                return;
            }
            string targetName = cmd.Next();
            if (String.IsNullOrEmpty(targetName))
            {
                CdSetClickDistance.PrintUsage(player);
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
            if (target == null)
            {
                player.MessageNoPlayer(targetName);
                return;
            }
            if (!target.usesCPE)
            {
                player.Message("You can only use /SetClickDistance on ClassiCube players!");
            }
            string number = cmd.Next();
            if (number == "normal")
            {
                target.Send(PacketWriter.MakeSetClickDistance(160));
                player.Message("Player {0}&s's click distance was set to normal.", target.ClassyName);
                target.hasCustomClickDistance = false;
                return;
            }
            int distance;
            if (String.IsNullOrEmpty(number) || !Int32.TryParse(number, out distance))
            {
                CdSetClickDistance.PrintUsage(player);
                return;
            }
            if (distance > 20 || distance < 1)
            {
                player.Message("Accepted values are between 1 and 20!");
                return;
            }
            int adjustedDistance = (32 * distance);
            target.Send(PacketWriter.MakeSetClickDistance(adjustedDistance));
            target.hasCustomClickDistance = true;
            player.Message("Player {0}&s's click distance was set to {1} blocks.", target.ClassyName, distance);
        }
        
        
        static readonly CommandDescriptor CdTempRank = new CommandDescriptor
        {
            Name = "TempRank",
            Aliases = new[] { "tRank"},
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote },
            Help = "&SRanks a player for a limited amount of time. Tempranks are reset upon server restart or shutdown.",
            Usage = "&S/TempRank [player] [rank] [duration] [reason]",
            Handler = TempRankHandler
        };

        static void TempRankHandler(Player player, Command cmd)
        {
            string target = cmd.Next();
            string rank = cmd.Next();
            string rankTime = cmd.Next();
            TimeSpan rankDuration;
            string reason = cmd.Next();

            if(String.IsNullOrEmpty(target))
            {
                player.Message("&SPlease input a player's name to temprank.");
                return;
            }
            if (String.IsNullOrEmpty(rank))
            {
                player.Message("&SPlease input your desired rank.");
                return;
            }
            if (String.IsNullOrEmpty(rankTime))
            {
                player.Message("&SPlease input the duration of the temprank.");
                return;
            }
            if (String.IsNullOrEmpty(reason))
            {
                player.Message("&SPlease input the reason for the temprank.");
                return;
            }

            if (!rankTime.TryParseMiniTimespan(out rankDuration) || rankDuration <= TimeSpan.Zero)
            {
                player.Message("&SThe time must be an integer greater than 0!");
                return;
            }

            Rank targetRank = RankManager.FindRank(rank);
            if (targetRank == null)
            {
                player.MessageNoRank(rank);
                return;
            }
            
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact(target);
            if (targetInfo == null)
            {
                player.Message("&SThe player, {0}&S could not be found. Please check the spelling of the player.", target);
            }

            targetInfo.ChangeRank(player, targetRank, "TempRank(" + rankTime + "): " + reason , true, true, false);
            targetInfo.isTempRanked = true;
            targetInfo.tempRankTime = rankDuration;

        }
        static readonly CommandDescriptor CdTPA = new CommandDescriptor
        {
            Name = "TPA",
            Aliases = new[] { "TeleportAsk"},
            IsConsoleSafe = false,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.TPA },
            Help = "&SAsks a player permission to teleport to them before teleporting.",
            Usage = "&S/Tpa player",
            Handler = TPAHandler
        };

        static void TPAHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            Player player_ = player;
            if (String.IsNullOrEmpty(name))
            {
                CdTPA.PrintUsage(player);
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null)
            {
                return;
            }
            else
            {
                if (!cmd.IsConfirmed)
                {
                    target.Confirm(cmd, player.ClassyName + "&S would like to teleport to you.");
                }
                else
                {
                    //Big error here. Both of the following messages are sent to the target, and the target teleports towards itself.
                    player.Message("&STeleporting you to {0}...", name);
                    target.ParseMessage("/tp " + name, false, false);
                    return;
                }
            }
        }
        static readonly CommandDescriptor CdFreezeBring = new CommandDescriptor
        {
            Name = "FreezeBring",
            Aliases = new[] { "fBring" , "fSummon", "fb" },
            IsConsoleSafe = false,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Freeze },
            Help = "&SFreezes and summons a player to your location. Usefull for griefers.",
            Usage = "&S/FreezeBring player",
            Handler = FBHandler
        };

        static void FBHandler(Player player, Command cmd) 
        {
            string name = cmd.Next();
            if (String.IsNullOrEmpty(name))
            {
                CdFreezeBring.PrintUsage(player);
                return;
            }
            else
            {
                Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
                if (target == null)
                {
                    return;
                }
                else
                {
                    player.ParseMessage("/summon " + name, false, false); //to lazy to change target's coords, so ill just summon
                    target.Info.IsFrozen = true;
                    target.Message("&SYou have been frozen by {0}", player.ClassyName);
                    return;
                }
            }
        }
        static readonly CommandDescriptor CdLastCommand = new CommandDescriptor
        {
            Name = "LastCommand",
            Aliases = new[] { "LastCmd", "last" },
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Help = "Checks the last command used by a player. Leave the player parameter blank for your last command.",
            Usage = "/LastCommand (player)",
            Handler = LastCommandHandler
        };

        static void LastCommandHandler(Player player, Command cmd) //allows the user to see the last command a target player has used
        {
            string target = cmd.Next();
            if (String.IsNullOrEmpty(target))
            {
                if (player.LastCommand == null)
                {
                    player.Message("You do not have a last command");
                    CdLastCommand.PrintUsage(player);
                    return;
                }

                string sub = player.LastCommand.ToString().Split('"')[1].Split('"')[0];

                player.Message("Your last command used was: " + sub);
                return;
            }
            Player targetName = Server.FindPlayerOrPrintMatches(player, target, false, true);
            if (targetName == null)
            {
                player.Message("No player found matching " + target);
                return;
            }
            else
            {
                string sub = targetName.LastCommand.ToString().Split('"')[1].Split('"')[0];

                player.Message(targetName.Name + "'s last command was " + sub);
                return;
            }
        }
        static readonly CommandDescriptor CdBeatDown = new CommandDescriptor
        {
            Name = "Beatdown",
            IsConsoleSafe = true,
            NotRepeatable = true,
            Aliases = new[] { "ground", "bd", "pummel" },
            Category = CommandCategory.Moderation | CommandCategory.Fun,
            Permissions = new[] { Permission.Basscannon },
            Help = "Pummels a player into the ground. " +
            "Available items are: dog, hammer, sack, pistol, curb and soap.",
            Usage = "/Beatdown <playername> [item]",
            Handler = BeatDownHandler
        };

        static void BeatDownHandler(Player player, Command cmd) //a more brutal /slap cmd, sends the player underground to the bottom of the world
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
                player.Message("&SYou failed to beatdown {0}&S, they are immortal", target.ClassyName);
                return;
            }
            if (target == player)
            {
                player.Message("&sYou can't pummel yourself.... What's wrong with you???");
                return;
            }
            double time = (DateTime.Now - player.Info.LastUsedBeatDown).TotalSeconds;
            double timeLeft = Math.Round(20 - time);
            if (time < 20)
            {
                player.Message("&WYou can use /BeatDown again in " + timeLeft + " seconds.");
                return;
            }
            string aMessage;
            if (player.Can(Permission.Basscannon, target.Info.Rank))
            {
                Position pummeled = new Position(target.Position.X, target.Position.Y, (target.World.Map.Bounds.ZMin) * 32);
                target.previousLocation = target.Position;
                target.previousWorld = null;
                target.TeleportTo(pummeled);

                if (string.IsNullOrEmpty(item))
                {
                    Server.Players.CanSee(target).Union(target).Message("{0} &Swas pummeled into the ground by {1}", target.ClassyName, player.ClassyName);
                    target.Message("Do &a/Spawn&s to get back above ground.");
                    IRC.PlayerSomethingMessage(player, "beat down", target, null);
                    player.Info.LastUsedBeatDown = DateTime.Now;
                    return;
                }
                else if (item.ToLower() == "hammer")
                    aMessage = String.Format("{1}&S Beat Down {0}&S with a Hammer", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "sack")
                    aMessage = String.Format("{1}&s pummeled {0}&S with a Sack of Potatoes", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "soap")
                    aMessage = String.Format("{1}&S pummeled {0}&s with Socks Full of Soap", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "pistol")
                    aMessage = String.Format("{1}&S Pistol-Whipped {0}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "curb")
                    aMessage = String.Format("{1}&S Curb-Stomped {0}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "dog")
                    aMessage = String.Format("{1}&S Beat Down {0}&S like a dog", target.ClassyName, player.ClassyName);
                else
                {
                    Server.Players.CanSee(target).Union(target).Message("{0} &Swas pummeled into the ground by {1}", target.ClassyName, player.ClassyName);
                    target.Message("Do &a/Spawn&s to get back above ground.");
                    IRC.PlayerSomethingMessage(player, "beat down", target, null);
                    player.Info.LastUsedBeatDown = DateTime.Now;
                    return;
                }
                Server.Players.CanSee(target).Union(target).Message(aMessage);
                target.Message("Do &a/Spawn&s to get back above ground.");
                IRC.PlayerSomethingMessage(player, "beat down", target, null);
                player.Info.LastUsedBeatDown = DateTime.Now;
                return;
            }
            else
            {
                player.Message("&sYou can only Beat Down players ranked {0}&S or lower",
                               player.Info.Rank.GetLimit(Permission.Basscannon).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }
        
        static readonly CommandDescriptor CdStealthKick = new CommandDescriptor
        {
            Name = "StealthKick",
            Aliases = new[] { "sk", "stealthk" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Gtfo },
            Usage = "/StealthKick (playername)",
            Help = "&SKicks a player stealthily. The kick will say the player disconnected and will not save to the playerDB.",
            Handler = StealthKickHandler
        };

        internal static void StealthKickHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;

            if (target == player)
            {
                player.Message("You cannot StealthKick yourself.");
                return;
            }

            if (cmd.HasNext)
            {
                player.Message("A reason does not need to be specified when using StealthKick.");
            }

            if (player.Can(Permission.Gtfo, target.Info.Rank))
            {
                Player targetPlayer = target;
                target.Send(PacketWriter.MakeDisconnect("You've lost connection to the server."));
            }
            else
            {
                player.Message("You can only StealthKick players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Gtfo).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }
        
        static CommandDescriptor CdBanGrief = new CommandDescriptor
        {
            Name = "BanGrief",
            Category = CommandCategory.Moderation,
            Aliases = new[] { "Bg" },
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            NotRepeatable = true,
            Usage = "/BanGrief Playername",
            Help = "Bans the player with the reason \"Grief, appeal at <website>\"",
            Handler = BanGriefHandler
        };

        static void BanGriefHandler(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdBanGrief.PrintUsage(player);
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetName);
            if (target == null) return;
            string reason = "Grief, appeal at " + ConfigKey.WebsiteURL.GetString();
            try
            {
                Player targetPlayer = target.PlayerObject;
                target.Ban(player, reason, true, true);
                WarnIfOtherPlayersOnIP(player, target, targetPlayer);
            }
            catch (PlayerOpException ex)
            {
                player.Message(ex.MessageColored);
                if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                {
                    FreezeIfAllowed(player, target);
                }
            }
        }

        #region Economy

        static readonly CommandDescriptor CdPay = new CommandDescriptor
        {
            Name = "Pay",
            Aliases = new[] { "Purchase" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Economy },
            Usage = "/pay player amount",
            Help = "&SUsed to pay a certain player an amount of bits.",
            Handler = PayHandler
        };

        static void PayHandler(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            string amount = cmd.Next();
            int amountnum;
            //lotsa idiot proofing in this one ^.^

            if (targetName == null)
            {
                player.Message("&ePlease type in a player's name to pay bits towards.");
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
            if (target == player)
            {
                player.Message("You can't pay yourself >.> Doesn't work like that.");
                return;
            }

            if (target == null)
            {
                return;
            }
            else
            {
                if (!int.TryParse(amount, out amountnum))
                {
                    player.Message("&eThe amount must be a positive whole number!");
                    return;
                }
                if (amountnum < 1)
                {
                    player.Message("&eThe amount must be a positive whole number!");
                    return;
                }

                if (cmd.IsConfirmed)
                {
                    if (amountnum > player.Info.Money)
                    {
                        player.Message("You don't have that many bits!");
                        return;
                    }
                    else
                    {
                        //show him da monai
                        int pNewMoney = player.Info.Money - amountnum;
                        int tNewMoney = target.Info.Money + amountnum;
                        player.Message("&eYou have paid &C{1}&e to {0}&e.", target.ClassyName, amountnum);
                        target.Message("&e{0} &ehas paid you {1} &ebit(s).", player.ClassyName, amountnum);
                        Server.Players.Except(target).Except(player).Message("&e{0} &ewas paid {1} &ebit(s) from {2}&e.", target.ClassyName, amountnum, player.ClassyName);
                        player.Info.Money = pNewMoney;
                        target.Info.Money = tNewMoney;
                        return;
                    }
                }
                else
                {
                    player.Confirm(cmd, "&eAre you sure you want to pay {0}&e {1} &ebits?", target.ClassyName, amountnum);
                    return;
                }


            }
        }

        static readonly CommandDescriptor CdEconomy = new CommandDescriptor
        {
            Name = "Economy",
            Aliases = new[] { "Money", "Econ" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Economy },
            Usage = "/Economy [give/take/show/pay] [playername] [pay/give/take: amount]",
            Help = "&SEconomy commands for LegendCraft. Show will show you the amount of money a player has" +
            "and give/take will give or take bits from or to a player. WARNING, give and take will change your server's inflation.",
            Handler = EconomyHandler
        };

        static void EconomyHandler(Player player, Command cmd)
        {
            try
            {
                string option = cmd.Next();
                string targetName = cmd.Next();
                string amount = cmd.Next();
                int amountnum;
                if (option == null)
                {
                    CdEconomy.PrintUsage(player);
                }
                if (option == "give")
                {
                    if (!player.Can(Permission.ManageEconomy))
                    {
                        player.Message("You do not have the required permisions to use that command!");
                        return;
                    }
                    Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
                    if (targetName == null)
                    {
                        player.Message("&ePlease type in a player's name to give bits towards.");
                        return;
                    }
                    if (target == null)
                    {
                        return;
                    }
                    else
                    {
                        if (!int.TryParse(amount, out amountnum))
                        {
                            player.Message("&ePlease select from a whole number.");
                            return;
                        }
                        if (cmd.IsConfirmed)
                        {
                            //actually give the player the money
                            int tNewMoney = target.Info.Money + amountnum;
                            player.Message("&eYou have given {0} &C{1} &ebit(s).", target.ClassyName, amountnum);
                            target.Message("&e{0} &ehas given you {1} &ebit(s).", player.ClassyName, amountnum);
                            Server.Players.Except(target).Except(player).Message("&e{0} &ewas given {1} &ebit(s) from {2}&e.", target.ClassyName, amountnum, player.ClassyName);
                            target.Info.Money = tNewMoney;
                            return;
                        }
                        else
                        {
                            player.Confirm(cmd, "&eAre you sure you want to give {0} &C{1} &ebits?&s", target.ClassyName, amountnum);
                            return;
                        }

                    }
                }
                if (option == "take")
                {
                    if (!player.Can(Permission.ManageEconomy))
                    {
                        player.Message("You do not have the required permisions to use that command!");
                        return;
                    }
                    Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
                    if (targetName == null)
                    {
                        player.Message("&ePlease type in a player's name to take bits away from.");
                        return;
                    }
                    if (target == null)
                    {
                        return;
                    }
                    else
                    {
                        if (!int.TryParse(amount, out amountnum))
                        {
                            player.Message("&eThe amount must be a number!");
                            return;
                        }

                        if (cmd.IsConfirmed)
                        {
                            if (amountnum > target.Info.Money)
                            {
                                player.Message("{0}&e doesn't have that many bits!", target.ClassyName);
                                return;
                            }
                            else
                            {
                                //actually give the player the money
                                int tNewMoney = target.Info.Money - amountnum;
                                player.Message("&eYou have taken &c{1}&e from {0}.", target.ClassyName, amountnum);
                                target.Message("&e{0} &ehas taken {1} &ebit(s) from you.", player.ClassyName, amountnum);
                                Server.Players.Except(target).Except(player).Message("&e{0} &etook {1} &ebit(s) from {2}&e.", player.ClassyName, amountnum, target.ClassyName);
                                target.Info.Money = tNewMoney;
                                return;
                            }
                        }
                        else
                        {
                            player.Confirm(cmd, "&eAre you sure you want to take &c{1} &ebits from {0}&e?&s", target.ClassyName, amountnum);
                            return;
                        }

                    }


                }

                if (option == "pay")
                {
                    //lotsa idiot proofing in this one ^.^

                    if (targetName == null)
                    {
                        player.Message("&ePlease type in a player's name to pay bits towards.");
                        return;
                    }
                    Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
                    if (target == player)
                    {
                        player.Message("You can't pay yourself >.> Doesn't work like that.");
                        return;
                    }

                    if (target == null)
                    {
                        return;
                    }
                    else
                    {
                        if (!int.TryParse(amount, out amountnum))
                        {
                            player.Message("&eThe amount must be a positive whole number!");
                            return;
                        }
                        if (amountnum < 1)
                        {
                            player.Message("&eThe amount must be a positive whole number!");
                            return;
                        }

                        if (cmd.IsConfirmed)
                        {
                            if (amountnum > player.Info.Money)
                            {
                                player.Message("You don't have that many bits!");
                                return;
                            }
                            else
                            {
                                //show him da monai
                                int pNewMoney = player.Info.Money - amountnum;
                                int tNewMoney = target.Info.Money + amountnum;
                                player.Message("&eYou have paid &C{1}&e to {0}&e.", target.ClassyName, amountnum);
                                target.Message("&e{0} &ehas paid you {1} &ebit(s).", player.ClassyName, amountnum);
                                Server.Players.Except(target).Except(player).Message("&e{0} &ewas paid {1} &ebit(s) from {2}&e.", target.ClassyName, amountnum, player.ClassyName);
                                player.Info.Money = pNewMoney;
                                target.Info.Money = tNewMoney;
                                return;
                            }
                        }
                        else
                        {
                            player.Confirm(cmd, "&eAre you sure you want to pay {0}&e {1} &ebits?&s", target.ClassyName, amountnum);
                            return;
                        }


                    }
                }

                else if (option == "show")
                {
                    Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);
                    if (targetName == null)
                    {
                        player.Message("&ePlease type in a player's name to see how many bits they have.");
                        return;
                    }

                    if (target == null)
                    {
                        return;
                    }
                    else
                    {
                        //actually show how much money that person has
                        player.Message("&e{0}&e has &C{1}&e bits currently!", target.ClassyName, target.Info.Money);
                    }

                }
                else
                {
                    player.Message("&eValid choices are '/economy take', '/economy give', '/economy show' and '/econ pay'.");
                    return;
                }
            }
            catch (ArgumentNullException)
            {
                CdEconomy.PrintUsage(player);
            }
        }
        #endregion


        static readonly CommandDescriptor CdBanAll = new CommandDescriptor
        {
            Name = "BanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/BanAll PlayerName|IPAddress [Reason]",
            Help = "&SBans the player's name, IP, and all other names associated with the IP. " +
                   "UndoAll's the playername. If player is not online, " +
                   "the last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAllHandler
        };

        static void BanAllHandler(Player player, Command cmd)
        {
            string targetNameOrIP = cmd.Next();
            if (targetNameOrIP == null)
            {
                CdBanAll.PrintUsage(player);
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if (Server.IsIP(targetNameOrIP) && IPAddress.TryParse(targetNameOrIP, out targetAddress))
            {
                try
                {
                    targetAddress.BanAll(player, reason, true, true);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                }
            }
            else
            {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetNameOrIP);
                if (target == null) return;
                try
                {
                    if (target.LastIP.Equals(IPAddress.Any) || target.LastIP.Equals(IPAddress.None))
                    {
                        target.Ban(player, reason, true, true);
                    }
                    else
                    {
                        target.BanAll(player, reason, true, true);
                        BuildingCommands.UndoAllHandler(player, new Command("/UndoAll " + target.Name));
                    }
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                    {
                        FreezeIfAllowed(player, target);
                    }
                }
            }
        }

        static readonly CommandDescriptor CdAssassinate = new CommandDescriptor
        {
            Name = "Assasinate",
            Category = CommandCategory.Fun | CommandCategory.Fun,
            Aliases = new[] { "Snipe", "Assassinate" },
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kill },
            Help = "Silently kills a player.",
            NotRepeatable = true,
            Usage = "/assassinate playername",
            Handler = AssassinateHandler
        };

        internal static void AssassinateHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name.");
                return;
            }
            if (!player.Info.IsHidden)
            {
                player.Message("You can only assassinate while hidden silly head.");
                return;
            }
            else
            {

                Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
                if (target == null) return;
                if (target.Immortal)
                {
                    player.Message("&SYou can't assassinate {0}&S, they're immortal.", target.ClassyName);
                    return;
                }

                if (player.Can(Permission.Kill, target.Info.Rank))
                {
                    target.TeleportTo(player.World.Map.Spawn);

                    target.Message("&8You were just assassinated!");

                    player.Message("&8Successfully assassinated {0}.", target.ClassyName);
                }
                if (target == player)
                {
                    Server.Message("{0} killed itself in confusion!", player.ClassyName);
                    return;
                }

                if (target == null)
                {
                    player.Message("Please enter a victim's name.");
                    return;
                }
                else
                {
                    if (player.Can(Permission.Kill, target.Info.Rank))
                    {
                        target.TeleportTo(player.World.Map.Spawn);

                    }
                    else
                    {
                        player.Message("You can only assassinate players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit(Permission.Kill).ClassyName);
                        player.Message("{0}&S is ranked {1}.", target.ClassyName, target.Info.Rank.ClassyName);
                    }
                }
            }
        }



        static readonly CommandDescriptor CdPunch = new CommandDescriptor
        {
            Name = "Punch",
            Aliases = new string[] { "Pu" },
            Category = CommandCategory.Chat | CommandCategory.Fun,
            Permissions = new[] { Permission.Brofist },
            IsConsoleSafe = true,
            Help = "&aPunches &Sa player. " +
        "Availble items are: groin, stomach, tits, and knockout.\n" +
        "NOTE: Items are optional.",
            Usage = "/Punch playerName item",
            Handler = PunchHandler
        };


        static void PunchHandler(Player player, Command cmd)
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
                player.Message("&SYou failed to &aPunch {0}&S, they are immortal", target.ClassyName);
                return;
            }
            if (target == player)
            {
                player.Message("&sYou can't &aPunch &syourself, Weirdo!");
                return;
            }
            double time = (DateTime.Now - player.Info.LastUsedSlap).TotalSeconds;
            if (time < 10)
            {
                player.Message("&WYou can use /Punch again in " + Math.Round(10 - time) + " seconds.");
                return;
            }
            string aMessage;
            if (player.Can(Permission.Slap, target.Info.Rank))
            {
                Position slap = new Position(target.Position.X, target.Position.Y, (target.World.Map.Bounds.ZMax) * 32);
                target.TeleportTo(slap);
                if (string.IsNullOrEmpty(item))
                {
                    Server.Players.CanSee(target).Union(target).Message("{0} &Swas &aPunched &Sin the &cFace &Sby {1}", target.ClassyName, player.ClassyName);
                    IRC.PlayerSomethingMessage(player, "punched", target, null);
                    player.Info.LastUsedSlap = DateTime.Now;
                    return;
                }
                else if (item.ToLower() == "groin")
                    aMessage = String.Format("{0} &Swas &aPunched &Sin the &cGroin &Sby {1}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "stomach")
                    aMessage = String.Format("{0} &Swas &aPunched &Sin the &cStomach &Sby {1}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "tits")
                    aMessage = String.Format("{0} &Swas &aPunched &Sright in the &cTits &Sby {1}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "knockout")
                    aMessage = String.Format("{0} &Swas &cKnocked Out &Sby {1}", target.ClassyName, player.ClassyName);
                else
                {
                    Server.Players.CanSee(target).Union(target).Message("{0} &Swas &aPunched &Sin the &cFace &Sby {1}", target.ClassyName, player.ClassyName);
                    IRC.PlayerSomethingMessage(player, "punched", target, null);
                    player.Info.LastUsedSlap = DateTime.Now;
                    return;
                }
                Server.Players.CanSee(target).Union(target).Message(aMessage);
                IRC.PlayerSomethingMessage(player, "punched", target, null);
                player.Info.LastUsedSlap = DateTime.Now;
                return;
            }
            else
            {
                player.Message("&sYou can only Punch players ranked {0}&S or lower",
                               player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdMuteAll = new CommandDescriptor
        {
            Name = "MuteAll",
            Aliases = new[] { "quiet" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.MuteAll },
            Usage = "/Muteall [duration] [s/h/d/w]",
            Help = "Mutes all chat feed for the specified time. 30 seconds is the time limit. To unmuteall, type /muteall 1s",
            Handler = MuteAllHandler
        };

        internal static void MuteAllHandler(Player player, Command cmd)
        {
            string timeString = cmd.Next();
            TimeSpan duration;
            // validate command parameters
            if (String.IsNullOrEmpty(timeString) ||
                    !timeString.TryParseMiniTimespan(out duration) || duration <= TimeSpan.Zero)
            {
                player.Message("&H/Muteall Duration");
                return;
            }
            TimeSpan MaxMuteDuration = TimeSpan.FromSeconds(30);
            // check if given time exceeds maximum (30 seconds)
            if (duration > MaxMuteDuration)
            {
                player.Message("Maximum mute duration is {0}.", MaxMuteDuration.ToMiniString());
                duration = MaxMuteDuration;

            } //check to keep limit under

            foreach (Player target in Server.Players.Where(p => p.World != null && p != player))
            {
                // actually mute
                try
                {

                    target.Info.Mute(player, duration, false, true);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                }
            }
            string message = cmd.Next();
            if (message == null)
            {
                Server.Message(" {0}&W has muted the chat feed for {1}&W seconds!", player.ClassyName, timeString);
            }
        }

        #endregion

        #region 800Craft

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
        public static List<string> BassText = new List<string>();

        static readonly CommandDescriptor CdTitle = new CommandDescriptor
        {
            Name = "Title",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Usage = "/Title <Playername> <Title>",
            Help = "&SChanges or sets a player's title.",
            Handler = TitleHandler
        };

        static void TitleHandler(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            string titleName = cmd.NextAll();

            if (string.IsNullOrEmpty(targetName))
            {
                CdTitle.PrintUsage(player);
                return;
            }

            PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetName);
            if (info == null) return;
            string oldTitle = info.TitleName;
            if (titleName.Length == 0) titleName = null;
            if (titleName == info.TitleName)
            {
                if (titleName == null)
                {
                    player.Message("Title: Title for {0} is not set.",
                                    info.Name);
                }
                else
                {
                    player.Message("Title: Title for {0} is already set to \"{1}&S\"",
                                    info.Name,
                                    titleName);
                }
                return;
            }
            //check the title, is it a title?
            if (titleName != null)
            {
                string StripT = Color.StripColors(titleName);
                if (!StripT.StartsWith("[") && !StripT.EndsWith("]"))
                {
                    titleName = info.Rank.Color + "[" + titleName + info.Rank.Color + "] ";
                }
            }
            info.TitleName = titleName;

            if (oldTitle == null)
            {
                player.Message("Title: Title for {0} set to \"{1}&S\"",
                                info.Name,
                                titleName);
            }
            else if (titleName == null)
            {
                player.Message("Title: Title for {0} was reset (was \"{1}&S\")",
                                info.Name,
                                oldTitle);
            }
            else
            {
                player.Message("Title: Title for {0} changed from \"{1}&S\" to \"{2}&S\"",
                                info.Name,
                                oldTitle,
                                titleName);
            }
        }

        static readonly CommandDescriptor CdImmortal = new CommandDescriptor
        {
            Name = "Immortal",
            Aliases = new [] { "Invincible", "God" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Immortal },
            Help = "Stops death by all things.",
            NotRepeatable = true,
            Usage = "/Immortal",
            Handler = ImmortalHandler
        };

        internal static void ImmortalHandler(Player player, Command cmd)
        {
            if (player.Immortal){
                player.Immortal = false;
                Server.Players.Message("{0}&S is no longer Immortal", player.ClassyName);
                return;
            }
            player.Immortal = true;
            Server.Players.Message("{0}&S is now Immortal", player.ClassyName);
        }
        
        
        static readonly CommandDescriptor CdKill = new CommandDescriptor
        {
            Name = "Kill",
            Category = CommandCategory.Moderation | CommandCategory.Fun,
            Aliases = new[] { "Slay" },
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Kill },
            Help = "Kills a player.",
            NotRepeatable = true,
            Usage = "/Kill playername",
            Handler = KillHandler
        };

        internal static void KillHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            string reason = cmd.NextAll();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;
            if (target.Immortal)
            {
                player.Message("&SYou failed to kill {0}&S, they are immortal", target.ClassyName);
                return;
            }

            double time = (DateTime.Now - player.Info.LastUsedKill).TotalSeconds;
            if (time < 10)
            {
                player.Message("&WYou can use /Kill again in " + Math.Round(10 - time) + " seconds.");
                return;
            }
            if (target == null)
            {
                player.Message("You need to enter a player name to Kill");
                return;
            }
            else 
            {
                if (target == player)
                {
                    player.TeleportTo(player.World.Map.Spawn);
                    player.Info.LastUsedKill = DateTime.Now;
                    Server.Players.CanSee(target).Message("{0}&C killed itself in confusion!", player);
                    return;
                }

                if (player.Can(Permission.Kill, target.Info.Rank) && reason.Length < 1)
                {
                    target.TeleportTo(player.World.Map.Spawn);
                    player.Info.LastUsedKill = DateTime.Now;
                    Server.Players.CanSee(target).Message("{0}&C was &4Killed&C by {1}", target.ClassyName, player.ClassyName);
                    return;
                }
                else if (player.Can(Permission.Kill, target.Info.Rank) && reason != null)
                {
                    target.TeleportTo(player.World.Map.Spawn);
                    player.Info.LastUsedKill = DateTime.Now;
                    Server.Players.CanSee(target).Message("{0}&C was &4Killed&C by {1}&c: {2}", target.ClassyName, player.ClassyName, reason);
                }
                else
                {
                    player.Message("You can only Kill players ranked {0}&S or lower",
                                    player.Info.Rank.GetLimit(Permission.Kill).ClassyName);
                    player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
                }
            }
        }
        
        static readonly CommandDescriptor CdSlap = new CommandDescriptor
        {
            Name = "Slap",
            IsConsoleSafe = true,
            NotRepeatable = true,
            Aliases = new[] { "Sky" },
            Category = CommandCategory.Moderation | CommandCategory.Fun,
            Permissions = new[] { Permission.Slap },
            Help = "Slaps a player to the sky. " +
            "Available items are: bakingtray, fish, bitchslap, shoe, fryingpan, ho, noodle, and ass.",
            Usage = "/Slap <playername> [item]",
            Handler = Slap
        };

        static void Slap(Player player, Command cmd)
        {
            string name = cmd.Next();
            string item = cmd.Next();
            if (name == null){
                player.Message("Please enter a name");
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;
            if (target.Immortal){
                player.Message("&SYou failed to slap {0}&S, they are immortal", target.ClassyName);
                return;
            }
            if (target == player){
                player.Message("&sYou can't slap yourself.... What's wrong with you???");
                return;
            }
            double time = (DateTime.Now - player.Info.LastUsedSlap).TotalSeconds;
            if (time < 10){
                player.Message("&WYou can use /Slap again in " + Math.Round(10 - time) + " seconds.");
                return;
            }
            string aMessage;
            if (player.Can(Permission.Slap, target.Info.Rank)){
                Position slap = new Position(target.Position.X, target.Position.Y, (target.World.Map.Bounds.ZMax) * 32);
                target.previousLocation = target.Position;
                target.previousWorld = null;
                target.TeleportTo(slap);
                if (string.IsNullOrEmpty(item)){
                    Server.Players.CanSee(target).Union(target).Message("{0} &Swas slapped sky high by {1}", target.ClassyName, player.ClassyName);
                    IRC.PlayerSomethingMessage(player, "slapped", target, null);
                    player.Info.LastUsedSlap = DateTime.Now;
                    return;
                }
                else if (item.ToLower() == "bakingtray")
                    aMessage = String.Format("{0} &Swas slapped by {1}&S with a Baking Tray", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "fish")
                    aMessage = String.Format("{0} &Swas slapped by {1}&S with a Giant Fish", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "bitchslap")
                    aMessage = String.Format("{0} &Swas bitch-slapped by {1}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "shoe")
                    aMessage = String.Format("{0} &Swas slapped by {1}&S with a Shoe", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "ass")
                    aMessage = String.Format("{0} &Swas slapped on the &0Ass&s by {1}", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "fryingpan")
                    aMessage = String.Format("{0} &Swas slapped by {1}&S with a Frying Pan", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "noodle")
                    aMessage = String.Format("{0} &Swas slapped by {1}&S with a Wet Noodle", target.ClassyName, player.ClassyName);
                else if (item.ToLower() == "ho")
                    aMessage = String.Format("{1} &Sslapped {0}&S like a Pimp slaps a Ho", target.ClassyName, player.ClassyName);
                else{
                    Server.Players.CanSee(target).Union(target).Message("{0} &Swas slapped sky high by {1}", target.ClassyName, player.ClassyName);
                    IRC.PlayerSomethingMessage(player, "slapped", target, null);
                    player.Info.LastUsedSlap = DateTime.Now;
                    return;
                }
                Server.Players.CanSee(target).Union(target).Message(aMessage);
                IRC.PlayerSomethingMessage(player, "slapped", target, null);
                player.Info.LastUsedSlap = DateTime.Now;
                return;
            }else{
                player.Message("&sYou can only Slap players ranked {0}&S or lower",
                               player.Info.Rank.GetLimit(Permission.Slap).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdTPZone = new CommandDescriptor
        {
            Name = "Tpzone",
            IsConsoleSafe = false,
            Aliases = new[] { "tpz", "zonetp" },
            Category = CommandCategory.World | CommandCategory.Zone,
            Permissions = new[] { Permission.Teleport },
            Help = "Teleports you to the centre of a Zone listed in /Zones.",
            Usage = "/tpzone ZoneName",
            Handler = TPZone
        };

        static void TPZone(Player player, Command cmd)
        {
            string zoneName = cmd.Next();
            if (zoneName == null){
                player.Message("No zone name specified. See &W/Help tpzone");
                return;
            }else{
                Zone zone = player.World.Map.Zones.Find(zoneName);
                if (zone == null){
                    player.MessageNoZone(zoneName);
                    return;
                }
                Position zPos = new Position((((zone.Bounds.XMin + zone.Bounds.XMax) / 2) * 32),
                    (((zone.Bounds.YMin + zone.Bounds.YMax) / 2) * 32),
                    (((zone.Bounds.ZMin + zone.Bounds.ZMax) / 2) + 2) * 32);
                player.TeleportTo((zPos));
                player.Message("&WTeleporting you to zone " + zone.ClassyName);
            }
        }

        static readonly CommandDescriptor CdImpersonate = new CommandDescriptor
        {
            Name = "Impersonate",
            Category = CommandCategory.Moderation | CommandCategory.Fun,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "&SChanges to players skin to a desired name. " + 
            "If no playername is given, all changes are reverted. " + 
            "Note: The name above your head changes too",
            Usage = "/Impersonate PlayerName",
            Handler = ImpersonateHandler
        };

        static void ImpersonateHandler(Player player, Command cmd)
        {
            //entityChanged should be set to true for the skin update to happen in real time
            string iName = cmd.Next();
            if (iName == null && player.iName == null)
            {
                CdImpersonate.PrintUsage(player);
                return;
            }
            if (iName == null)
            {
                player.iName = null;
                player.entityChanged = true;
                player.Info.tempDisplayedName = null;
                player.Message("&SAll changes have been removed and your skin has been updated");
                return;
            }
            //ignore isvalidname for percent codes to work
            if (player.iName == null)
            {
                player.Message("&SYour name has changed from '" + player.Info.Rank.Color + player.Name + "&S' to '" + iName + "&S'");
            }
            if (player.iName != null)
            {
                player.Message("&SYour name has changed from '" + player.iName + "&S' to '" + iName + "&S'");
            }
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact(iName);

            try
            {
                player.iName = targetInfo.Rank.Color + targetInfo.Name;

                if (targetInfo.DisplayedName != null)
                    player.Info.tempDisplayedName = targetInfo.DisplayedName;
                else
                    player.Info.tempDisplayedName = targetInfo.Rank.Color + targetInfo.Name;
            }
            catch 
            {
                player.iName = RankManager.LowestRank.Color + iName;
                player.Info.tempDisplayedName = RankManager.LowestRank.Color + iName;
            }
            player.entityChanged = true;
        }

        static readonly CommandDescriptor CdTempBan = new CommandDescriptor
        {
            Name = "Tempban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Aliases = new[] { "tban" },
            Permissions = new[] { Permission.TempBan },
            Help = "Bans a player for a selected amount of time. Example: 10s | 10m | 10h ",
            Usage = "/Tempban Player Duration",
            Handler = Tempban
        };

        static void Tempban(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;
            try{
                if (String.IsNullOrEmpty(targetName) || String.IsNullOrEmpty(timeString) ||
                !timeString.TryParseMiniTimespan(out duration) || duration <= TimeSpan.Zero)
                {
                    CdTempBan.PrintUsage(player);
                    return;
                }
            }catch (OverflowException){
                player.Message("TempBan: Given duration is too long.");
                return;
            }

            // find the target
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, targetName);

            if (target == null){
                player.MessageNoPlayer(targetName);
                return;
            };

            if (target.Name == player.Name){
                player.Message("Trying to T-Ban yourself? Fail!");
                return;
            }

            // check permissions
            if (!player.Can(Permission.BanIP, target.Rank)){
                player.Message("You can only Temp-Ban players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit(Permission.BanIP).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Rank.ClassyName);
                return;
            }

            // do the banning
            if (target.Tempban(player.Name, duration)){
                string reason = cmd.NextAll();
                try{
                    Player targetPlayer = target.PlayerObject;
                    target.Ban(player, "You were Banned for " + timeString, false, true);
                    Server.TempBans.Add(targetPlayer);
                }
                catch (PlayerOpException ex){
                    player.Message(ex.MessageColored);
                }
                Scheduler.NewTask(t => Untempban(player, target)).RunOnce(duration);
                Server.Message("&SPlayer {0}&S was Banned by {1}&S for {2}",
                                target.ClassyName, player.ClassyName, duration.ToMiniString());
                if (reason.Length > 0) Server.Message("&Wreason: {0}", reason);
                Logger.Log(LogType.UserActivity, "Player {0} was Banned by {1} for {2}",
                            target.Name, player.Name, duration.ToMiniString());
            }else{
                player.Message("Player {0}&S is already Banned by {1}&S for {2:0} more.",
                                target.ClassyName,
                                target.BannedBy,
                                target.BannedUntil.Subtract(DateTime.UtcNow).ToMiniString());
            }
        }

        public static void Untempban(Player player, PlayerInfo target)
        {
            if (!target.IsBanned) return;
            else
                target.Unban(player, "Tempban Expired", true, true);
        }

        static readonly CommandDescriptor CdBasscannon = new CommandDescriptor
        {
            Name = "Basscannon",
            Category = CommandCategory.Moderation | CommandCategory.Fun,
            IsConsoleSafe = true,
            Aliases = new[] { "bc" },
            IsHidden = false,
            Permissions = new[] { Permission.Basscannon },
            Usage = "Let the Basscannon 'Kick' it!",
            Help = "A classy way to kick players from the server",
            Handler = Basscannon
        };

        internal static void Basscannon(Player player, Command cmd)
        {
            string name = cmd.Next();
            string reason = cmd.NextAll();

            if (name == null)
            {
                player.Message("Please enter a player name to use the basscannon on.");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null)
            {
                return;
            }

            if (ConfigKey.RequireKickReason.Enabled() && String.IsNullOrEmpty(reason))
            {
                player.Message("&WPlease specify a reason: &W/Basscannon PlayerName Reason");
                // freeze the target player to prevent further damage
                return;
            }

            if (player.Can(Permission.Kick, target.Info.Rank))
            {
                target.Info.IsHidden = false;

                try
                {
                    Player targetPlayer = target;
                    target.BassKick(player, reason, LeaveReason.Kick, true, true, true);
                    if (BassText.Count < 1){
                        BassText.Add("Flux Pavillion does not approve of your behavior");
                        BassText.Add("Let the Basscannon KICK IT!");
                        BassText.Add("WUB WUB WUB WUB WUB WUB!");
                        BassText.Add("Basscannon, Basscannon, Basscannon, Basscannon!");
                        BassText.Add("Pow pow POW!!!");
                    }
                    string line = BassText[new Random().Next(0, BassText.Count)].Trim();
                    if (line.Length == 0) return;
                    Server.Message("&9{0}", line);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                        return;
                }
            }
            else
            {
                player.Message("You can only use /Basscannon on players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Kick).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdWarn = new CommandDescriptor
        {
            Name = "Warn",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            NotRepeatable = true,
            Permissions = new[] { Permission.Warn },
            Help = "&SWarns a player and puts a black star next to their name for 20 minutes. During the 20 minutes, if they are warned again, they will get kicked.",
            Usage = "/Warn playername",
            Handler = Warn
        };

        internal static void Warn(Player player, Command cmd)
        {
            string name = cmd.Next();

            if (name == null)
            {
                player.Message("No player specified.");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null)
            {
                player.MessageNoPlayer(name);
                return;
            }

            if (player.Can(Permission.Warn, target.Info.Rank))
            {
                target.Info.IsHidden = false;
                if (target.Info.Warn(player.Name))
                {
                    Server.Message("{0}&S has been warned by {1}",
                                      target.ClassyName, player.ClassyName);
                    Scheduler.NewTask(t => target.Info.UnWarn()).RunOnce(TimeSpan.FromMinutes(15));
                }
                else
                {
                    try
                    {
                        Player targetPlayer = target;
                        target.Kick(player, "Auto Kick (2 warnings or more)", LeaveReason.Kick, true, true, true);
                    }
                    catch (PlayerOpException ex)
                    {
                        player.Message(ex.MessageColored);
                        if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                            return;
                    }
                }
            }
            else
            {
                player.Message("You can only warn players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Warn).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }

        static readonly CommandDescriptor CdUnWarn = new CommandDescriptor
        {
            Name = "Unwarn",
            Aliases = new string[] { "uw" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Warn },
            Usage = "/Unwarn PlayerName",
            Help = "&SUnwarns a player",
            Handler = UnWarn
        };

        internal static void UnWarn(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("No player specified.");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);

            if (target == null)
            {
                player.MessageNoPlayer(name);
                return;
            }

            if (player.Can(Permission.Warn, target.Info.Rank))
            {
                if (target.Info.UnWarn())
                {
                    Server.Message("{0}&S had their warning removed by {1}.", target.ClassyName, player.ClassyName);
                }
                else
                {
                    player.Message("{0}&S does not have a warning.", target.ClassyName);
                }
            }
            else
            {
                player.Message("You can only unwarn players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Warn).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }


        static readonly CommandDescriptor CdDisconnect = new CommandDescriptor
        {
            Name = "Disconnect",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Aliases = new[] { "gtfo" },
            IsHidden = false,
            Permissions = new[] { Permission.Gtfo },
            Usage = "/disconnect playername",
            Help = "Get rid of those annoying people without saving to PlayerDB",
            Handler = dc
        };

        internal static void dc(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name == null)
            {
                player.Message("Please enter a name");
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, name, false, true);
            if (target == null) return;

            if (player.Can(Permission.Gtfo, target.Info.Rank))
            {
                try
                {
                    Player targetPlayer = target;
                    target.Kick(player, "Manually disconnected by " + player.Name, LeaveReason.Kick, false, true, false);
                    Server.Players.Message("{0} &Swas manually disconnected by {1}", target.ClassyName, player.ClassyName);
                }
                catch (PlayerOpException ex)
                {
                    player.Message(ex.MessageColored);
                    if (ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired)
                        return;
                }
            }
            else
            {
                player.Message("You can only Disconnect players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit(Permission.Gtfo).ClassyName);
                player.Message("{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName);
            }
        }
        #endregion

        #region Ban / Unban

        static readonly CommandDescriptor CdBan = new CommandDescriptor {
            Name = "Ban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Ban PlayerName [Reason]",
            Help = "&SBans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a ban reason. ",
            Handler = BanHandler
        };

        static void BanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdBan.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                Player targetPlayer = target.PlayerObject;
                target.Ban( player, reason, true, true );
                WarnIfOtherPlayersOnIP( player, target, targetPlayer );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target );
                }
            }
        }



        static readonly CommandDescriptor CdBanIP = new CommandDescriptor {
            Name = "BanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanIP PlayerName|IPAddress [Reason]",
            Help = "&SBans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIPHandler
        };

        static void BanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdBanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                try {
                    targetAddress.BanIP( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            } else {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                if( target == null ) return;
                try {
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Ban( player, reason, true, true );
                    } else {
                        target.BanIP( player, reason, true, true );
                    }
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                    if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                        FreezeIfAllowed( player, target );
                    }
                }
            }
        }

      

        static readonly CommandDescriptor CdUnban = new CommandDescriptor {
            Name = "Unban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Unban PlayerName [Reason]",
            Help = "&SRemoves ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanHandler
        };

        static void UnbanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdUnban.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                target.Unban( player, reason, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanIP = new CommandDescriptor {
            Name = "UnbanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/UnbanIP PlayerName|IPaddress [Reason]",
            Help = "&SRemoves ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIPHandler
        };

        static void UnbanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdUnbanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            try {
                IPAddress targetAddress;
                if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                    targetAddress.UnbanIP( player, reason, true, true );
                } else {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                    if( target == null ) return;
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Unban( player, reason, true, true );
                    } else {
                        target.UnbanIP( player, reason, true, true );
                    }
                }
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanAll = new CommandDescriptor {
            Name = "UnbanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/UnbanAll PlayerName|IPaddress [Reason]",
            Help = "&SRemoves ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAllHandler
        };

        static void UnbanAllHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdUnbanAll.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            try {
                IPAddress targetAddress;
                if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                    targetAddress.UnbanAll( player, reason, true, true );
                } else {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                    if( target == null ) return;
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Unban( player, reason, true, true );
                    } else {
                        target.UnbanAll( player, reason, true, true );
                    }
                }
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdBanEx = new CommandDescriptor {
            Name = "BanEx",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanEx +PlayerName&S or &H/BanEx -PlayerName",
            Help = "&SAdds or removes an IP-ban exemption for an account. " +
                   "Exempt accounts can log in from any IP, including banned ones.",
            Handler = BanExHandler
        };

        static void BanExHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null || playerName.Length < 2 || (playerName[0] != '-' && playerName[0] != '+') ) {
                CdBanEx.PrintUsage( player );
                return;
            }
            bool addExemption = (playerName[0] == '+');
            string targetName = playerName.Substring( 1 );
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;

            switch( target.BanStatus ) {
                case BanStatus.Banned:
                    if( addExemption ) {
                        player.Message( "Player {0}&S is currently banned. Unban before adding an exemption.",
                                        target.ClassyName );
                    } else {
                        player.Message( "Player {0}&S is already banned. There is no exemption to remove.",
                                        target.ClassyName );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if( addExemption ) {
                        player.Message( "IP-Ban exemption already exists for player {0}", target.ClassyName );
                    } else {
                        player.Message( "IP-Ban exemption removed for player {0}",
                                        target.ClassyName );
                        target.BanStatus = BanStatus.NotBanned;
                    }
                    break;
                case BanStatus.NotBanned:
                    if( addExemption ) {
                        player.Message( "IP-Ban exemption added for player {0}",
                                        target.ClassyName );
                        target.BanStatus = BanStatus.IPBanExempt;
                    } else {
                        player.Message( "No IP-Ban exemption exists for player {0}",
                                        target.ClassyName );
                    }
                    break;
            }
        }

        #endregion


        #region Kick

        static readonly CommandDescriptor CdKick = new CommandDescriptor {
            Name = "Kick",
            Aliases = new[] { "k" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/Kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            Handler = KickHandler
        };

        static void KickHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "Usage: &H/Kick PlayerName [Message]" );
                return;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            string reason = cmd.NextAll();
            DateTime previousKickDate = target.Info.LastKickDate;
            string previousKickedBy = target.Info.LastKickByClassy;
            string previousKickReason = target.Info.LastKickReason;

            // do the kick
            try {
                Player targetPlayer = target;
                target.Kick( player, reason, LeaveReason.Kick, true, true, true );
                WarnIfOtherPlayersOnIP( player, target.Info, targetPlayer );

            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target.Info );
                }
                return;
            }

            // warn player if target has been kicked before
            if( target.Info.TimesKicked > 1 ) {
                player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                target.ClassyName, target.Info.TimesKicked - 1 );
                if( previousKickDate != DateTime.MinValue ) {
                    player.Message( "Most recent kick was {0} ago, by {1}",
                                    DateTime.UtcNow.Subtract( previousKickDate ).ToMiniString(),
                                    previousKickedBy );
                }
                if( !String.IsNullOrEmpty( previousKickReason ) ) {
                    player.Message( "Most recent kick reason was: {0}",
                                    previousKickReason );
                }
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor CdRank = new CommandDescriptor {
            Name = "Rank",
            Aliases = new[] { "user", "promote", "demote" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote, Permission.Demote },
            AnyPermission = true,
            IsConsoleSafe = true,
            Usage = "/Rank PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = RankHandler
        };

        public static void RankHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if( name == null || newRankName == null ) {
                CdRank.PrintUsage( player );
                player.Message( "See &H/Ranks&S for list of ranks." );
                return;
            }

            // Parse rank name
            Rank newRank = RankManager.FindRank( newRankName );
            if( newRank == null ) {
                player.MessageNoRank( newRankName );
                return;
            }

            // Parse player name
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return;
                }
            }
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact( name );

            if( targetInfo == null ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.MessageNoPlayer( name );
                    return;
                }
                if( !Player.IsValidName( name ) ) {
                    player.MessageInvalidPlayerName( name );
                    CdRank.PrintUsage( player );
                    return;
                }
                if( cmd.IsConfirmed ) {
                    if( newRank > RankManager.DefaultRank ) {
                        targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                    } else {
                        targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Demoted );
                    }
                } else {
                    player.Confirm( cmd,
                                    "Warning: Player \"{0}\" is not in the database (possible typo). Type the full name or",
                                    name );
                    return;
                }
            }

            try {
                player.LastUsedPlayerName = targetInfo.Name;
                
                //reset temprank values
                if (targetInfo.isTempRanked)
                {
                    targetInfo.isTempRanked = false;
                    targetInfo.tempRankTime = TimeSpan.FromSeconds(0);
                }
                targetInfo.ChangeRank( player, newRank, cmd.NextAll(), true, true, false );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        #endregion


        #region Hide

        static readonly CommandDescriptor CdHide = new CommandDescriptor {
            Name = "Hide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Hide [silent]",
            Help = "&SEnables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/Unhide&S to reveal yourself.",
            Handler = HideHandler
        };

        static void HideHandler( Player player, Command cmd ) {
            if( player.Info.IsHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.Info.IsHidden = true;
            player.Message( "&8You are now hidden." );

            // to make it look like player just logged out in /Info
            player.Info.LastSeen = DateTime.UtcNow;

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Server.Players.CantSee( player ).Message( "&SPlayer {0}&S left the server.", player.ClassyName );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerDisconnectedHandler( null, new PlayerDisconnectedEventArgs( player, LeaveReason.ClientQuit, true ) );
                }
            }

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is now hidden.", player.ClassyName );

            Player.RaisePlayerHideChangedEvent( player );
        }



        static readonly CommandDescriptor CdUnhide = new CommandDescriptor {
            Name = "Unhide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Unhide [silent]",
            Help = "&SDisables the &H/Hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = UnhideHandler
        };

        static void UnhideHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            if( !player.Info.IsHidden ) {
                player.Message( "You are not currently hidden." );
                return;
            }

            bool silent = cmd.HasNext;

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is no longer hidden.",
                                                     player.ClassyName );
            player.Message( "&8You are no longer hidden." );
            player.Info.IsHidden = false;
            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    string msg = Server.MakePlayerConnectedMessage( player, false, player.World );
                    // ReSharper restore AssignNullToNotNullAttribute
                    Server.Players.CantSee( player ).Message( msg );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerReadyHandler( null, new PlayerConnectedEventArgs( player, player.World ) );
                }
            }

            Player.RaisePlayerHideChangedEvent( player );
        }

        #endregion


        #region Set Spawn

        static readonly CommandDescriptor CdSetSpawn = new CommandDescriptor {
            Name = "SetSpawn",
            Category = CommandCategory.Moderation | CommandCategory.World,
            Permissions = new[] { Permission.SetSpawn },
            Help = "&SAssigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/SetSpawn [PlayerName]",
            Handler = SetSpawnHandler
        };

        public static void SetSpawnHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );


            string playerName = cmd.Next();
            if( playerName == null ) {
                Map map = player.WorldMap;
                map.Spawn = player.Position;
                player.TeleportTo( map.Spawn );
                player.Send( PacketWriter.MakeAddEntity( 255, player.ListName, player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( LogType.UserActivity,
                            "{0} changed the spawned point.",
                            player.Name );

            } else if( player.Can( Permission.Bring ) ) {
                Player[] infos = playerWorld.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    player.LastUsedPlayerName = target.Name;
                    if( player.Can( Permission.Bring, target.Info.Rank ) ) {
                        target.Send( PacketWriter.MakeAddEntity( 255, target.ListName, player.Position ) );
                    } else {
                        player.Message( "You may only set spawn of players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                        player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    }

                } else if( infos.Length > 0 ) {
                    player.MessageManyMatches( "player", infos );

                } else {
                    infos = Server.FindPlayers( player, playerName, true );
                    if( infos.Length > 0 ) {
                        player.Message( "You may only set spawn of players on the same world as you." );
                    } else {
                        player.MessageNoPlayer( playerName );
                    }
                }
            } else {
                player.MessageNoAccess( CdSetSpawn );
            }
        }

        #endregion


        #region Freeze

        static readonly CommandDescriptor CdFreeze = new CommandDescriptor {
            Name = "Freeze",
            Aliases = new[] { "f" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = FreezeHandler
        };

        public static void FreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            try {
                target.Info.Freeze( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdUnfreeze = new CommandDescriptor {
            Name = "Unfreeze",
            Aliases = new[] { "uf" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/Help Freeze&S for more information.",
            Handler = UnfreezeHandler
        };

        static void UnfreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            try {
                target.Info.Unfreeze( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }

        #endregion


        #region TP

        static readonly CommandDescriptor CdTP = new CommandDescriptor {
            Name = "TP",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/TP PlayerName&S or &H/TP X Y Z",
            Help = "&STeleports you to a specified player's location. " +
                   "If coordinates are given, teleports to that location.",
            Handler = TPHandler
        };

        static void TPHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdTP.PrintUsage( player );
                return;
            }

            if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, z;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || z <= -1024 || z >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.previousLocation = player.Position;
                        player.previousWorld = null;
                        player.TeleportTo( new Position {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            Z = (short)(z * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        } );
                    }
                } else {
                    CdTP.PrintUsage( player );
                }

            } else {
                if( name == "-" ) {
                    if( player.LastUsedPlayerName != null ) {
                        name = player.LastUsedPlayerName;
                    } else {
                        player.Message( "Cannot repeat player name: you haven't used any names yet." );
                        return;
                    }
                }
                Player[] matches = Server.FindPlayers( player, name, true );
                if( matches.Length == 1 ) {
                    Player target = matches[0];
                    World targetWorld = target.World;
                    if( targetWorld == null ) PlayerOpException.ThrowNoWorld( target );

                    if( targetWorld == player.World ) {
                        player.previousLocation = player.Position;
                        player.previousWorld = null;
                        player.TeleportTo( target.Position );

                    } else {
                        switch( targetWorld.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if( targetWorld.IsFull ) {
                                    player.Message( "Cannot teleport to {0}&S because world {1}&S is full.",
                                                    target.ClassyName,
                                                    targetWorld.ClassyName );
                                    return;
                                }
                                player.StopSpectating();
                                player.previousLocation = player.Position;
                                player.previousWorld = player.World;
                                player.JoinWorld( targetWorld, WorldChangeReason.Tp, target.Position );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.ClassyName,
                                                targetWorld.ClassyName );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.ClassyName,
                                                targetWorld.ClassyName,
                                                targetWorld.AccessSecurity.MinRank.ClassyName );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.MessageManyMatches( "player", matches );

                } else {
                    // Try to guess if player typed "/TP" instead of "/Join"
                    World[] worlds = WorldManager.FindWorlds( player, name );

                    if( worlds.Length == 1 ) {
                        player.LastUsedWorldName = worlds[0].Name;
                        player.StopSpectating();
                        player.ParseMessage( "/Join " + worlds[0].Name, false, true );
                    } else {
                        player.MessageNoPlayer( name );
                    }
                }
            }
        }

        #endregion


        #region Bring / WorldBring / BringAll

        static readonly CommandDescriptor CdBring = new CommandDescriptor {
            Name = "Bring",
            IsConsoleSafe = true,
            Aliases = new[] { "summon", "fetch" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/Bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = BringHandler
        };

        static void BringHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches( player, toName, false, true );
                if( toPlayer == null ) return;
            } else if( toPlayer.World == null ) {
                player.Message( "When used from console, /Bring requires both names to be given." );
                return;
            }

            World world = toPlayer.World;
            if( world == null ) PlayerOpException.ThrowNoWorld( toPlayer );

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( target.World == world ) {
                // teleport within the same world
                target.previousLocation = target.Position;
                target.previousWorld = null;
                target.TeleportTo( toPlayer.Position );
                target.Message("&8You were summoned by {0}", player.ClassyName);

            } else {
                // teleport to a different world
                SecurityCheckResult check = world.AccessSecurity.CheckDetailed( target.Info );
                if( check == SecurityCheckResult.RankTooHigh || check == SecurityCheckResult.RankTooLow ) {
                    if( player.CanJoin( world ) ) {
                        if( cmd.IsConfirmed ) {
                            BringPlayerToWorld( player, target, world, true, true );
                        } else {
                            player.Confirm( cmd,
                                            "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                            target.ClassyName,
                                            world.ClassyName );
                        }
                    } else {
                        player.Message( "Neither you nor {0}&S are allowed to join world {1}",
                                        target.ClassyName, world.ClassyName );
                    }
                } else {
                    BringPlayerToWorld( player, target, world, false, true );
                    target.Message("&8You were summoned by {0}", player.ClassyName);
                }
            }
        }


        static readonly CommandDescriptor CdWorldBring = new CommandDescriptor {
            Name = "WBring",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/WBring PlayerName WorldName",
            Help = "&STeleports a player to the given world's spawn.",
            Handler = WorldBringHandler
        };

        static void WorldBringHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            string worldName = cmd.Next();
            if( playerName == null || worldName == null ) {
                CdWorldBring.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, playerName, false, true );
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );

            if( target == null || world == null ) return;

            if( target == player ) {
                player.Message( "&WYou cannot &H/WBring&W yourself." );
                return;
            }

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( world == target.World ) {
                player.Message( "Player {0}&S is already in world {1}&S. They were brought to spawn.",
                                target.ClassyName, world.ClassyName );
                target.TeleportTo( target.WorldMap.Spawn );
                return;
            }

            SecurityCheckResult check = world.AccessSecurity.CheckDetailed( target.Info );
            if( check == SecurityCheckResult.RankTooHigh || check == SecurityCheckResult.RankTooLow ) {
                if( player.CanJoin( world ) ) {
                    if( cmd.IsConfirmed ) {
                        BringPlayerToWorld( player, target, world, true, false );
                    } else {
                        player.Confirm( cmd,
                                        "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                        target.ClassyName,
                                        world.ClassyName );
                    }
                } else {
                    player.Message( "Neither you nor {0}&S are allowed to join world {1}",
                                    target.ClassyName, world.ClassyName );
                }
            } else {
                BringPlayerToWorld( player, target, world, false, false );
            }
        }


        static readonly CommandDescriptor CdBringAll = new CommandDescriptor {
            Name = "BringAll",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring, Permission.BringAll },
            Usage = "/BringAll [@Rank [@AnotherRank]] [*|World [AnotherWorld]]",
            Help = "&STeleports all players from your world to you. " +
                   "If any world names are given, only teleports players from those worlds. " +
                   "If any rank names are given, only teleports players of those ranks.",
            Handler = BringAllHandler
        };

        static void BringAllHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            List<World> targetWorlds = new List<World>();
            List<Rank> targetRanks = new List<Rank>();
            bool allWorlds = false;
            bool allRanks = true;

            // Parse the list of worlds and ranks
            string arg;
            while( (arg = cmd.Next()) != null ) {
                if( arg.StartsWith( "@" ) ) {
                    Rank rank = RankManager.FindRank( arg.Substring( 1 ) );
                    if( rank == null ) {
                        player.Message( "Unknown rank: {0}", arg.Substring( 1 ) );
                        return;
                    } else {
                        if( player.Can( Permission.Bring, rank ) ) {
                            targetRanks.Add( rank );
                        } else {
                            player.Message( "&WYou are not allowed to bring players of rank {0}",
                                            rank.ClassyName );
                        }
                        allRanks = false;
                    }
                } else if( arg == "*" ) {
                    allWorlds = true;
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, arg );
                    if( world == null ) return;
                    targetWorlds.Add( world );
                }
            }

            // If no worlds were specified, use player's current world
            if( !allWorlds && targetWorlds.Count == 0 ) {
                targetWorlds.Add( player.World );
            }

            // Apply all the rank and world options
            HashSet<Player> targetPlayers;
            if( allRanks && allWorlds ) {
                targetPlayers = new HashSet<Player>( Server.Players );
            } else if( allWorlds ) {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( Player rankPlayer in Server.Players.Ranked( rank ) ) {
                        targetPlayers.Add( rankPlayer );
                    }
                }
            } else if( allRanks ) {
                targetPlayers = new HashSet<Player>();
                foreach( World world in targetWorlds ) {
                    foreach( Player worldPlayer in world.Players ) {
                        targetPlayers.Add( worldPlayer );
                    }
                }
            } else {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( World world in targetWorlds ) {
                        foreach( Player rankWorldPlayer in world.Players.Ranked( rank ) ) {
                            targetPlayers.Add( rankWorldPlayer );
                        }
                    }
                }
            }

            Rank bringLimit = player.Info.Rank.GetLimit( Permission.Bring );

            // Remove the player him/herself
            targetPlayers.Remove( player );

            int count = 0;


            // Actually bring all the players
            foreach( Player targetPlayer in targetPlayers.CanBeSeen( player )
                                                         .RankedAtMost( bringLimit ) ) {
                if( targetPlayer.World == player.World ) {
                    // teleport within the same world
                    targetPlayer.TeleportTo( player.Position );
                    targetPlayer.Position = player.Position;
                    if( targetPlayer.Info.IsFrozen ) {
                        targetPlayer.Position = player.Position;
                    }

                } else {
                    // teleport to a different world
                    BringPlayerToWorld( player, targetPlayer, player.World, false, true );
                }
                count++;
            }

            // Check if there's anyone to bring
            if( count == 0 ) {
                player.Message( "No players to bring!" );
            } else {
                player.Message( "Bringing {0} players...", count );
            }
        }



        static void BringPlayerToWorld( [NotNull] Player player, [NotNull] Player target, [NotNull] World world,
                                        bool overridePermissions, bool usePlayerPosition ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( world == null ) throw new ArgumentNullException( "world" );
            switch( world.AccessSecurity.CheckDetailed( target.Info ) ) {
                case SecurityCheckResult.Allowed:
                case SecurityCheckResult.WhiteListed:
                    if( world.IsFull ) {
                        player.Message( "Cannot bring {0}&S because world {1}&S is full.",
                                        target.ClassyName,
                                        world.ClassyName );
                        return;
                    }
                    target.StopSpectating();
                    if( usePlayerPosition ) {
                        target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                    } else {
                        target.JoinWorld( world, WorldChangeReason.Bring );
                    }
                    break;

                case SecurityCheckResult.BlackListed:
                    player.Message( "Cannot bring {0}&S because he/she is blacklisted on world {1}",
                                    target.ClassyName,
                                    world.ClassyName );
                    break;

                case SecurityCheckResult.RankTooLow:
                    if( overridePermissions ) {
                        target.StopSpectating();
                        if( usePlayerPosition ) {
                            target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                        } else {
                            target.JoinWorld( world, WorldChangeReason.Bring );
                        }
                    } else {
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                        target.ClassyName,
                                        world.ClassyName,
                                        world.AccessSecurity.MinRank.ClassyName );
                    }
                    break;
                // TODO: case PermissionType.RankTooHigh:
            }
        }

        #endregion


        #region Patrol & SpecPatrol

        static readonly CommandDescriptor CdPatrol = new CommandDescriptor {
            Name = "Patrol",
            Aliases = new[] { "pat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = PatrolHandler
        };

        static void PatrolHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = playerWorld.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            player.TeleportTo( target.Position );
            player.Message( "Patrol: Teleporting to {0}", target.ClassyName );
        }


        static readonly CommandDescriptor CdSpecPatrol = new CommandDescriptor {
            Name = "SpecPatrol",
            Aliases = new[] { "spat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol, Permission.Spectate },
            Help = "Teleports you to the next player in need of checking.",
            Handler = SpecPatrolHandler
        };

        static void SpecPatrolHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = playerWorld.GetNextPatrolTarget( player,
                                                             p => player.Can( Permission.Spectate, p.Info.Rank ),
                                                             true );
            if( target == null ) {
                player.Message( "Patrol: No one to spec-patrol in this world." );
                return;
            }

            target.LastPatrolTime = DateTime.UtcNow;
            player.Spectate( target );
        }

        #endregion


        #region Mute / Unmute

        static readonly TimeSpan MaxMuteDuration = TimeSpan.FromDays( 700 ); // 100w0d

        static readonly CommandDescriptor CdMute = new CommandDescriptor {
            Name = "Mute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "&SMutes a player for a specified length of time.",
            Usage = "/Mute PlayerName Duration",
            Handler = MuteHandler
        };

        static void MuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;

            // validate command parameters
            if( String.IsNullOrEmpty( targetName ) || String.IsNullOrEmpty( timeString ) ||
                !timeString.TryParseMiniTimespan( out duration ) || duration <= TimeSpan.Zero ) {
                CdMute.PrintUsage( player );
                return;
            }

            // check if given time exceeds maximum (700 days)
            if( duration > MaxMuteDuration ) {
                player.Message( "Maximum mute duration is {0}.", MaxMuteDuration.ToMiniString() );
                duration = MaxMuteDuration;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            // actually mute
            try {
                target.Info.Mute( player, duration, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdUnmute = new CommandDescriptor {
            Name = "Unmute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "&SUnmutes a player.",
            Usage = "/Unmute PlayerName",
            Handler = UnmuteHandler
        };

        static void UnmuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( String.IsNullOrEmpty( targetName ) ) {
                CdUnmute.PrintUsage( player );
                return;
            }

            // find target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            try {
                target.Info.Unmute( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }

        #endregion


        #region Spectate / Unspectate

        static readonly CommandDescriptor CdSpectate = new CommandDescriptor {
            Name = "Spectate",
            Aliases = new[] { "follow", "spec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Usage = "/Spectate PlayerName",
            Handler = SpectateHandler
        };

        static void SpectateHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                PlayerInfo lastSpec = player.LastSpectatedPlayer;
                if( lastSpec != null ) {
                    Player spec = player.SpectatedPlayer;
                    if( spec != null ) 
                    {
                        if (spec.World.Name != player.World.Name)
                        {
                            player.JoinWorld(spec.World, WorldChangeReason.SpectateTargetJoined);
                            player.Message("Joined " + spec.World.Name + " to continue spectating " + spec.ClassyName);
                        }
                        player.Message( "Now spectating {0}", spec.ClassyName );
                    } 
                    else 
                    {
                        player.Message( "Last spectated {0}", lastSpec.ClassyName );
                    }
                } else {
                    CdSpectate.PrintUsage( player );
                }
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot spectate yourself." );
                return;
            }

            if( !player.Can( Permission.Spectate, target.Info.Rank ) ) {
                player.Message( "You may only spectate players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Spectate ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( !player.Spectate( target ) ) {
                player.Message( "Already spectating {0}", target.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnspectate = new CommandDescriptor {
            Name = "Unspectate",
            Aliases = new[] { "unfollow", "unspec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            NotRepeatable = true,
            Handler = UnspectateHandler
        };

        static void UnspectateHandler( Player player, Command cmd ) {
            if( !player.StopSpectating() ) {
                player.Message( "You are not currently spectating anyone." );
            }
        }

        #endregion


        // freeze target if player is allowed to do so
        static void FreezeIfAllowed( Player player, PlayerInfo targetInfo ) {
            if( targetInfo.IsOnline && !targetInfo.IsFrozen && player.Can( Permission.Freeze, targetInfo.Rank ) ) {
                try {
                    targetInfo.Freeze( player, true, true );
                    player.Message( "Player {0}&S has been frozen while you retry.", targetInfo.ClassyName );
                } catch( PlayerOpException ) { }
            }
        }


        // warn player if others are still online from target's IP
        static void WarnIfOtherPlayersOnIP( Player player, PlayerInfo targetInfo, Player except ) {
            Player[] otherPlayers = Server.Players.FromIP( targetInfo.LastIP )
                                                  .Except( except )
                                                  .ToArray();
            if( otherPlayers.Length > 0 ) {
                player.Message( "&WWarning: Other player(s) share IP with {0}&W: {1}",
                                targetInfo.ClassyName,
                                otherPlayers.JoinToClassyString() );
            }
        }
    }
}

﻿// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using fCraft.Drawing;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace fCraft {
    /// <summary> Commands for placing specific blocks (solid, water, grass),
    /// and switching block placement modes (paint, bind). </summary>
    static class BuildingCommands {

        public static int MaxUndoCount = 2000000;

        const string GeneralDrawingHelp = " Use &H/Cancel&S to cancel selection mode. " +
                                          "Use &H/Undo&S to stop and undo the last command.";

        internal static void Init() {
            CommandManager.RegisterCommand( CdBind );
            CommandManager.RegisterCommand( CdGrass );
            CommandManager.RegisterCommand( CdLava );
            CommandManager.RegisterCommand( CdPaint );
            CommandManager.RegisterCommand( CdSolid );
            CommandManager.RegisterCommand( CdWater );


            CommandManager.RegisterCommand( CdCancel );
            CommandManager.RegisterCommand( CdMark );
            CommandManager.RegisterCommand( CdUndo );
            CommandManager.RegisterCommand( CdRedo );

            CommandManager.RegisterCommand( CdReplace );
            CommandManager.RegisterCommand( CdReplaceNot );
            CommandManager.RegisterCommand( CdReplaceBrush );
            CdReplace.Help += GeneralDrawingHelp;
            CdReplaceNot.Help += GeneralDrawingHelp;
            CdReplaceBrush.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdCopySlot );
            CommandManager.RegisterCommand( CdCopy );
            CommandManager.RegisterCommand( CdCut );
            CommandManager.RegisterCommand( CdPaste );
            CommandManager.RegisterCommand( CdPasteNot );
            CommandManager.RegisterCommand( CdPasteX );
            CommandManager.RegisterCommand( CdPasteNotX );
            CommandManager.RegisterCommand( CdMirror );
            CommandManager.RegisterCommand( CdRotate );
            CdCut.Help += GeneralDrawingHelp;
            CdPaste.Help += GeneralDrawingHelp;
            CdPasteNot.Help += GeneralDrawingHelp;
            CdPasteX.Help += GeneralDrawingHelp;
            CdPasteNotX.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdRestore );
            CdRestore.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdCuboid );
            CommandManager.RegisterCommand( CdCuboidWireframe );
            CommandManager.RegisterCommand( CdCuboidHollow );
            CdCuboid.Help += GeneralDrawingHelp;
            CdCuboidHollow.Help += GeneralDrawingHelp;
            CdCuboidWireframe.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdEllipsoid );
            CommandManager.RegisterCommand( CdEllipsoidHollow );
            CdEllipsoid.Help += GeneralDrawingHelp;
            CdEllipsoidHollow.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdLine );
            CommandManager.RegisterCommand( CdTriangle );
            CommandManager.RegisterCommand( CdTriangleWireframe );
            CdLine.Help += GeneralDrawingHelp;
            CdTriangle.Help += GeneralDrawingHelp;
            CdTriangleWireframe.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdSphere );
            CommandManager.RegisterCommand( CdSphereHollow );
            CommandManager.RegisterCommand( CdTorus );
            CdSphere.Help += GeneralDrawingHelp;
            CdSphereHollow.Help += GeneralDrawingHelp;
            CdTorus.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdFill2D );
            CdFill2D.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdUndoArea );
            CommandManager.RegisterCommand(CdUndoAreaNot);
            CommandManager.RegisterCommand( CdUndoPlayer );
            CommandManager.RegisterCommand(CdUndoPlayerNot);
            CdUndoArea.Help += GeneralDrawingHelp;
            CommandManager.RegisterCommand( CdStatic );

            CommandManager.RegisterCommand(CdTree);
            CommandManager.RegisterCommand(CdWalls);
            CommandManager.RegisterCommand(CdBanx);
            CommandManager.RegisterCommand(CdFly);
            CommandManager.RegisterCommand(CdPlace);
            CommandManager.RegisterCommand(CdTower);
            CommandManager.RegisterCommand(CdCylinder);
            CommandManager.RegisterCommand(CdCenter);

            CommandManager.RegisterCommand(CdUndoAll);
            //CommandManager.RegisterCommand(CdMessageBlock);
            //CommandManager.RegisterCommand(CdBanX2);
            CommandManager.RegisterCommand(CdDoubleStair);
            CommandManager.RegisterCommand(CdAbortAll);
            CommandManager.RegisterCommand(Cdzz);
            CommandManager.RegisterCommand(CdHighlight);


        }

        sealed class BlockDBUndoArgs
        {
            public Player Player;
            public PlayerInfo[] Targets;
            public World World;
            public int CountLimit;
            public TimeSpan AgeLimit;
            public BlockDBEntry[] Entries;
            public BoundingBox Area;
            public bool Not;
        }

        // parses and checks command parameters (for both UndoPlayer and UndoArea)
        [CanBeNull]
        static BlockDBUndoArgs ParseBlockDBUndoParams(Player player, Command cmd, string cmdName, bool not)
        {
            // check if command's being called by a worldless player (e.g. console)
            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);

            // ensure that BlockDB is enabled
            if (!BlockDB.IsEnabledGlobally)
            {
                player.Message("&W{0}: BlockDB is disabled on this server.", cmdName);
                return null;
            }
            if (!playerWorld.BlockDB.IsEnabled)
            {
                player.Message("&W{0}: BlockDB is disabled in this world.", cmdName);
                return null;
            }

            // parse the first parameter - either numeric or time limit
            string range = cmd.Next();
            if (range == null)
            {
                CdUndoPlayer.PrintUsage(player);
                return null;
            }
            int countLimit;
            TimeSpan ageLimit = TimeSpan.Zero;
            if (!Int32.TryParse(range, out countLimit) && !range.TryParseMiniTimespan(out ageLimit))
            {
                player.Message("{0}: First parameter should be a number or a timespan.", cmdName);
                return null;
            }
            if (ageLimit > DateTimeUtil.MaxTimeSpan)
            {
                player.MessageMaxTimeSpan();
                return null;
            }

            // parse second and consequent parameters (player names)
            HashSet<PlayerInfo> targets = new HashSet<PlayerInfo>();
            bool allPlayers = false;
            while (true)
            {
                string name = cmd.Next();
                if (name == null)
                {
                    break;
                }
                else if (name == "*")
                {
                    // all players
                    if (not)
                    {
                        player.Message("{0}: \"*\" not allowed (cannot undo \"everyone except everyone\")", cmdName);
                        return null;
                    }
                    if (allPlayers)
                    {
                        player.Message("{0}: \"*\" was listed twice.", cmdName);
                        return null;
                    }
                    allPlayers = true;

                }
                else
                {
                    // individual player
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, name);
                    if (target == null)
                    {
                        return null;
                    }
                    if (targets.Contains(target))
                    {
                        player.Message("{0}: Player {1}&S was listed twice.",
                                        target.ClassyName, cmdName);
                        return null;
                    }
                    // make sure player has the permission
                    if (!not &&
                        player.Info != target && !player.Can(Permission.UndoAll) &&
                        !player.Can(Permission.UndoOthersActions, target.Rank))
                    {
                        player.Message("&W{0}: You may only undo actions of players ranked {1}&S or lower.",
                                        cmdName,
                                        player.Info.Rank.GetLimit(Permission.UndoOthersActions).ClassyName);
                        player.Message("Player {0}&S is ranked {1}",
                                        target.ClassyName, target.Rank.ClassyName);
                        return null;
                    }
                    targets.Add(target);
                }
            }
            if (targets.Count == 0 && !allPlayers)
            {
                player.Message("{0}: Specify at least one player name, or \"*\" to undo everyone.", cmdName);
                return null;
            }
            if (targets.Count > 0 && allPlayers)
            {
                player.Message("{0}: Cannot mix player names and \"*\".", cmdName);
                return null;
            }

            // undoing everyone ('*' in place of player name) requires UndoAll permission
            if ((not || allPlayers) && !player.Can(Permission.UndoAll))
            {
                player.MessageNoAccess(Permission.UndoAll);
                return null;
            }

            // Queue UndoPlayerCallback to run
            return new BlockDBUndoArgs
            {
                Player = player,
                AgeLimit = ageLimit,
                CountLimit = countLimit,
                Area = player.WorldMap.Bounds,
                World = playerWorld,
                Targets = targets.ToArray(),
                Not = not
            };
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
        
        static readonly CommandDescriptor CdHighlight = new CommandDescriptor 
        {
            Name = "Highlight",
            Aliases = new[] { "SelectionCuboid", "Select", "hl" },
            IsConsoleSafe = false,
            Permissions = new[] { Permission.DrawAdvanced },
            Category = CommandCategory.Building,
            Help = "Highlights a specific location of the world. Use /help Highlight [option] for more options",
            Usage = "/Highlight [Create | Remove | List | Clear]",
            HelpSections = new System.Collections.Generic.Dictionary<string, string>{
                { "create", "&H/Highlight create [highlight name] [color or #htmlcolor] [Percent Opaque]\n&S" +
                                "Creates a new highlighted zone with the given name, color (or html color), and percent opacity." },
                { "remove", "&H/Highlight remove [highlight name]\n&S" +
                                "Removes the given highlighted zone." },
                { "list", "&H/Highlight list [world name]\n&S" +
                                "Lists all highlighted zones in a world. Use 'all' for [world name] to list all highlights."},  
                { "clear", "&H/Highlight clear [world name]\n&S" +
                                "Removes all highlight from a given world. Use 'all' for [world name] to remove all highlights."}  
            },
            Handler = HighlightHandler
        };

        static void HighlightHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            if (String.IsNullOrEmpty(option))
            {
                CdHighlight.PrintUsage(player);
                return;
            }

            switch (option.ToLower())
            {
                case "create":
                case "new":
                case "add":
                    string name = cmd.Next();
                    if (String.IsNullOrEmpty(name))
                    {
                        player.Message("Usage is /Highlight create [highlight name]");
                        return;
                    }

                    if (Server.Highlights.Keys.Contains(name))
                    {
                        player.Message("A highlight with that name already exists!");
                        return;
                    }
                    System.Drawing.Color systemColor;

                    string color = cmd.Next();
                    if (String.IsNullOrEmpty(color))
                    {
                        player.Message("Usage is /Highlight create [highlight name] [color or #htmlcolor] [Percent Opaque]");
                        return;
                    }

                    if (color.Contains('#'))
                    {
                        try
                        {
                            systemColor = System.Drawing.ColorTranslator.FromHtml(color);
                        }
                        catch
                        {
                            player.Message("Unrecognized color! Please use a valid color name or html color code.");
                            return;
                        }
                    }
                    else
                    {
                        try
                        {
                            systemColor = System.Drawing.Color.FromName(color);
                        }
                        catch
                        {
                            player.Message("Unrecognized color! Please use a valid color name or html color code.");
                            return;
                        }
                    }

                    string opacity = cmd.Next();
                    int percentOpacity;
                    if (String.IsNullOrEmpty(opacity))
                    {
                        player.Message("Usage is /Highlight create [highlight name] [color or #htmlcolor] [Percent Opaque]");
                        return;
                    }

                    if (!Int32.TryParse(opacity, out percentOpacity))
                    {
                        player.Message("Percent opacity must be from 0-100, where 0 would be transparent, and 100 would be completely opaque.");
                        return;
                    }

                    player.highlightName = name;
                    player.cmd = cmd.Name;
                    player.color = systemColor;
                    player.percentOpacity = percentOpacity;
                    player.SelectMarks();
                    break;
                case "remove":
                case "delete":
                    string targetHighlight = cmd.Next();
                    if (String.IsNullOrEmpty(targetHighlight))
                    {
                        CdHighlight.PrintUsage(player);
                            return;
                    }
                    if(!Server.Highlights.Keys.Contains(targetHighlight))
                    {
                        player.Message("That highlight zone was not found! Please make sure you spelled it correctly!");
                        return;
                    }

                    //get the value of the highlight key, item one of the value tuple is the ID
                    Tuple<int, Vector3I, Vector3I, System.Drawing.Color, int> val;
                    Server.Highlights.TryGetValue(targetHighlight, out val);

                    player.World.Players.Send(PacketWriter.RemoveSelectionCuboid((byte)val.Item1));
                    Server.Highlights.Remove(targetHighlight);

                    break;
                case "list":
                case "show":
                    string worldName = cmd.Next();
                    if (String.IsNullOrEmpty(worldName))
                    {
                        CdHighlight.PrintUsage(player);
                        return;
                    }

                    if (worldName == "all")
                    {
                        player.Message("_Listing all the highlights on {0}_", ConfigKey.ServerName.GetString());
                        foreach (string s in Server.Highlights.Keys)
                        {
                            player.Message(s);
                        }
                    }
                    else
                    {
                        World targetWorld = WorldManager.FindWorldOrPrintMatches(player, worldName);
                        if (targetWorld == null)
                        {
                            return;
                        }

                        player.Message("_Listing all highlights on {0}_", targetWorld.Name);
                        foreach (string s in targetWorld.Highlights.Keys)
                        {
                            player.Message(s);
                        }
                    }
                    break;
                case "clear":
                case "empty":
                    string worldTarget = cmd.Next();
                    if (String.IsNullOrEmpty(worldTarget))
                    {
                        CdHighlight.PrintUsage(player);
                        return;
                    }

                    if (worldTarget == "all")
                    {
                        player.Message("_Removing all highlights {0}_", ConfigKey.ServerName.GetString());
                        foreach (Player p in Server.Players.Where(p => p.usesCPE))
                        {
                            //physically remove all highlights where players are connected to specific worlds
                            if (p.World.Highlights.Count > 0)
                            {
                                foreach (var i in p.World.Highlights.Values)
                                {
                                    p.Send(PacketWriter.RemoveSelectionCuboid((byte)i.Item1));
                                }
                            }
                        }

                        //remove all traces of highlights on each world
                        var worldWithHighlights = from w in WorldManager.Worlds
                                                  where w.Highlights.Count > 0
                                                  select w;

                        foreach (World world in worldWithHighlights)
                        {
                            world.Highlights.Clear();
                        }

                        //remove all traces of highlights on the server
                        Server.Highlights.Clear();
                    }
                    else
                    {
                        World targetWorld = WorldManager.FindWorldOrPrintMatches(player, worldTarget);
                        if (targetWorld == null)
                        {
                            return;
                        }

                        player.Message("_Removing all highlights on {0}_", targetWorld.Name);
                        foreach (Player p in targetWorld.Players.Where(p => p.usesCPE))
                        {
                            foreach (var i in targetWorld.Highlights.Values)
                            {
                                p.Send(PacketWriter.RemoveSelectionCuboid((byte)i.Item1));
                            }
                        }

                        targetWorld.Highlights.Clear();
                    }
                    break;
                default:
                    CdHighlight.PrintUsage(player);
                    break;
            }

        }

        static readonly CommandDescriptor Cdzz = new CommandDescriptor
        {
            Name = "zz",
            Aliases = new[] { "staticz", "cc" },
            Permissions = new[] { Permission.Draw },
            Category = CommandCategory.Building,
            Help = "Allows you to use cuboid subsequently.",
            Handler = zzHandler
        };

        static void zzHandler(Player player, Command cmd)
        {
            bool zz = false;
            if (zz == false)
            {
                StaticHandler(player, new Command("/static"));
                CuboidHandler(player, new Command("/cuboid"));
                zz = true;
            }
            else
            {
                StaticHandler(player, new Command("/static"));
                zz = false;
            }
        }
        
         static readonly CommandDescriptor CdDoubleStair = new CommandDescriptor //a copy and paste of /lava
        {
            Name = "DoubleStair",
            Aliases = new[] { "ds" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceGrass },
            Help = "Toggles the DoubleStair placement mode. When enabled, any Stair block you place is replaced with DoubleStair.",
            Handler = DoubleStairHandler
        };

        static void DoubleStairHandler(Player player, Command cmd)
        {
            if (player.GetBind(Block.Stair) == Block.DoubleStair)
            {
                player.ResetBind(Block.Stair);
                player.Message("DoubleStair: OFF");
            }

            else
            {
                player.Bind(Block.Stair, Block.DoubleStair);
                player.Message("DoubleStair: ON. Stair blocks are replaced with DoubleStairs.");
            }
        }
        
        static readonly CommandDescriptor CdMessageBlock = new CommandDescriptor
        {
            Name = "MessageBlock",
            Aliases = new[] { "MsgBlock" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            IsHidden = false,
            Permissions = new[] { Permission.Ban },
            Usage = "/MessageBlock Message",
            Help = "Creates a message that is sent to the player whenever the touch the messageblock.",
            Handler = MsgBlockHandler
        };

        public static void MsgBlockHandler(Player player, Command cmd)
        {
            String message = cmd.NextAll();
            if (message == null) 
            {
                CdMessageBlock.PrintUsage(player);
            }
             Vector3I MessageBlock = new Vector3I(player.Position.X / 32, player.Position.Y / 32, (player.Position.Z / 32) - 2);
        }

        static readonly CommandDescriptor CdUndoAll = new CommandDescriptor
        {
            Name = "UndoAll",
            Aliases = new[] { "Revert" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            IsHidden = false,
            Permissions = new[] { Permission.Ban },
            Usage = "/UndoAll playername",
            Help = "Completely undo's a player's blocks.",
            Handler = UndoAllHandler
        };

        public static void UndoAllHandler(Player player, Command cmd) {
            // check if command's being called by a worldless player (e.g. console)
            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);

            // ensure that BlockDB is enabled
            if (!BlockDB.IsEnabledGlobally) {
                player.Message("&WUndoAll: BlockDB is disabled on this server."); return;
            }
            if (!playerWorld.BlockDB.IsEnabled) {
                player.Message("&WUndoAll: BlockDB is disabled in this world."); return;
            }  
            
            string name = cmd.Next();
            if (name == null) {
                CdUndoAll.PrintUsage(player); return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, name);
            if (target == null) {
                player.MessageNoPlayer(name); return;
            }
            if( !player.Can(Permission.UndoOthersActions, target.Rank)) {
                player.Message("&WUndoAll: You may only undo actions of players ranked {0}&S or lower.",
                               player.Info.Rank.GetLimit(Permission.UndoOthersActions).ClassyName);
                player.Message("Player {0}&S is ranked {1}", target.ClassyName, target.Rank.ClassyName);
                return;
            }
            
            BlockDBUndoArgs args = new BlockDBUndoArgs {
                Player = player,
                CountLimit = 100000,
                Area = player.WorldMap.Bounds,
                World = playerWorld,
                Targets = new PlayerInfo[] { target },
            }; 
            Scheduler.NewBackgroundTask(UndoAllLookup)
                     .RunOnce(args, TimeSpan.Zero);
        }
        
        static void UndoAllLookup(SchedulerTask task) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            const string cmdName = "UndoPlayer";
            Vector3I[] coords = new Vector3I[0];
            args.Entries = args.World.BlockDB.Lookup(args.CountLimit, args.Targets, args.Not);

            // Produce a brief param description for BlockDBDrawOperation
            string description = String.Format("{0} by {1}", args.CountLimit,
                                                 args.Targets.JoinToString(p => p.Name));

            // start undoing (using DrawOperation infrastructure)
            var op = new BlockDBDrawOperation(args.Player, cmdName, description, coords.Length);
            op.Prepare(coords, args.Entries);

            // log operation
            string targetList = targetList = args.Targets.JoinToClassyString();
            Logger.Log(LogType.UserActivity,
                        "{0}: Player {1} will undo {2} changes (limit of {3}) by {4} on world {5}",
                        cmdName, args.Player.Name, args.Entries.Length,
                        args.CountLimit.ToString(NumberFormatInfo.InvariantInfo),
                        targetList, args.World.Name);
            op.Begin();
        }
        
        static readonly CommandDescriptor CdAbortAll = new CommandDescriptor 
    	{
		Name = "AbortAll",
		Aliases = new[] { "abort", "aba" },
		Category = CommandCategory.Building,
		Help = "Sets all toggles to false (paint, static, etc)",
		Handler = AbortAllHandler
	};

	static void AbortAllHandler( Player player, Command cmd ) 
	{
		player.IsPainting = false;			// /paint
		player.ResetAllBinds();				// /bind
		player.ParseMessage( "/brush normal", false, false );  // /brush (totally not a sneaky way to do this)
		player.BuildingPortal = false;			// /portal
		player.fireworkMode = false;			// /fireworks
		player.GunMode = false;				// /gun
		player.IsRepeatingSelection = false;		// /static
		player.SelectionCancel();			// /cancel
		player.StopSpectating();			// /spectate
		player.towerMode = false;			// /tower
		player.ParseMessage( "/nvm", false, false );		// /nvm
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


        static readonly CommandDescriptor CdTree = new CommandDescriptor
        {
            Name = "Tree",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Tree },
            Usage = "/Tree Shape Height",
            Help = "&SPlants a tree of given shape and height. Available shapes: Normal, Bamboo, Palm, Cone, Round, Rainforest, Mangrove",
            Handler = TreeHandler
        };

        static void TreeHandler(Player player, Command cmd)
        {
            string shapeName = cmd.Next();
            int height;
            Forester.TreeShape shape;

            // that's one ugly if statement... does the job though.
            if (shapeName == null ||
                !cmd.NextInt(out height) ||
                !EnumUtil.TryParse(shapeName, out shape, true) ||
                shape == Forester.TreeShape.Stickly ||
                shape == Forester.TreeShape.Procedural)
            {
                CdTree.PrintUsage(player);
                player.Message("Available shapes: Normal, Bamboo, Palm, Cone, Round, Rainforest, Mangrove.");
                return;
            }

            if (height < 6 || height > 1024)
            {
                player.Message("Tree height must be 6 blocks or above");
                return;
            }

            Map map = player.World.Map;

            ForesterArgs args = new ForesterArgs
            {
                Height = height - 1,
                Operation = Forester.ForesterOperation.Add,
                Map = map,
                Shape = shape,
                TreeCount = 1,
                RootButtresses = false,
                Roots = Forester.RootMode.None,
                Rand = new Random()
            };
            player.SelectionStart(1, TreeCallback, args, CdTree.Permissions);
            player.MessageNow("Tree: Place a block or type /Mark to use your location.");
        }

        static void TreeCallback(Player player, Vector3I[] marks, object tag)
        {
            ForesterArgs args = (ForesterArgs)tag;
            int blocksPlaced = 0, blocksDenied = 0;
            UndoState undoState = player.DrawBegin(null);
            args.BlockPlacing +=
                (sender, e) =>
               DrawOneBlock(player, player.World.Map, e.Block, new Vector3I(e.Coordinate.X, e.Coordinate.Y, e.Coordinate.Z),
                              BlockChangeContext.Drawn,
                              ref blocksPlaced, ref blocksDenied, undoState);
            Forester.SexyPlant(args, marks[0]);
            DrawingFinished(player, "/Tree: Planted", blocksPlaced, blocksDenied);
        }


        static readonly CommandDescriptor CdCylinder = new CommandDescriptor
        {
            Name = "Cylinder",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            IsConsoleSafe = false,
            NotRepeatable = false,
            RepeatableSelection = true,
            Help = "&SFills the selected rectangular area with a cylinder of blocks. " +
                   "Unless two blocks are specified, leaves the inside hollow.",
            UsableByFrozenPlayers = false,
            Handler = CylinderHandler
        };

        static void CylinderHandler(Player player, Command cmd)
        {
            DrawOperationBegin(player, cmd, new CylinderDrawOperation(player));
        }
        
        
        static readonly CommandDescriptor CdPlace = new CommandDescriptor
        {
            Name = "Place",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/Place",
            Help = "&SPlaces a block below your feet.",
            UsableByFrozenPlayers = false,
            Handler = Place
        };

        static void Place(Player player, Command cmd)
        {
            Block block;
            if (player.LastUsedBlockType == Block.Undefined)
            {
                block = Block.Stone;
            }
            else
            {
                block = player.LastUsedBlockType;
            }
            Vector3I Pos = new Vector3I(player.Position.X / 32, player.Position.Y / 32, (player.Position.Z / 32) - 2);

            if (player.CanPlace(player.World.Map, Pos, player.GetBind(block), BlockChangeContext.Manual) != CanPlaceResult.Allowed)
            {
                player.Message("&WYou are not allowed to build here");
                return;
            }

            Player.RaisePlayerPlacedBlockEvent(player, player.WorldMap, Pos, player.WorldMap.GetBlock(Pos), block, BlockChangeContext.Manual);
            BlockUpdate blockUpdate = new BlockUpdate(null, Pos, block);
            player.World.Map.QueueUpdate(blockUpdate);
            player.Message("Block placed below your feet");
        }

        static readonly CommandDescriptor CdCenter = new CommandDescriptor
        {
            Name = "Center",
            Aliases = new[] {"Centre", "Middle", "Mid"},
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/Center",
            Help = "Places a block at the center for a chosen cuboided area",
            UsableByFrozenPlayers = false,
            Handler = CenterHandler
        };

        static void CenterHandler( Player player, Command cmd ) {
            player.SelectionStart( 2, CenterCallback, null, CdCenter.Permissions );
            player.MessageNow( "Center: Place a block or type /Mark to use your location." );
        }


        static void CenterCallback(Player player, Vector3I[] marks, object tag)
        {
            if (player.LastUsedBlockType != Block.Undefined)
            {
                int sx = Math.Min(marks[0].X, marks[1].X), ex = Math.Max(marks[0].X, marks[1].X),
                sy = Math.Min(marks[0].Y, marks[1].Y), ey = Math.Max(marks[0].Y, marks[1].Y),
                sz = Math.Min(marks[0].Z, marks[1].Z), ez = Math.Max(marks[0].Z, marks[1].Z);

                BoundingBox bounds = new BoundingBox(sx, sy, sz, ex, ey, ez);
                Vector3I cPos = new Vector3I((bounds.XMin + bounds.XMax) / 2,
                    (bounds.YMin + bounds.YMax) / 2,
                    (bounds.ZMin + bounds.ZMax) / 2);
                int blocksDrawn = 0,
                blocksSkipped = 0;
                UndoState undoState = player.DrawBegin(null);

                World playerWorld = player.World;
                if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);
                Map map = player.WorldMap;
                DrawOneBlock(player, player.World.Map, player.GetBind(player.LastUsedBlockType), cPos,
                          BlockChangeContext.Drawn,
                          ref blocksDrawn, ref blocksSkipped, undoState);
                DrawingFinished(player, "Placed", blocksDrawn, blocksSkipped);
            }else{
                player.Message("&WCannot deduce desired block. Click a block or type out the block name.");
            }
        }


       
        static readonly CommandDescriptor CdTower = new CommandDescriptor
        {
            Name = "Tower",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Tower },
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/Tower [/Tower Remove]",
            Help = "&SToggles tower mode on for yourself. All Iron blocks will be replaced with towers.",
            UsableByFrozenPlayers = false,
            Handler = towerHandler
        };

        static void towerHandler(Player player, Command cmd)
        {
            string Param = cmd.Next();
            if (Param == null)
            {
                if (player.towerMode)
                {
                    player.towerMode = false;
                    player.Message("TowerMode has been turned off.");
                    return;
                }
                else
                {
                    player.towerMode = true;
                    player.Message("TowerMode has been turned on. " +
                        "All Iron blocks are now being replaced with Towers.");
                }
            }
            else if (Param.ToLower() == "remove")
            {
                if (player.TowerCache != null)
                {
                    World world = player.World;
                    if (world.Map != null)
                    {
                        player.World.Map.QueueUpdate(new BlockUpdate(null, player.towerOrigin, Block.Air));
                        foreach (Vector3I block in player.TowerCache.Values)
                        {
                            if (world.Map != null)
                            {
                                player.Send(PacketWriter.MakeSetBlock(block, player.WorldMap.GetBlock(block)));
                            }
                        }
                    }
                    player.TowerCache.Clear();
                    return;
                }
                else
                {
                    player.Message("&WThere is no Tower to remove");
                }
            }

            else CdTower.PrintUsage(player);
        }
        static readonly CommandDescriptor CdFly = new CommandDescriptor
        {
            Name = "Fly",
            Category = CommandCategory.Building | CommandCategory.Fun,
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/fly",
            Help = "&SAllows a player to fly on a sheet of glass.",
            UsableByFrozenPlayers = false,
            Handler = Fly
        };

        static void Fly(Player player, Command cmd)
        {
            if (player.IsFlying)
            {
                fCraft.Utils.FlyHandler.GetInstance().StopFlying(player);
                player.Message("You are no longer flying.");
                return;
            }
            else
            {
                if (player.IsUsingWoM)
                {
                    player.Message("You cannot use /fly when using WOM");
                    return;
                }
                fCraft.Utils.FlyHandler.GetInstance().StartFlying(player);
                player.Message("You are now flying, jump!");
            }
        }

         
        #region banx
        static readonly CommandDescriptor CdBanx = new CommandDescriptor
        {
            Name = "Banx",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            IsHidden = false,
            Permissions = new[] { Permission.Ban },
            Usage = "/Banx playerName reason",
            Help = "Bans and undoes a players actions up to 100000 blocks",
            Handler = BanXHandler
        };

        static void BanXHandler(Player player, Command cmd)
        {
            string ban = cmd.Next();

            if (ban == null)
            {
                player.Message("&WError: Enter a player name to BanX");
                return;
            }

            //parse
            if (ban == "-")
            {
                if (player.LastUsedPlayerName != null)
                {
                    ban = player.LastUsedPlayerName;
                }
                else
                {
                    player.Message("Cannot repeat player name: you haven't used any names yet.");
                    return;
                }
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches(player, ban);
            if (target == null) return;
            if (!Player.IsValidName(ban))
            {
                CdBanx.PrintUsage(player);
                return;
            }else{
                UndoAllHandler(player, new Command("/undoall " + target.Name));

                string reason = cmd.NextAll();

                if (reason.Length < 1)
                    reason = "Reason Undefined: BanX";
                try{
                    Player targetPlayer = target.PlayerObject;
                    target.Ban(player, reason, false, true);
                }catch (PlayerOpException ex){
                    player.Message(ex.MessageColored);
                    return;
                }
                    if (player.Can(Permission.Demote, target.Rank))
                    {
                        if (target.Rank != RankManager.LowestRank)
                        {
                            player.LastUsedPlayerName = target.Name;
                            target.ChangeRank(player, RankManager.LowestRank, cmd.NextAll(), false, true, false);
                        }
                        Server.Players.Message("{0}&S was BanX'd by {1}&S (with auto-demote):&W {2}", target.ClassyName, player.ClassyName, reason);
                        IRC.PlayerSomethingMessage(player, "BanX'd (with auto-demote)", target, reason);
                        return;
                    }
                    else
                    {
                        player.Message("&WAuto demote failed: You didn't have the permissions to demote the target player");
                        Server.Players.Message("{0}&S was BanX'd by {1}: &W{2}", target.ClassyName, player.ClassyName, reason);
                        IRC.PlayerSomethingMessage(player, "BanX'd", target, reason);
                    }
                player.Message("&SConfirm the undo with &A/ok");
            }
        }
      
        #endregion


        static readonly CommandDescriptor CdWalls = new CommandDescriptor
        {
            Name = "Walls",
            IsConsoleSafe = false,
            RepeatableSelection = true,
            Category = CommandCategory.Building,
            IsHidden = false,
            Permissions = new[] { Permission.Draw },
            Help = "&SFills a rectangular area of walls",
            Handler = WallsHandler
        };

        static void WallsHandler(Player player, Command cmd)
        {
            DrawOperationBegin(player, cmd, new WallsDrawOperation(player));
        }
        #endregion

        #region DrawOperations & Brushes

        static readonly CommandDescriptor CdCuboid = new CommandDescriptor {
            Name = "Cuboid",
            Aliases = new[] { "blb", "c", "z" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Fills a rectangular area (cuboid) with blocks.",
            Handler = CuboidHandler
        };

        static void CuboidHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidWireframe = new CommandDescriptor {
            Name = "CuboidW",
            Aliases = new[] { "cubw", "cw", "bfb", "frame", "wire" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a wireframe box (a frame) around the selected rectangular area.",
            Handler = CuboidWireframeHandler
        };

        static void CuboidWireframeHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidHollow = new CommandDescriptor {
            Name = "CuboidH",
            Aliases = new[] { "cubh", "ch", "h", "bhb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Surrounds the selected rectangular area with a box of blocks. " +
                   "Unless two blocks are specified, leaves the inside untouched.",
            Handler = CuboidHollowHandler
        };

        static void CuboidHollowHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoid = new CommandDescriptor {
            Name = "Ellipsoid",
            Aliases = new[] { "e" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Fills an ellipsoid-shaped area (elongated sphere) with blocks.",
            Handler = EllipsoidHandler
        };

        static void EllipsoidHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoidHollow = new CommandDescriptor {
            Name = "EllipsoidH",
            Aliases = new[] { "eh" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Surrounds the selected an ellipsoid-shaped area (elongated sphere) with a shell of blocks.",
            Handler = EllipsoidHollowHandler
        };

        static void EllipsoidHollowHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdSphere = new CommandDescriptor {
            Name = "Sphere",
            Aliases = new[] { "sp", "spheroid" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Fills a spherical area with blocks. " +
                   "The first mark denotes the CENTER of the sphere, and " +
                   "distance to the second mark denotes the radius.",
            Handler = SphereHandler
        };

        static void SphereHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new SphereDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdSphereHollow = new CommandDescriptor {
            Name = "SphereH",
            Aliases = new[] { "sph", "hsphere" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Surrounds a spherical area with a shell of blocks. " +
                   "The first mark denotes the CENTER of the sphere, and " +
                   "distance to the second mark denotes the radius.",
            Handler = SphereHollowHandler
        };

        static void SphereHollowHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new SphereHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdLine = new CommandDescriptor {
            Name = "Line",
            Aliases = new[] { "ln" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a continuous line between two points with blocks. " +
                   "Marks do not have to be aligned.",
            Handler = LineHandler
        };

        static void LineHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new LineDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTriangleWireframe = new CommandDescriptor {
            Name = "TriangleW",
            Aliases = new[] { "tw" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws lines between three points, to form a triangle.",
            Handler = TriangleWireframeHandler
        };

        static void TriangleWireframeHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new TriangleWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTriangle = new CommandDescriptor {
            Name = "Triangle",
            Aliases = new[] { "t" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Help = "Draws a triangle between three points.",
            Handler = TriangleHandler
        };

        static void TriangleHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new TriangleDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTorus = new CommandDescriptor {
            Name = "Torus",
            Aliases = new[] { "donut", "bagel", "circle" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Draws a horizontally-oriented torus. The first mark denotes the CENTER of the torus, horizontal " +
                   "distance to the second mark denotes the ring radius, and the vertical distance to the second mark denotes the " +
                   "tube radius",
            Handler = TorusHandler
        };

        static void TorusHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new TorusDrawOperation( player ) );
        }



        public static void DrawOperationBegin( Player player, Command cmd, DrawOperation op ) {
            // try to create instance of player's currently selected brush
            // all command parameters are passed to the brush
            IBrushInstance brush = player.Brush.MakeInstance( player, cmd, op );

            // MakeInstance returns null if there were problems with syntax, abort
            if( brush == null ) return;
            op.Brush = brush;
            player.SelectionStart( op.ExpectedMarks, DrawOperationCallback, op, Permission.Draw );
            player.Message( "{0}: Click {1} blocks or use &H/Mark&S to make a selection.",
                            op.Description, op.ExpectedMarks );
        }


        static void DrawOperationCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) ) return;
            if( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   op.Bounds.Volume );
                op.Cancel();
                return;
            }
            player.Message( "{0}: Processing ~{1} blocks.",
                            op.Description, op.BlocksTotalEstimate );
            op.Begin();
        }

        #endregion


        #region Fill

        static readonly CommandDescriptor CdFill2D = new CommandDescriptor {
            Name = "Fill2D",
            Aliases = new[] { "f2d" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help = "Fills a continuous area with blocks, in 2D. " +
                   "Takes just 1 mark, and replaces blocks of the same type as the block you clicked. " +
                   "Works similar to \"Paint Bucket\" tool in Photoshop. " +
                   "Direction of effect is determined by where the player is looking.",
            Handler = Fill2DHandler
        };

        static void Fill2DHandler( Player player, Command cmd ) {
            Fill2DDrawOperation op = new Fill2DDrawOperation( player );
            op.ReadParams( cmd );
            player.SelectionStart( 1, Fill2DCallback, op, Permission.Draw );
            player.Message( "{0}: Click a block to start filling.", op.Description );
        }


        static void Fill2DCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) ) return;
            if( player.WorldMap.GetBlock( marks[0] ) == Block.Air ) {
                player.Confirm( Fill2DConfirmCallback, op, "{0}: Replace air?", op.Description );
            } else {
                Fill2DConfirmCallback( player, op, false );
            }
        }


        static void Fill2DConfirmCallback( Player player, object tag, bool fromConsole ) {
            Fill2DDrawOperation op = (Fill2DDrawOperation)tag;
            player.Message( "{0}: Filling in a {1}x{1} area...",
                            op.Description, player.Info.Rank.FillLimit );
            op.Begin();
        }

        #endregion


        #region Block Commands

        static readonly CommandDescriptor CdSolid = new CommandDescriptor {
            Name = "Solid",
            Aliases = new[] { "s" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceAdmincrete },
            Help = "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            Handler = SolidHandler
        };

        static void SolidHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Stone ) == Block.Admincrete ) {
                player.ResetBind( Block.Stone );
                player.Message( "Solid: OFF" );
            } else {
                player.Bind( Block.Stone, Block.Admincrete );
                player.Message( "Solid: ON. Stone blocks are replaced with admincrete." );
            }
        }



        static readonly CommandDescriptor CdPaint = new CommandDescriptor {
            Name = "Paint",
            Aliases = new[] { "p" },
            Category = CommandCategory.Building,
            Help = "When paint mode is on, any block you delete will be replaced with the block you are holding. " +
                   "Paint command toggles this behavior on and off.",
            Handler = PaintHandler
        };

        static void PaintHandler( Player player, Command cmd ) {
            player.IsPainting = !player.IsPainting;
            if( player.IsPainting ) {
                player.Message( "Paint mode: ON" );
            } else {
                player.Message( "Paint mode: OFF" );
            }
        }



        static readonly CommandDescriptor CdGrass = new CommandDescriptor {
            Name = "Grass",
            Aliases = new[] { "g" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceGrass },
            Help = "Toggles the grass placement mode. When enabled, any dirt block you place is replaced with a grass block.",
            Handler = GrassHandler
        };

        static void GrassHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Dirt ) == Block.Grass ) {
                player.ResetBind( Block.Dirt );
                player.Message( "Grass: OFF" );
            } else {
                player.Bind( Block.Dirt, Block.Grass );
                player.Message( "Grass: ON. Dirt blocks are replaced with grass." );
            }
        }



        static readonly CommandDescriptor CdWater = new CommandDescriptor {
            Name = "Water",
            Aliases = new[] { "w" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceWater },
            Help = "Toggles the water placement mode. When enabled, any blue or cyan block you place is replaced with water.",
            Handler = WaterHandler
        };

        static void WaterHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Aqua ) == Block.Water ||
                player.GetBind( Block.Cyan ) == Block.Water ||
                player.GetBind( Block.Blue ) == Block.Water ) {
                player.ResetBind( Block.Aqua, Block.Cyan, Block.Blue );
                player.Message( "Water: OFF" );
            } else {
                player.Bind( Block.Aqua, Block.Water );
                player.Bind( Block.Cyan, Block.Water );
                player.Bind( Block.Blue, Block.Water );
                player.Message( "Water: ON. Blue blocks are replaced with water." );
            }
        }



        static readonly CommandDescriptor CdLava = new CommandDescriptor {
            Name = "Lava",
            Aliases = new[] { "l" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceLava },
            Help = "Toggles the lava placement mode. When enabled, any red block you place is replaced with lava.",
            Handler = LavaHandler
        };

        static void LavaHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Red ) == Block.Lava ) {
                player.ResetBind( Block.Red );
                player.Message( "Lava: OFF" );
            } else {
                player.Bind( Block.Red, Block.Lava );
                player.Message( "Lava: ON. Red blocks are replaced with lava." );
            }
        }


        static readonly CommandDescriptor CdBind = new CommandDescriptor {
            Name = "Bind",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            Help = "&SAssigns one blocktype to another. " +
                   "Allows to build blocktypes that are not normally buildable directly: admincrete, lava, water, grass, double step. " +
                   "Calling &H/Bind BlockType&S without second parameter resets the binding. If used with no params, ALL bindings are reset.",
            Usage = "/Bind OriginalBlockType ReplacementBlockType",
            Handler = BindHandler
        };

        static void BindHandler( Player player, Command cmd ) {
            string originalBlockName = cmd.Next();
            if( originalBlockName == null ) {
                player.Message( "All bindings have been reset." );
                player.ResetAllBinds();
                return;
            }
            Block originalBlock = Map.GetBlockByName( originalBlockName );
            if( originalBlock == Block.Undefined ) {
                player.Message( "Bind: Unrecognized block name: {0}", originalBlockName );
                return;
            }

            string replacementBlockName = cmd.Next();
            if( replacementBlockName == null ) {
                if( player.GetBind( originalBlock ) != originalBlock ) {
                    player.Message( "{0} is no longer bound to {1}",
                                    originalBlock,
                                    player.GetBind( originalBlock ) );
                    player.ResetBind( originalBlock );
                } else {
                    player.Message( "{0} is not bound to anything.",
                                    originalBlock );
                }
                return;
            }

            if( cmd.HasNext ) {
                CdBind.PrintUsage( player );
                return;
            }

            Block replacementBlock = Map.GetBlockByName( replacementBlockName );
            if( replacementBlock == Block.Undefined ) {
                player.Message( "Bind: Unrecognized block name: {0}", replacementBlockName );
            } else {
                Permission permission = Permission.Build;
                switch( replacementBlock ) {
                    case Block.Grass:
                        permission = Permission.PlaceGrass;
                        break;
                    case Block.Admincrete:
                        permission = Permission.PlaceAdmincrete;
                        break;
                    case Block.Water:
                        permission = Permission.PlaceWater;
                        break;
                    case Block.Lava:
                        permission = Permission.PlaceLava;
                        break;
                }
                if( player.Can( permission ) ) {
                    player.Bind( originalBlock, replacementBlock );
                    player.Message( "{0} is now replaced with {1}", originalBlock, replacementBlock );
                } else {
                    player.Message( "&WYou do not have {0} permission.", permission );
                }
            }
        }

        #endregion


        static void DrawOneBlock( [NotNull] Player player, [NotNull] Map map, Block drawBlock, Vector3I coord,
                                  BlockChangeContext context, ref int blocks, ref int blocksDenied, UndoState undoState ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            if( !map.InBounds( coord ) ) return;
            Block block = map.GetBlock( coord );
            if( block == drawBlock ) return;

            if( player.CanPlace( map, coord, drawBlock, context ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }

            map.QueueUpdate( new BlockUpdate( null, coord, drawBlock ) );
            Player.RaisePlayerPlacedBlockEvent( player, map, coord, block, drawBlock, context );

            if( !undoState.IsTooLargeToUndo ) {
                if( !undoState.Add( coord, block ) ) {
                    player.MessageNow( "NOTE: This draw command is too massive to undo." );
                    player.LastDrawOp = null;
                }
            }
            blocks++;
        }


        static void DrawingFinished( [NotNull] Player player, string verb, int blocks, int blocksDenied ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( blocks == 0 ) {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "No blocks could be {0} due to permission issues.", verb.ToLower() );
                } else {
                    player.MessageNow( "No blocks were {0}.", verb.ToLower() );
                }
            } else {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "{0} {1} blocks ({2} blocks skipped due to permission issues)... " +
                                       "The map is now being updated.", verb, blocks, blocksDenied );
                } else {
                    player.MessageNow( "{0} {1} blocks... The map is now being updated.", verb, blocks );
                }
            }
            if( blocks > 0 ) {
                player.Info.ProcessDrawCommand( blocks );
                Server.RequestGC();
            }
        }


        #region Replace

        static void ReplaceHandlerInternal( IBrush factory, Player player, Command cmd ) {
            CuboidDrawOperation op = new CuboidDrawOperation( player );
            IBrushInstance brush = factory.MakeInstance( player, cmd, op );
            if( brush == null ) return;
            op.Brush = brush;

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.MessageNow( "{0}: Click 2 blocks or use &H/Mark&S to make a selection.",
                               op.Brush.InstanceDescription );
        }


        static readonly CommandDescriptor CdReplace = new CommandDescriptor {
            Name = "Replace",
            Aliases = new[] { "r" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection=true,
            Usage = "/Replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) in an area.",
            Handler = ReplaceHandler
        };

        static void ReplaceHandler( Player player, Command cmd ) {
            var replaceBrush = ReplaceBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null ) return;
            ReplaceHandlerInternal( replaceBrush, player, cmd );
        }



        static readonly CommandDescriptor CdReplaceNot = new CommandDescriptor {
            Name = "ReplaceNot",
            Aliases = new[] { "rn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            RepeatableSelection = true,
            Usage = "/ReplaceNot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            Help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            Handler = ReplaceNotHandler
        };

        static void ReplaceNotHandler( Player player, Command cmd ) {
            var replaceBrush = ReplaceNotBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null ) return;
            ReplaceHandlerInternal( replaceBrush, player, cmd );
        }



        static readonly CommandDescriptor CdReplaceBrush = new CommandDescriptor {
            Name = "ReplaceBrush",
            Aliases = new[] { "rb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            RepeatableSelection = true,
            Usage = "/ReplaceBrush Block BrushName [Params]",
            Help = "Replaces all blocks of specified type(s) in an area with output of a given brush. " +
                   "See &H/Help brush&S for a list of available brushes.",
            Handler = ReplaceBrushHandler
        };

        static void ReplaceBrushHandler( Player player, Command cmd ) {
            var replaceBrush = ReplaceBrushBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null ) return;
            ReplaceHandlerInternal( replaceBrush, player, cmd );
        }
        #endregion


        #region Undo / Redo

        static readonly CommandDescriptor CdUndo = new CommandDescriptor {
            Name = "Undo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "&SSelectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = UndoHandler
        };

        static void UndoHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );
            if( cmd.HasNext ) {
                player.Message( "Undo command takes no parameters. Did you mean to do &H/UndoPlayer&S or &H/UndoArea&S?" );
                return;
            }

            string msg = "Undo: ";
            UndoState undoState = player.UndoPop();
            if( undoState == null ) {
                player.MessageNow( "There is currently nothing to undo." );
                return;
            }

            // Cancel the last DrawOp, if still in progress
            if( undoState.Op != null && !undoState.Op.IsDone && !undoState.Op.IsCancelled ) {
                undoState.Op.Cancel();
                msg += String.Format( "Cancelled {0} (was {1}% done). ",
                                     undoState.Op.Description,
                                     undoState.Op.PercentDone );
            }

            // Check if command was too massive.
            if( undoState.IsTooLargeToUndo ) {
                if( undoState.Op != null ) {
                    player.MessageNow( "Cannot undo {0}: too massive.", undoState.Op.Description );
                } else {
                    player.MessageNow( "Cannot undo: too massive." );
                }
                return;
            }

            // no need to set player.drawingInProgress here because this is done on the user thread
            if (Updater.CurrentRelease.IsFlagged(ReleaseFlags.Dev))
            {
                Logger.Log(LogType.UserActivity,
                            "Player {0} initiated /Undo affecting {1} blocks (on world {2})",
                            player.Name,
                            undoState.Buffer.Count,
                            playerWorld.Name);
            }

            msg += String.Format( "Restoring {0} blocks. Type &H/Redo&S to reverse.",
                                  undoState.Buffer.Count );
            player.MessageNow( msg );

            var op = new UndoDrawOperation( player, undoState, false );
            op.Prepare( new Vector3I[0] );
            op.Begin();
        }


        static readonly CommandDescriptor CdRedo = new CommandDescriptor {
            Name = "Redo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "&SSelectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = RedoHandler
        };

        static void RedoHandler( Player player, Command cmd ) {
            if( cmd.HasNext ) {
                CdRedo.PrintUsage( player );
                return;
            }

            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            UndoState redoState = player.RedoPop();
            if( redoState == null ) {
                player.MessageNow( "There is currently nothing to redo." );
                return;
            }

            string msg = "Redo: ";
            if( redoState.Op != null && !redoState.Op.IsDone ) {
                redoState.Op.Cancel();
                msg += String.Format( "Cancelled {0} (was {1}% done). ",
                                     redoState.Op.Description,
                                     redoState.Op.PercentDone );
            }

            // no need to set player.drawingInProgress here because this is done on the user thread
            Logger.Log( LogType.UserActivity,
                        "Player {0} initiated /Redo affecting {1} blocks (on world {2})",
                        player.Name,
                        redoState.Buffer.Count,
                        playerWorld.Name );

            msg += String.Format( "Restoring {0} blocks. Type &H/Undo&S to reverse.",
                                  redoState.Buffer.Count );
            player.MessageNow( msg );

            var op = new UndoDrawOperation( player, redoState, true );
            op.Prepare( new Vector3I[0] );
            op.Begin();
        }

        #endregion


        #region Copy and Paste

        static readonly CommandDescriptor CdCopySlot = new CommandDescriptor {
            Name = "CopySlot",
            Aliases = new[] { "cs" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Usage = "/CopySlot [#]",
            Help = "&SSelects a slot to copy to/paste from. The maximum number of slots is limited per-rank.",
            Handler = CopySlotHandler
        };

        static void CopySlotHandler( Player player, Command cmd ) {
            int slotNumber;
            if( cmd.NextInt( out slotNumber ) ) {
                if( cmd.HasNext ) {
                    CdCopySlot.PrintUsage( player );
                    return;
                }
                if( slotNumber < 1 || slotNumber > player.Info.Rank.CopySlots ) {
                    player.Message( "CopySlot: Select a number between 1 and {0}", player.Info.Rank.CopySlots );
                } else {
                    player.CopySlot = slotNumber - 1;
                    CopyState info = player.GetCopyInformation();
                    if( info == null ) {
                        player.Message( "Selected copy slot {0} (unused).", slotNumber );
                    } else {
                        player.Message( "Selected copy slot {0}: {1} blocks from {2}, {3} old.",
                                        slotNumber, info.Buffer.Length,
                                        info.OriginWorld, DateTime.UtcNow.Subtract( info.CopyTime ).ToMiniString() );
                    }
                }
            } else {
                CopyState[] slots = player.CopyInformation;
                player.Message( "Using {0} of {1} slots. Selected slot: {2}",
                                slots.Count( info => info != null ), player.Info.Rank.CopySlots, player.CopySlot + 1 );
                for( int i = 0; i < slots.Length; i++ ) {
                    if( slots[i] != null ) {
                        player.Message( "  {0}: {1} blocks from {2}, {3} old",
                                        i + 1, slots[i].Buffer.Length,
                                        slots[i].OriginWorld, DateTime.UtcNow.Subtract( slots[i].CopyTime ).ToMiniString() );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdCopy = new CommandDescriptor {
            Name = "Copy", 
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "&SCopy blocks for pasting. " +
                   "Used together with &H/Paste&S and &H/PasteNot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/Copy&S from.",
            Handler = CopyHandler
        };

        static void CopyHandler( Player player, Command cmd ) {
            if( cmd.HasNext ) {
                CdCopy.PrintUsage( player );
                return;
            }
            player.SelectionStart( 2, CopyCallback, null, CdCopy.Permissions );
            player.MessageNow( "Copy: Place a block or type /Mark to use your location." );
        }


        static void CopyCallback( Player player, Vector3I[] marks, object tag ) {
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );
            BoundingBox bounds = new BoundingBox( sx, sy, sz, ex, ey, ez );

            int volume = bounds.Volume;
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyState copyInfo = new CopyState( marks[0], marks[1] );

            Map map = player.WorldMap;
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int z = sz; z <= ez; z++ ) {
                        copyInfo.Buffer[x - sx, y - sy, z - sz] = map.GetBlock( x, y, z );
                    }
                }
            }

            copyInfo.OriginWorld = playerWorld.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            player.SetCopyInformation( copyInfo );

            player.MessageNow( "{0} blocks copied into slot #{1}. You can now &H/Paste",
                               volume, player.CopySlot + 1 );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.Orientation.X == 1 ? "bottom" : "top"),
                               (copyInfo.Orientation.Y == 1 ? "south" : "north"),
                               (copyInfo.Orientation.Z == 1 ? "east" : "west") );

            Logger.Log( LogType.UserActivity,
                        "{0} copied {1} blocks from {2} (between {3} and {4}).",
                        player.Name, volume, playerWorld.Name,
                        bounds.MinVertex, bounds.MaxVertex );
        }



        static readonly CommandDescriptor CdCut = new CommandDescriptor {
            Name = "Cut",
            Aliases = new[] { "x" , "snip" , "cutout"},
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "&SCopies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                   "Used together with &H/Paste&S and &H/PasteNot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/Cut&S from.",
            Usage = "/Cut [FillBlock]",
            Handler = CutHandler
        };

        static void CutHandler( Player player, Command cmd ) {
            Block fillBlock = Block.Air;
            if( cmd.HasNext ) {
                fillBlock = cmd.NextBlock( player );
                if( fillBlock == Block.Undefined ) return;
                if( cmd.HasNext ) {
                    CdCut.PrintUsage( player );
                    return;
                }
            }

            CutDrawOperation op = new CutDrawOperation( player ) {
                Brush = new NormalBrush( fillBlock )
            };

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            if( fillBlock != Block.Air ) {
                player.Message( "Cut/{0}: Click 2 blocks or use &H/Mark&S to make a selection.",
                                fillBlock );
            } else {
                player.Message( "Cut: Click 2 blocks or use &H/Mark&S to make a selection." );
            }
        }


        static readonly CommandDescriptor CdMirror = new CommandDescriptor {
            Name = "Mirror",
            Aliases = new[] { "flip" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/Mirror X Y",
            Usage = "/Mirror [X] [Y] [Z]",
            Handler = MirrorHandler
        };

        static void MirrorHandler( Player player, Command cmd ) {
            CopyState originalInfo = player.GetCopyInformation();
            if( originalInfo == null ) {
                player.MessageNow( "Nothing to flip! Copy something first." );
                return;
            }

            // clone to avoid messing up any paste-in-progress
            CopyState info = new CopyState( originalInfo );

            bool flipX = false, flipY = false, flipH = false;
            string axis;
            while( (axis = cmd.Next()) != null ) {
                foreach( char c in axis.ToLower() ) {
                    if( c == 'x' ) flipX = true;
                    if( c == 'y' ) flipY = true;
                    if( c == 'z' ) flipH = true;
                }
            }

            if( !flipX && !flipY && !flipH ) {
                CdMirror.PrintUsage( player );
                return;
            }

            Block block;

            if( flipX ) {
                int left = 0;
                int right = info.Dimensions.X - 1;
                while( left < right ) {
                    for( int y = info.Dimensions.Y - 1; y >= 0; y-- ) {
                        for( int z = info.Dimensions.Z - 1; z >= 0; z-- ) {
                            block = info.Buffer[left, y, z];
                            info.Buffer[left, y, z] = info.Buffer[right, y, z];
                            info.Buffer[right, y, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipY ) {
                int left = 0;
                int right = info.Dimensions.Y - 1;
                while( left < right ) {
                    for( int x = info.Dimensions.X - 1; x >= 0; x-- ) {
                        for( int z = info.Dimensions.Z - 1; z >= 0; z-- ) {
                            block = info.Buffer[x, left, z];
                            info.Buffer[x, left, z] = info.Buffer[x, right, z];
                            info.Buffer[x, right, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipH ) {
                int left = 0;
                int right = info.Dimensions.Z - 1;
                while( left < right ) {
                    for( int x = info.Dimensions.X - 1; x >= 0; x-- ) {
                        for( int y = info.Dimensions.Y - 1; y >= 0; y-- ) {
                            block = info.Buffer[x, y, left];
                            info.Buffer[x, y, left] = info.Buffer[x, y, right];
                            info.Buffer[x, y, right] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipX ) {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along all axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) and Y (north/south) axes." );
                    }
                } else {
                    if( flipH ) {
                        player.Message( "Flipped copy along X (east/west) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) axis." );
                    }
                }
            } else {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along Y (north/south) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along Y (north/south) axis." );
                    }
                } else {
                    player.Message( "Flipped copy along Z (vertical) axis." );
                }
            }

            player.SetCopyInformation( info );
        }



        static readonly CommandDescriptor CdRotate = new CommandDescriptor {
            Name = "Rotate",
            Aliases = new[] { "spin" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            Usage = "/Rotate (-90|90|180|270) (X|Y|Z)",
            Handler = RotateHandler
        };

        static void RotateHandler( Player player, Command cmd ) {
            CopyState originalInfo = player.GetCopyInformation();
            if( originalInfo == null ) {
                player.MessageNow( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                CdRotate.PrintUsage( player );
                return;
            }

            string axisName = cmd.Next();
            Axis axis = Axis.Z;
            if( axisName != null ) {
                switch( axisName.ToLower() ) {
                    case "x":
                        axis = Axis.X;
                        break;
                    case "y":
                        axis = Axis.Y;
                        break;
                    case "z":
                    case "h":
                        axis = Axis.Z;
                        break;
                    default:
                        CdRotate.PrintUsage( player );
                        return;
                }
            }

            // allocate the new buffer
            Block[, ,] oldBuffer = originalInfo.Buffer;
            Block[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new Block[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == Axis.X ) {
                newBuffer = new Block[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];

            } else if( axis == Axis.Y ) {
                newBuffer = new Block[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];

            } else { // axis == Axis.Z
                newBuffer = new Block[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
            }

            // clone to avoid messing up any paste-in-progress
            CopyState info = new CopyState( originalInfo, newBuffer );

            // construct the rotation matrix
            int[,] matrix = new[,]{
                {1,0,0},
                {0,1,0},
                {0,0,1}
            };

            int a, b;
            switch( axis ) {
                case Axis.X:
                    a = 1;
                    b = 2;
                    break;
                case Axis.Y:
                    a = 0;
                    b = 2;
                    break;
                default:
                    a = 0;
                    b = 1;
                    break;
            }

            switch( degrees ) {
                case 90:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = -1;
                    matrix[b, a] = 1;
                    break;
                case 180:
                    matrix[a, a] = -1;
                    matrix[b, b] = -1;
                    break;
                case -90:
                case 270:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = 1;
                    matrix[b, a] = -1;
                    break;
            }

            // apply the rotation matrix
            for( int x = oldBuffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = oldBuffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = oldBuffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        int nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                                 (matrix[0, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                                 (matrix[0, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        int ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                                 (matrix[1, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                                 (matrix[1, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        int nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                                 (matrix[2, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                                 (matrix[2, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message( "Rotated copy (slot {0}) by {1} degrees around {2} axis.",
                            info.Slot + 1, degrees, axis );
            player.SetCopyInformation( info );
        }



        static readonly CommandDescriptor CdPasteX = new CommandDescriptor {
            Name = "PasteX",
            Aliases = new[] { "px" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks, aligned. Used together with &H/Copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Takes 2 marks: first sets the origin of pasting, and second sets the direction where to paste.",
            Usage = "/PasteX [IncludedBlock [AnotherOne etc]]",
            Handler = PasteXHandler
        };

        static void PasteXHandler( Player player, Command cmd ) {
            PasteDrawOperation op = new PasteDrawOperation( player, false );
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            player.MessageNow( "{0}: Click 2 blocks or use &H/Mark&S to make a selection.",
                               op.Description );
        }



        static readonly CommandDescriptor CdPasteNotX = new CommandDescriptor {
            Name = "PasteNotX",
            Aliases = new[] { "pnx", "pxn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks, aligned, except the given block type(s). " +
                    "Used together with &H/Copy&S command. " +
                   "Takes 2 marks: first sets the origin of pasting, and second sets the direction where to paste.",
            Usage = "/PasteNotX ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotXHandler
        };

        static void PasteNotXHandler( Player player, Command cmd ) {
            PasteDrawOperation op = new PasteDrawOperation( player, true );
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            player.MessageNow( "{0}: Click 2 blocks or use &H/Mark&S to make a selection.",
                               op.Description );
        }



        static readonly CommandDescriptor CdPaste = new CommandDescriptor {
            Name = "Paste",
            Aliases = new string[] { "v" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "&SPastes previously copied blocks. Used together with &H/Copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Alignment semantics are... complicated.",
            Usage = "/Paste [IncludedBlock [AnotherOne etc]]",
            Handler = PasteHandler
        };

        static void PasteHandler( Player player, Command cmd ) {
            QuickPasteDrawOperation op = new QuickPasteDrawOperation( player, false );
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( 1, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            player.MessageNow( "{0}: Click a block or use &H/Mark&S to begin pasting.",
                               op.Description );
        }



        static readonly CommandDescriptor CdPasteNot = new CommandDescriptor {
            Name = "PasteNot",
            Aliases = new[] { "pn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            RepeatableSelection = true,
            Help = "Pastes previously copied blocks, except the given block type(s). " +
                   "Used together with &H/Copy&S command. " +
                   "Alignment semantics are... complicated.",
            Usage = "/PasteNot ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotHandler
        };

        static void PasteNotHandler( Player player, Command cmd ) {
            QuickPasteDrawOperation op = new QuickPasteDrawOperation( player, true );
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( 1, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            player.MessageNow( "{0}: Click a block or use &H/Mark&S to begin pasting.",
                               op.Description );
        }

        #endregion


        #region Restore

        const BlockChangeContext RestoreContext = BlockChangeContext.Drawn | BlockChangeContext.Restored;


        static readonly CommandDescriptor CdRestore = new CommandDescriptor {
            Name = "Restore",
            Category = CommandCategory.World,
            Permissions = new[] {
                Permission.Draw,
                Permission.DrawAdvanced,
                Permission.CopyAndPaste,
                Permission.ManageWorlds
            },
            RepeatableSelection = true,
            Usage = "/Restore FileName",
            Help = "&SSelectively restores/pastes part of mapfile into the current world. "+
                   "If the filename contains spaces, surround it with quote marks.",
            Handler = RestoreHandler
        };

        static void RestoreHandler( Player player, Command cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                CdRestore.PrintUsage( player );
                return;
            }
            if( cmd.HasNext ) {
                CdRestore.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null ) return;

            Map map;
            if( !MapUtility.TryLoad( fullFileName, out map ) ) {
                player.Message( "Could not load the given map file ({0})", fileName );
                return;
            }

            Map playerMap = player.WorldMap;
            if( playerMap.Width != map.Width || playerMap.Length != map.Length || playerMap.Height != map.Height ) {
                player.Message( "Mapfile dimensions must match your current world's dimensions ({0}x{1}x{2})",
                                playerMap.Width,
                                playerMap.Length,
                                playerMap.Height );
                return;
            }

            map.Metadata["fCraft.Temp", "FileName"] = fullFileName;
            player.SelectionStart( 2, RestoreCallback, map, CdRestore.Permissions );
            player.MessageNow( "Restore: Select the area to restore. To mark a corner, place/click a block or type &H/Mark" );
        }


        static void RestoreCallback( Player player, Vector3I[] marks, object tag ) {
            BoundingBox selection = new BoundingBox( marks[0], marks[1] );
            Map map = (Map)tag;

            if( !player.CanDraw( selection.Volume ) ) {
                player.MessageNow(
                    "You are only allowed to restore up to {0} blocks at a time. This would affect {1} blocks.",
                    player.Info.Rank.DrawLimit,
                    selection.Volume );
                return;
            }

            int blocksDrawn = 0,
                blocksSkipped = 0;
            UndoState undoState = player.DrawBegin( null );

            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );
            Map playerMap = player.WorldMap;
            for( int x = selection.XMin; x <= selection.XMax; x++ ) {
                for( int y = selection.YMin; y <= selection.YMax; y++ ) {
                    for( int z = selection.ZMin; z <= selection.ZMax; z++ ) {
                        DrawOneBlock( player, playerMap, map.GetBlock( x, y, z ), new Vector3I( x, y, z ),
                                      RestoreContext,
                                      ref blocksDrawn, ref blocksSkipped, undoState );
                    }
                }
            }

            Logger.Log( LogType.UserActivity,
                        "{0} restored {1} blocks on world {2} (@{3},{4},{5} - {6},{7},{8}) from file {9}.",
                        player.Name, blocksDrawn,
                        playerWorld.Name,
                        selection.XMin, selection.YMin, selection.ZMin,
                        selection.XMax, selection.YMax, selection.ZMax,
                        map.Metadata["fCraft.Temp", "FileName"] );

            DrawingFinished( player, "Restored", blocksDrawn, blocksSkipped );
        }

        #endregion


        #region Mark, Cancel

        static readonly CommandDescriptor CdMark = new CommandDescriptor {
            Name = "Mark",
            Aliases = new[] { "m" },
            Category = CommandCategory.Building,
            Usage = "/Mark&S or &H/Mark X Y H",
            Help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "If three numbers are given, those coordinates are used instead.",
            Handler = MarkHandler
        };

        static void MarkHandler( Player player, Command cmd ) {
            Map map = player.WorldMap;
            int x, y, z;
            Vector3I coords;
            if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {
                if( cmd.HasNext ) {
                    CdMark.PrintUsage( player );
                    return;
                }
                coords = new Vector3I( x, y, z );
            } else {
                coords = player.Position.ToBlockCoords();
            }
            coords.X = Math.Min( map.Width - 1, Math.Max( 0, coords.X ) );
            coords.Y = Math.Min( map.Length - 1, Math.Max( 0, coords.Y ) );
            coords.Z = Math.Min(map.Height - 1, Math.Max(0, coords.Z));

            if (player.SelectionMarksExpected > 0)
            {
                player.SelectionAddMark(coords, true);
            }
            else if(player.markSet > 0)
            {
                player.selectedMarks.Add(coords);
                player.SelectMarks();
            }
            else
            {
                player.MessageNow("Cannot mark - no selection in progress.");
            }
        }



        static readonly CommandDescriptor CdCancel = new CommandDescriptor {
            Name = "Cancel",
            Category = CommandCategory.Building,
            NotRepeatable = true,
            Help = "&SCancels current selection (for drawing or zoning) operation, for instance if you misclicked on the first block. " +
                   "If you wish to stop a drawing in-progress, use &H/Undo&S instead.",
            Handler = CancelHandler
        };

        static void CancelHandler( Player player, Command cmd ) {
            if( cmd.HasNext ) {
                CdCancel.PrintUsage( player );
                return;
            }
            if( player.IsMakingSelection ) {
                player.SelectionCancel();
                player.MessageNow( "Selection cancelled." );
                player.ParseMessage("/nvm", false, false);//LOL lazy
            } else {
                player.MessageNow( "There is currently nothing to cancel." );
            }
        }

        #endregion


        #region UndoPlayer and UndoArea

        /*struct UndoAreaCountArgs {
            [NotNull]
            public PlayerInfo Target;

            [NotNull]
            public World World;

            public int MaxBlocks;

            [NotNull]
            public BoundingBox Area;
        }

        struct UndoAreaTimeArgs {
            [NotNull]
            public PlayerInfo Target;

            [NotNull]
            public World World;

            public TimeSpan Time;

            [NotNull]
            public BoundingBox Area;
        }*/

        static readonly CommandDescriptor CdUndoArea = new CommandDescriptor {
            Name = "UndoArea",
            Aliases = new[] { "ua" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            RepeatableSelection = true,
            Usage = "/UndoArea PlayerName [TimeSpan|BlockCount]",
            Help = "Reverses changes made by a given player in the current world, in the given area.",
            Handler = UndoAreaHandler
        };

        static void UndoAreaHandler( Player player, Command cmd ) {

            BlockDBUndoArgs args = ParseBlockDBUndoParams(player, cmd, "UndoArea", false);
            if (args == null) return;

            Permission permission;
            if (args.Targets.Length == 0)
            {
                permission = Permission.UndoAll;
            }
            else
            {
                permission = Permission.UndoOthersActions;
            }
            player.SelectionStart(2, UndoAreaSelectionCallback, args, permission);
            player.MessageNow("UndoArea: Click or &H/Mark&S 2 blocks.");
        }

        static readonly CommandDescriptor CdUndoAreaNot = new CommandDescriptor
        {
            Name = "UndoAreaNot",
            Aliases = new[] { "uan", "una" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions, Permission.UndoAll },
            RepeatableSelection = true,
            Usage = "/UndoArea (TimeSpan|BlockCount) PlayerName [AnotherName]",
            Help = "Reverses changes made by everyone EXCEPT the given player(s). " +
                   "Applies to a selected area in the current world. " +
                   "More than one player name can be given at a time.",
            Handler = UndoAreaNotHandler
        };

        static void UndoAreaNotHandler(Player player, Command cmd)
        {
            BlockDBUndoArgs args = ParseBlockDBUndoParams(player, cmd, "UndoAreaNot", true);
            if (args == null) return;

            player.SelectionStart(2, UndoAreaSelectionCallback, args, CdUndoAreaNot.Permissions);
            player.MessageNow("UndoAreaNot: Click or &H/Mark&S 2 blocks.");
        }

        static void UndoAreaSelectionCallback(Player player, Vector3I[] marks, object tag)
        {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            args.Area = new BoundingBox(marks[0], marks[1]);
            Scheduler.NewBackgroundTask(UndoAreaLookup)
                     .RunOnce(args, TimeSpan.Zero);
        }

        // called after player types "/ok" to the confirmation prompt.
        static void BlockDBUndoConfirmCallback(Player player, object tag, bool fromConsole)
        {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            string cmdName = (args.Area == null ? "UndoArea" : "UndoPlayer");
            if (args.Not) cmdName += "Not";

            // Produce 
            Vector3I[] coords;
            if (args.Area != null)
            {
                coords = new[] { args.Area.MinVertex, args.Area.MaxVertex };
            }
            else
            {
                coords = new Vector3I[0];
            }

            // Produce a brief param description for BlockDBDrawOperation
            string description;
            if (args.CountLimit > 0)
            {
                if (args.Targets.Length == 0)
                {
                    description = args.CountLimit.ToString(NumberFormatInfo.InvariantInfo);
                }
                else if (args.Not)
                {
                    description = String.Format("{0} by everyone except {1}",
                                                 args.CountLimit,
                                                 args.Targets.JoinToString(p => p.Name));
                }
                else
                {
                    description = String.Format("{0} by {1}",
                                                 args.CountLimit,
                                                 args.Targets.JoinToString(p => p.Name));
                }
            }
            else
            {
                if (args.Targets.Length == 0)
                {
                    description = args.AgeLimit.ToMiniString();
                }
                else if (args.Not)
                {
                    description = String.Format("{0} by everyone except {1}",
                                                 args.AgeLimit.ToMiniString(),
                                                 args.Targets.JoinToString(p => p.Name));
                }
                else
                {
                    description = String.Format("{0} by {1}",
                                                 args.AgeLimit.ToMiniString(),
                                                 args.Targets.JoinToString(p => p.Name));
                }
            }

            // start undoing (using DrawOperation infrastructure)
            var op = new BlockDBDrawOperation(player, cmdName, description, coords.Length);
            op.Prepare(coords, args.Entries);

            // log operation
            string targetList;
            if (args.Targets.Length == 0)
            {
                targetList = "(everyone)";
            }
            else if (args.Not)
            {
                targetList = "(everyone) except " + args.Targets.JoinToClassyString();
            }
            else
            {
                targetList = args.Targets.JoinToClassyString();
            }
            Logger.Log(LogType.UserActivity,
                        "{0}: Player {1} will undo {2} changes (limit of {3}) by {4} on world {5}",
                        cmdName,
                        player.Name,
                        args.Entries.Length,
                        args.CountLimit == 0 ? args.AgeLimit.ToMiniString() : args.CountLimit.ToString(NumberFormatInfo.InvariantInfo),
                        targetList,
                        args.World.Name);

            op.Begin();
        }

        // Looks up the changes in BlockDB and prints a confirmation prompt. Runs on a background thread.
        static void UndoAreaLookup(SchedulerTask task)
        {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            bool allPlayers = (args.Targets.Length == 0);
            string cmdName = (args.Not ? "UndoAreaNot" : "UndoArea");

            // prepare to look up
            string targetList;
            if (allPlayers)
            {
                targetList = "EVERYONE";
            }
            else if (args.Not)
            {
                targetList = "EVERYONE except " + args.Targets.JoinToClassyString();
            }
            else
            {
                targetList = args.Targets.JoinToClassyString();
            }
            BlockDBEntry[] changes;

            if (args.CountLimit > 0)
            {
                // count-limited lookup
                if (args.Targets.Length == 0)
                {
                    changes = args.World.BlockDB.Lookup(args.CountLimit, args.Area);
                }
                else
                {
                    changes = args.World.BlockDB.Lookup(args.CountLimit, args.Area, args.Targets, args.Not);
                }
                if (changes.Length > 0)
                {
                    Logger.Log(LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name);
                    args.Player.Confirm(BlockDBUndoConfirmCallback, args,
                                         "Undo last {0} changes made here by {1}&S?",
                                         changes.Length, targetList);
                }

            }
            else
            {
                // time-limited lookup
                if (args.Targets.Length == 0)
                {
                    changes = args.World.BlockDB.Lookup(Int32.MaxValue, args.Area, args.AgeLimit);
                }
                else
                {
                    changes = args.World.BlockDB.Lookup(Int32.MaxValue, args.Area, args.Targets, args.Not, args.AgeLimit);
                }
                if (changes.Length > 0)
                {
                    Logger.Log(LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name);
                    args.Player.Confirm(BlockDBUndoConfirmCallback, args,
                                         "Undo changes ({0}) made here by {1}&S in the last {2}?",
                                         changes.Length, targetList, args.AgeLimit.ToMiniString());
                }
            }

            // stop if there's nothing to undo
            if (changes.Length == 0)
            {
                args.Player.Message("{0}: Found nothing to undo.", cmdName);
            }
            else
            {
                args.Entries = changes;
            }
        }


        static void UndoAreaTimeSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            args.Area = new BoundingBox(marks[0], marks[1]);
            Scheduler.NewBackgroundTask(UndoAreaLookup)
                     .RunOnce(args, TimeSpan.Zero);
        }
      


        static readonly CommandDescriptor CdUndoPlayer = new CommandDescriptor {
            Name = "UndoPlayer",
            Aliases = new[] { "up", "undox" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/UndoPlayer PlayerName [TimeSpan|BlockCount]",
            Help = "Reverses changes made by a given player in the current world.",
            Handler = UndoPlayerHandler
        };

        static void UndoPlayerHandler( Player player, Command cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams(player, cmd, "UndoPlayer", false);
            if (args == null) return;
            Scheduler.NewBackgroundTask(UndoPlayerLookup)
                     .RunOnce(args, TimeSpan.Zero);
        }

        static readonly CommandDescriptor CdUndoPlayerNot = new CommandDescriptor
        {
            Name = "UndoPlayerNot",
            Aliases = new[] { "upn", "unp" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions, Permission.UndoAll },
            Usage = "/UndoPlayerNot (TimeSpan|BlockCount) PlayerName [AnotherName...]",
            Help = "Reverses changes made by a given player in the current world. " +
                   "More than one player name can be given at a time.",
            Handler = UndoPlayerNotHandler
        };

        static void UndoPlayerNotHandler(Player player, Command cmd)
        {
            BlockDBUndoArgs args = ParseBlockDBUndoParams(player, cmd, "UndoPlayerNot", true);
            if (args == null) return;
            Scheduler.NewBackgroundTask(UndoPlayerLookup)
                     .RunOnce(args, TimeSpan.Zero);
        }

        // Looks up the changes in BlockDB and prints a confirmation prompt. Runs on a background thread.
        static void UndoPlayerLookup(SchedulerTask task)
        {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            bool allPlayers = (args.Targets.Length == 0);
            string cmdName = (args.Not ? "UndoPlayerNot" : "UndoPlayer");

            // prepare to look up
            string targetList;
            if (allPlayers)
            {
                targetList = "EVERYONE";
            }
            else if (args.Not)
            {
                targetList = "EVERYONE except " + args.Targets.JoinToClassyString();
            }
            else
            {
                targetList = args.Targets.JoinToClassyString();
            }
            BlockDBEntry[] changes;

            if (args.CountLimit > 0)
            {
                // count-limited lookup
                if (args.Targets.Length == 0)
                {
                    changes = args.World.BlockDB.Lookup(args.CountLimit);
                }
                else
                {
                    changes = args.World.BlockDB.Lookup(args.CountLimit, args.Targets, args.Not);
                }
                if (changes.Length > 0)
                {
                    Logger.Log(LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name);
                    args.Player.Confirm(BlockDBUndoConfirmCallback, args,
                                         "Undo last {0} changes made by {1}&S?",
                                         changes.Length, targetList);
                }

            }
            else
            {
                // time-limited lookup
                if (args.Targets.Length == 0)
                {
                    changes = args.World.BlockDB.Lookup(Int32.MaxValue, args.AgeLimit);
                }
                else
                {
                    changes = args.World.BlockDB.Lookup(Int32.MaxValue, args.Targets, args.Not, args.AgeLimit);
                }
                if (changes.Length > 0)
                {
                    Logger.Log(LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name);
                    args.Player.Confirm(BlockDBUndoConfirmCallback, args,
                                         "Undo changes ({0}) made by {1}&S in the last {2}?",
                                         changes.Length, targetList, args.AgeLimit.ToMiniString());
                }
            }

            // stop if there's nothing to undo
            if (changes.Length == 0)
            {
                args.Player.Message("{0}: Found nothing to undo.", cmdName);
            }
            else
            {
                args.Entries = changes;
            }
        }

        #endregion



        static readonly CommandDescriptor CdStatic = new CommandDescriptor {
            Name = "Static",
            Category = CommandCategory.Building,
            Help = "&SToggles repetition of last selection on or off.",
            Handler = StaticHandler
        };

        static void StaticHandler( Player player, Command cmd ) {
            if( cmd.HasNext ) {
                CdStatic.PrintUsage( player );
                return;
            }
            if( player.IsRepeatingSelection ) {
                player.Message( "Static: Off" );
                player.IsRepeatingSelection = false;
                player.SelectionCancel();
            } else {
                player.Message( "Static: On" );
                player.IsRepeatingSelection = true;
            }
        }
    }
}

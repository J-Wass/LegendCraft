/* Copyright (c) <2014> <LeChosenOne, DingusBungus>
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
using System.Threading;
using System.Diagnostics;

//AStarPathFinder - A* algorithm encoded in C# (c) http://www.csharpcity.com/reusable-code/a-path-finding-library/
using DeenGames.Utils;
using DeenGames.Utils.AStarPathFinder;

namespace fCraft
{
    public class Bot
    {
        /// <summary>
        /// Name of the bot. 
        /// </summary>
        public String Name;

        /// <summary>
        /// Current world the bot is on.
        /// </summary>
        public World World;

        /// <summary>
        /// Position of bot.
        /// </summary>
        public Position Position;

        /// <summary>
        /// Entity ID of the bot (-1 = default)
        /// </summary>
        public int ID = -1;

        /// <summary>
        /// Current model of the bot
        /// </summary>
        public string Model = "humanoid";

        /// <summary>
        /// Current skin of the bot
        /// </summary>
        public string Skin = "steve";

        /// <summary>
        /// Running thread for all bots
        /// </summary>
        private SchedulerTask thread;

        //movement
        public Player followTarget = null;
        public bool isMoving = false;
        public bool isFlying = false;
        public Position OldPosition;
        public Position NewPosition;
        public Stopwatch timeCheck = new Stopwatch();
        public static readonly double speed = 5.2; //Measured frequency in blocks per second
        public static readonly double period = (1 / (speed)) * 1000; //Invert speed and multiply by 1000 to get period in milliseconds
        public bool beganMoving;
        public List<Vector3I> PositionList = new List<Vector3I>();//Vectors in this list are connected, but not neccessarily in a line

        #region Public Methods

        /// <summary>
        /// Sets a bot, as well as the bot values. Must be called before any other bot classes.
        /// </summary>
        public void setBot(String botName, World botWorld, Position pos, int entityID)
        {
            Name = botName;
            World = botWorld;
            Position = pos;
            ID = entityID;           

            thread = Scheduler.NewTask(t => NetworkLoop());
            thread.RunForever(TimeSpan.FromSeconds(0.1));//run the network loop every 0.1 seconds

            Server.Bots.Add(this);
        }

        /// <summary>
        /// Main IO loop, handles the networking of the bot.
        /// </summary>
        private void NetworkLoop()
        {
            if (isMoving)
            {
                if (followTarget != null)
                {
                    Player p = Server.FindPlayers(followTarget.Name, false)[0];
                    Logger.LogToConsole("Chaning pos to " + p.Name + " at " + p.Position.ToString());
                    NewPosition = p.Position;
                    beganMoving = false;
                }

                //move bot 1 block after every period
                if (timeCheck.ElapsedMilliseconds > period) 
                {
                    Move();
                    timeCheck.Restart();
                }
            }

            //If bot is not flying, drop down to nearest solid block
            if (!isFlying)
            {
                //Drop();
            }
        }

        /// <summary>
        /// Creates only the bot entity, not the bot data. Bot data is created from setBot.
        /// </summary>
        public void createBot()
        {
            World.Players.Send(PacketWriter.MakeAddEntity(ID, Name, new Position(Position.X, Position.Y, Position.Z, Position.R, Position.L)));
        }

        /// <summary>
        /// Teleports the bot to a specific location
        /// </summary>
        public void teleportBot(Position p)
        {
            World.Players.Send(PacketWriter.MakeTeleport(ID, p));
        }

        /// <summary>
        /// Removes the bot entity, however the bot data remains. Intended as for temp bot changes.
        /// </summary>
        public void tempRemoveBot()
        {
            Server.Players.Send(PacketWriter.MakeRemoveEntity(ID));
        }

        /// <summary>
        /// Completely removes the entity and data of the bot.
        /// </summary>
        public void removeBot()
        {
            thread.Stop();
            isMoving = false;
            Server.Bots.Remove(this);
            Server.Players.Send(PacketWriter.MakeRemoveEntity(ID));
            
        }

        /// <summary>
        /// Updates the position and world of the bot for everyone in the world, used to replace bot from the tempRemoveBot() method
        /// </summary>
        public void updateBotPosition()
        {
            World.Players.Send(PacketWriter.MakeAddEntity(ID, Name, new Position(Position.X, Position.Y, Position.Z, Position.R, Position.L)));
        }

        /// <summary>
        /// Changes the model of the bot
        /// </summary>
        public void changeBotModel(String botModel)
        {
            if (!FunCommands.validEntities.Contains(botModel))
            {
                return; //something went wrong, model does not exist
            }

            World.Players.Where(p => p.usesCPE).Send(PacketWriter.MakeChangeModel((byte)ID, botModel));
            Model = botModel;
        }

        /// <summary>
        /// Changes the skin of the bot
        /// </summary>
        public void Clone(String targetSkin)
        {
            PlayerInfo target = PlayerDB.FindPlayerInfoExact(targetSkin);
            if (target == null)
            {
                //something went wrong, player doesn't exist
            }

            World.Players.Where(p => p.usesCPE).Send(PacketWriter.MakeExtAddEntity((byte)ID, targetSkin, targetSkin));
            Skin = targetSkin;
        }       

        /// <summary>
        /// Epically explodes the bot
        /// </summary>
        public void explodeBot(Player player)//Prepare for super copy and paste
        {
            removeBot();
            Vector3I vector = new Vector3I(Position.X / 32, Position.Y / 32, Position.Z / 32); //get the position in blockcoords as integers of the bot


            //the following code block generates the centers of each explosion hub for the greater explosion
            explode(vector, 0, 1);//start the center explosion immediately,last for a second

            //all 6 faces of the explosion point
            explode(new Vector3I(vector.X + 3, vector.Y, vector.Z), 0.75, 0.5);//start the face explosions at 0.75 seconds, last for a half second
            explode(new Vector3I(vector.X - 3, vector.Y, vector.Z), 0.75, 0.5);
            explode(new Vector3I(vector.X, vector.Y + 3, vector.Z), 0.75, 0.5);
            explode(new Vector3I(vector.X, vector.Y - 3, vector.Z), 0.75, 0.5);
            explode(new Vector3I(vector.X, vector.Y, vector.Z + 3), 0.75, 0.5);
            explode(new Vector3I(vector.X, vector.Y, vector.Z - 3), 0.75, 0.5);

            //all 8 corners of the explosion point
            explode(new Vector3I(vector.X + 1, vector.Y + 1, vector.Z + 1), 0.5, 0.5);//start the corner explosions at .5 seconds, last for a half second
            explode(new Vector3I(vector.X + 1, vector.Y + 1, vector.Z - 1), 0.5, 0.5);
            explode(new Vector3I(vector.X + 1, vector.Y - 1, vector.Z + 1), 0.5, 0.5);
            explode(new Vector3I(vector.X + 1, vector.Y - 1, vector.Z - 1), 0.5, 0.5);
            explode(new Vector3I(vector.X - 1, vector.Y + 1, vector.Z + 1), 0.5, 0.5);
            explode(new Vector3I(vector.X - 1, vector.Y + 1, vector.Z - 1), 0.5, 0.5);
            explode(new Vector3I(vector.X - 1, vector.Y - 1, vector.Z + 1), 0.5, 0.5);
            explode(new Vector3I(vector.X - 1, vector.Y - 1, vector.Z - 1), 0.5, 0.5);
        }


        /// <summary>
        /// Basic information about the bot
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} on {1} at {2}, Id: {3}", Name, World, Position.ToString(), ID.ToString());
        }

        #endregion

        #region Private Methods

        #region movement

        //If vector is a fluid block, return true
        private bool ValidBlock(Vector3I vector)
        {
            switch(World.Map.GetBlock(vector))
            {
                case Block.Air:
                case Block.Water:
                case Block.StillWater:
                case Block.Lava:
                case Block.StillLava:
                case Block.RedFlower:
                case Block.YellowFlower:
                case Block.RedMushroom:
                case Block.BrownMushroom:
                case Block.Plant:
                case Block.Fire: //only epic bots can walk through fire
                case Block.Snow:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates the shortest route between two points, used in Move()
        /// </summary>
        private void Path(Vector3I targetPosition)
        {
            //create a grid with all blocks in the 2d plane
            byte[,] grid = new byte[World.Map.Width, World.Map.Length];
            PathFinder pathFinder;

            Vector3I botPos = Position.ToBlockCoords();

            //Loop through all XZ coordinates of the map
            for (int y = 0; y < World.Map.Length; y++)
            {
                for (int x = 0; x < World.Map.Width; x++)
                {
                    //Find all valid positions, set as either an available tile or blocked tile
                    if (ValidBlock(new Vector3I(x, y, botPos.Z)) && ValidBlock(new Vector3I(x, y, botPos.Z - 1)))
                    {
                        grid[x, y] = PathFinderHelper.EMPTY_TILE;
                    }
                    else
                    {
                        grid[x, y] = PathFinderHelper.BLOCKED_TILE;
                    }
                }
            }

            //set pathFinder to the grid just created, disallow diagonals
            pathFinder = new PathFinder(grid);
            pathFinder.Diagonals = false;
            pathFinder.PunishChangeDirection = false;
            

            Point botInitPoint = new Point(botPos.X, botPos.Y);
            Point botFinalPoint = new Point(NewPosition.ToBlockCoords().X, NewPosition.ToBlockCoords().Y);

            //Implement A* to determine optimal path between two spots
            List<PathFinderNode> path = new PathFinderFast(grid).FindPath(botInitPoint, botFinalPoint);

            if (path == null)
            {
                //There is no path, stop moving
                beganMoving = false;
                isMoving = false;
                return;
            }

            //Convert node to block positions
            foreach(PathFinderNode node in path)
            {
                PositionList.Add(new Vector3I(node.X, node.Y, botPos.Z));
            }

            //A* returns points in the opposite order needed, reverse in order to get proper order
            PositionList.Reverse();


        }

        /// <summary>
        /// Intended to act like gravity and pull bot down
        /// </summary>
        private void Drop()
        {
            //generate vector at block coord under the feet of the bot
            Vector3I pos = Position.ToBlockCoords();

            Vector3I under1 = new Vector3I(pos.X, pos.Y, pos.Z - 1);

            Vector3I under2 = new Vector3I(pos.X, pos.Y, pos.Z - 2);

            if (ValidBlock(under1) && ValidBlock(under2))
            {
                teleportBot(under1.ToPlayerCoords());
            }
        }

        /// <summary>
        /// Attempts to move bot between two lines, will use a straight line if possible
        /// </summary>
        private void Move()
        {
            if (!isMoving)
            {
                return;
            }

            //Create vector list of positions
            if (!beganMoving)
            {
                beganMoving = true;

                //create an IEnumerable list of all blocks that will be in the path between blocks
                IEnumerable<Vector3I> positions = fCraft.Drawing.LineDrawOperation.LineEnumerator(Position.ToBlockCoords(), NewPosition.ToBlockCoords());

                //edit IEnumarable into List
                foreach(Vector3I v in positions)
                {
                    PositionList.Add(v);
                }

                bool clear = true;

                //Make sure that each block in the line is a fluid
                foreach (Vector3I vect in PositionList)
                {
                    Vector3I underVect = new Vector3I(vect.X, vect.Y, vect.Z - 1);

                    if (!ValidBlock(vect) || !ValidBlock(underVect))
                    {
                        clear = false;
                    }
                }

                //if list contains blocks that aren't air blocks, bot cannot make a direct line to target, need a path
                if (!clear)
                {
                    PositionList.Clear();
                    Path(NewPosition.ToBlockCoords());
                }
            }

            //once the posList is empty, we have reached the final point
            if (PositionList.Count() == 0 || Position == NewPosition)
            {
                isMoving = false;
                beganMoving = false;
                return;
            }       

            //determine distance from the target player to the target bot
            double groundDistance = Math.Sqrt(Math.Pow((NewPosition.X - OldPosition.X),2) + Math.Pow((NewPosition.Y - OldPosition.Y),2));

            int xDisplacement = NewPosition.X - Position.X;
            int yDisplacement = NewPosition.Y - Position.Y;
            int zDisplacement = NewPosition.Z - Position.Z;

            //use arctan to find appropriate angles (doesn't work yet)
            double rAngle = Math.Atan((double)zDisplacement / groundDistance);//pitch
            double lAngle = Math.Atan((double)xDisplacement / yDisplacement);//yaw
        
            //create a new position with the next pos list in the posList, then remove that pos
            Position targetPosition = new Position
            {
                X = (short)(PositionList.First().X * 32 + 16),
                Y = (short)(PositionList.First().Y * 32 + 16),
                Z = (short)(PositionList.First().Z * 32 + 16),
                R = (byte)(rAngle),
                L = (byte)(lAngle)
            };

            //Remove the position the bot just went to in the list, teleport to that created position, change position to created position
            PositionList.Remove(PositionList.First());
            Position = targetPosition;
            teleportBot(targetPosition);                       
        }
 

        #endregion

        /// <summary>
        /// Emulates a small explosion at a specific location
        /// </summary>
        private void explode(Vector3I center, double delay, double length)
        {
            Scheduler.NewTask(t => updateBlock(Block.Lava, center, true, length)).RunManual(TimeSpan.FromSeconds(delay));

            Random rand1 = new Random((int)DateTime.Now.Ticks);
            Random rand2 = new Random((int)DateTime.Now.Ticks + 1);
            Random rand3 = new Random((int)DateTime.Now.Ticks + 2); 
            Random rand4 = new Random((int)DateTime.Now.Ticks + 3);
            Random rand5 = new Random((int)DateTime.Now.Ticks + 4);
            Random rand6 = new Random((int)DateTime.Now.Ticks + 5);

            //The code block generates a lava block from 0 to 3 block spaces, randomly away from the center block

            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X, center.Y, center.Z), true, length)).RunManual(TimeSpan.FromSeconds(delay));
            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X + rand1.Next(0, 3), center.Y, center.Z), true, length)).RunManual(TimeSpan.FromSeconds(delay));
            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X - rand2.Next(0, 3), center.Y, center.Z), true, length)).RunManual(TimeSpan.FromSeconds(delay));
            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X, center.Y + rand3.Next(0, 3), center.Z), true, length)).RunManual(TimeSpan.FromSeconds(delay));
            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X, center.Y - rand4.Next(0, 3), center.Z), true, length)).RunManual(TimeSpan.FromSeconds(delay));
            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X, center.Y, center.Z + rand5.Next(0, 3)), true, length)).RunManual(TimeSpan.FromSeconds(delay));
            Scheduler.NewTask(t => updateBlock(Block.Lava, new Vector3I(center.X, center.Y, center.Z - rand6.Next(0, 3)), true, length)).RunManual(TimeSpan.FromSeconds(delay));

        }

        /// <summary>
        /// Updates a specific block for a given time
        /// </summary>
        private void updateBlock(Block blockType, Vector3I blockPosition, bool replaceWithAir, double time)//I left this class rather generic incase i used it for anything else
        {
            BlockUpdate update = new BlockUpdate(null, blockPosition, blockType);
            foreach (Player p in World.Players)
            {
                p.World.Map.QueueUpdate(update);
            }
            
            if (replaceWithAir)
            {
                Scheduler.NewTask(t => updateBlock(Block.Air, blockPosition, false, time)).RunManual(TimeSpan.FromSeconds(time));//place a block, replace it with air once 'time' is up
            }

        }

        #endregion

    }
}
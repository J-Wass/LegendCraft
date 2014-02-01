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
        /// Determines whether the bot should be moving
        /// </summary>
        public bool isMoving = false;

        /// <summary>
        /// Running thread for all bots
        /// </summary>
        private SchedulerTask thread;

        //movement

        public Position OldPosition;
        public Position NewPosition;
        public DateTime sinceLastMove = DateTime.MinValue;
        public readonly double speed = 4.3;//entities move at 4.3 blocks per second

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
            thread.RunForever(TimeSpan.FromSeconds(2));//run the network loop every 2 seconds

            Server.Bots.Add(this);
        }

        /// <summary>
        /// Handles the networking of the bot. Is always running while the bot is on the server.
        /// </summary>
        private void NetworkLoop()
        {
            if (isMoving)
            {
                Move();
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
            World.Players.Send(PacketWriter.MakeRemoveEntity(ID));
        }

        /// <summary>
        /// Completely removes the entity and data of the bot.
        /// </summary>
        public void removeBot()
        {
            thread.Stop();
            isMoving = false;
            Server.Bots.Remove(this);
            World.Players.Send(PacketWriter.MakeRemoveEntity(ID));
            
        }

        /// <summary>
        /// Updates the position and world of the bot for everyone in the world, used to replace the tempRemoveBot() method
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

            World.Players.Send(PacketWriter.MakeChangeModel((byte)ID, botModel));
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

            World.Players.Send(PacketWriter.MakeExtAddEntity((byte)ID, targetSkin, targetSkin));
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
        #endregion

        #region Private Methods

        /// <summary>
        /// Called from NetworkLoop. Bot will gradually move to a position
        /// </summary>
        private void Move()
        {
            double speed = 4.3;//player walks at around 4.3 blocks per second

            if (NewPosition == Position)
            {
                isMoving = false;
                return;
            }

            if (sinceLastMove == DateTime.MinValue)//first time the bot has moved
            {
                sinceLastMove = DateTime.Now;
            }
            else
            {
                double time = (DateTime.Now - sinceLastMove).TotalSeconds;//if bot has moved before, wait till 1s has passed to move again
                if (time < 1)
                {
                    return;
                }
                sinceLastMove = DateTime.Now;
            }
            //past this point, the bot has not moved for 1 second (or has just begun to move) and is ready to move again

            double distance = Math.Sqrt(Math.Pow((NewPosition.X - OldPosition.X),2) + Math.Pow((NewPosition.Y - OldPosition.Y),2));//find the distance between both points
            double intervals = distance / speed; //find how many packets must be sent

            double intervalX = (NewPosition.X - OldPosition.X) / intervals; //determine how much of each interval should change for each packet sending
            double intervalY = (NewPosition.Y - OldPosition.Y) / intervals; 

            Position delta = new Position //using the new intervals, create a packet to move the bot
            {
                X = (short)(intervalX),
                Y = (short)(intervalY),
                Z = (short)(Position.Z),
                R = (byte)(NewPosition.R),
                L = (byte)(NewPosition.L)
            };

            World.Players.Send(PacketWriter.MakeTeleport(ID, delta));
            Position = delta; //update the position of the bot while it moves
          
        }

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
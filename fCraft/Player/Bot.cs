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

namespace fCraft
{
    public class Bot
    {
        /// <summary>
        /// Name of the bot. 
        /// </summary>
        public String name;

        /// <summary>
        /// Current world the bot is on.
        /// </summary>
        public World world;

        /// <summary>
        /// Position of bot.
        /// </summary>
        public Position position;

        /// <summary>
        /// Entity ID of the bot
        /// </summary>
        public int ID;

        /// <summary>
        /// Sets a bot. Must be called before any other bot classes.
        /// </summary>
        public void setBot(String botName, World botWorld, Position pos, int entityID)
        {
            name = botName;
            world = botWorld;
            position = pos;
            ID = entityID;
        }

        /// <summary>
        /// Creates a bot into the server.
        /// </summary>
        public void createBot()
        {
            Server.Bots.Add(this);
            world.Players.Send(PacketWriter.MakeAddEntity(ID, name, new Position(position.X, position.Y, position.Z, position.R, position.L)));
        }

        /// <summary>
        /// Removes the bot entity, however the bot data remains. Intended as for temp bot changes.
        /// </summary>
        public void tempRemoveBot()
        {
            Server.Bots.Remove(this);
            world.Players.Send(PacketWriter.MakeRemoveEntity(ID));
        }

        /// <summary>
        /// Completely removes the entity and data of the bot.
        /// </summary>
        public void removeBot()
        {
            Server.Bots.Remove(this);
            world.Players.Send(PacketWriter.MakeRemoveEntity(ID));
        }

        /// <summary>
        /// Updates the position and world of the bot for everyone in the world.
        /// </summary>
        public void updateBotPosition()
        {
            world.Players.Send(PacketWriter.MakeAddEntity(ID, name, new Position(position.X, position.Y, position.Z, position.R, position.L)));
        }

    }
}
//based off of the 800Craft bot code
//Using this as a class simply to call packets and store logic

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public sealed partial class Player
    {
        public string botName; //name of the bot
        public Position Pos; //bots current position
        public int ID; //ID of the bot, should not change
        public World world; //world the bot is on
        
        private bool _isMoving; //if the bot can move, can be changed if it is not boxed in ect
        private Position nextPos;
        private Position oldPos;
        int xInterval;
        int yInterval;
        Position targetPos;
        SchedulerTask task;

        public void Bot(string name, Position pos, int iD, World world_)
        {
            botName = name;
            Pos = pos;
            ID = iD;
            world = world_;
            _isMoving = true;
            SetBot();
            //Scheduler.NewBackgroundTask(t => StartNewAIMovement()).RunForever(TimeSpan.FromMilliseconds(500));
        }

        public Map BotWorldMap
        {
            get
            {
                World world_ = world;
                return world_.LoadMap();
            }
        }
        // Creates the bot. Need to add a list so bots can be shown to people 
        // reloading the world / joined after it was created
        public void SetBot()
        {
            world.Players.Send(PacketWriter.MakeAddEntity(this.ID, Color.Gray + this.botName, new Position(Pos.X, Pos.Y, Pos.Z, Pos.R, Pos.L)));
        }

        public void ChangeBot(string model)
        {
            world.Players.Send(PacketWriter.MakeChangeModel((byte)this.ID, model));
        }

        public void RemoveBot()
        {
            this.world.Players.Send(PacketWriter.MakeRemoveEntity(this.ID));
        }

        public static void TeleportBot(int ID, Position pos, World world)
        {
            Packet packet = PacketWriter.MakeTeleport(ID, new Position { X = pos.X, Y = pos.Y, Z = pos.Z });
            world.Players.Send(packet);
        }

        public void MoveTo(Position newPos) 
        {
            int deltaX = newPos.X - Pos.X; //find the X distance
            int deltaY = newPos.Y - Pos.Y; //find the Y distance
            targetPos = newPos;

            //point is wider than long
            if (deltaX > deltaY)
            {
                if (deltaX < 0)
                {
                    xInterval =  -1;
                }
                else if (deltaX > 0)
                {
                    xInterval = 1;
                }
                else
                {
                    xInterval = 0;
                }

                if (deltaY < 0)
                {
                    yInterval = -1 * ((1 / deltaX) * deltaY);
                }
                else if (deltaY > 0)
                {
                    yInterval = ((1 / deltaX) * deltaY);
                }
                else
                {
                    yInterval = 0;
                }
            }
            
            //point is longer than wide
            else if (deltaX < deltaY)
            {
                if (deltaX < 0)
                {
                    xInterval = -1 * ((1 / deltaY) * deltaX);
                }
                else if (deltaX > 0)
                {
                    xInterval = ((1 / deltaY) * deltaX);
                }
                else
                {
                    xInterval = 0;
                }

                if (deltaY < 0)
                {
                    yInterval = -1;
                }
                else if (deltaY > 0)
                {
                    yInterval = 1;
                }
                else
                {
                    yInterval = 0;
                }
            }

            //point is a perfect diagonal
            else
            {
                if (deltaX < 0)
                {
                    xInterval = -1;
                }
                else if (deltaX > 0)
                {
                    xInterval = 1;
                }
                else
                {
                    xInterval = 0;
                }

                if (deltaY < 0)
                {
                    yInterval = -1;
                }
                else if (deltaY > 0)
                {
                    yInterval = 1;
                }
                else
                {
                    yInterval = 0;
                }
            }

            task = Scheduler.NewBackgroundTask(t => BeginMove(ID, xInterval, yInterval));
            task.RunForever(TimeSpan.FromMilliseconds(500));
            

        }

        public void BeginMove(int ID, int x, int y)
        {
            if (Pos == targetPos)
            {
                task.Stop();
            }
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position( Pos.X + (short)x, Pos.Y + (short)y, Pos.Z));
            world.Players.Send(packet);
        }

        public void MoveBot()
        {
            Position oldPos = Pos; //curent pos
            Position delta = new Position(
                (short)(nextPos.X - oldPos.X),
                (short)(nextPos.Y - oldPos.Y),
                (short)(nextPos.Z - oldPos.Z));

            //set the packet
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = Pos.R,
                L = 0
            });
            //send packet to everyone in the world
            if (nextPos == oldPos && oldPos != null)
            {
                world.Players.Send(PacketWriter.MakeTeleport(ID, new Position(world.Map.Spawn.X, world.Map.Spawn.Y, world.Map.Spawn.Z)));
                Pos = new Position(world.Map.Spawn.X, world.Map.Spawn.Y, world.Map.Spawn.Z, Pos.R, Pos.L);
            }
            else
            {
                world.Players.Send(packet);
                Pos = nextPos;
            }
            //world.Players.Message(Pos.ToBlockCoords().ToString());
        }

        public bool CheckVelocity(Block block)
        {
            if (block == Block.Air)
            {
                Pos.Z -= 32;
                world.Players.Send(PacketWriter.MakeMove(ID, Pos));
                return true;
            }
            return false;
        }
        public void CheckIfCanMove()
        {
            double ksi = 2.0 * Math.PI * (-Pos.L) / 256.0;
            double phi = 2.0 * Math.PI * (Pos.R - 64) / 256.0;
            double sphi = Math.Sin(phi);
            double cphi = Math.Cos(phi);
            double sksi = Math.Sin(ksi);
            double cksi = Math.Cos(ksi);
            Position movePos;
            Vector3I BlockPos = new Vector3I((int)(cphi * cksi * 2 - sphi * (0.5 + 1) - cphi * sksi * (0.5 + 1)),
                                                                                  (int)(sphi * cksi * 2 + cphi * (0.5 + 1) - sphi * sksi * (0.5 + 1)),
                                                                                  (int)(sksi * 2 + cksi * (0.5 + 1)));
            BlockPos += Pos.ToBlockCoords();
            movePos = new Position((short)(BlockPos.X * 32), (short)(BlockPos.Y * 32), (short)(BlockPos.Z * 32), Pos.R, Pos.L);
            oldPos = movePos;
            switch (world.Map.GetBlock(BlockPos.X, BlockPos.Y, BlockPos.Z - 2))
            {
                case Block.Air:
                case Block.Water:
                case Block.Lava:
                case Block.Plant:
                case Block.RedFlower:
                case Block.RedMushroom:
                case Block.YellowFlower:
                case Block.BrownMushroom:
                    nextPos = movePos;
                    MoveBot();
                    break;
                default:
                    Pos.R -= 90;
                    world.Players.Send(PacketWriter.MakeRotate(ID, Pos));
                    break;
            }
        }

        public void StartNewAIMovement()
        {
            if (!_isMoving)
            {
                return;
            }
            CheckIfCanMove();
            int Rand = new Random().Next(1, 100);
            if (Rand > 95) MakeRandomDecision();
        }

        public void MakeRandomDecision()
        {
            int rand = new Random().Next(1, 5);
            switch (rand)
            {
                case 1:
                    Pos.R += 15;
                    world.Players.Send(PacketWriter.MakeRotate(ID, Pos));
                    break;
                case 2:
                    Pos.R += 90;
                    world.Players.Send(PacketWriter.MakeRotate(ID, Pos));
                    break;
                case 3:
                    int player = new Random().Next(0, world.Players.Count() - 1);
                    world.Players.Send(PacketWriter.MakeTeleport(ID, world.Players[player].Position));
                    Pos = world.Players[player].Position;
                    break;
                default:
                    break;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;
using System.Collections.Concurrent;

namespace fCraft
{
    public class LegendCraft
    {
        public static void testFunction(Player player)
        {
            player.Message("This is a legendcraft test function");
        }

        public static void coords(Player player)
        {
            Vector3I Blockcoords = player.Position.ToBlockCoords();
            player.Message("Your current coordinates are " + Blockcoords + ".");
        }

        //Used to extract a byte value for string
        public static byte toByteValue(string s)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(s);
            byte bytesValue = (byte)0;
            foreach (Byte b in bytes)
            {
                bytesValue = (byte)((byte)bytesValue + (byte)b);
            }
            return bytesValue;
        }

        //generates a new ID for newly created bots
        public static int getNewID()
        {
            int i = 0;
            foreach (Bot bot in Server.Bots)
            {
                if (bot.ID == i)
                {
                    i++;
                }
                else
                {
                    return i;
                }
            }
            return i;
        }

        //generates a new ID for new highlights, force a new int that is greater than the largest ID in Server.Highlights
        public static int getNewHighlightID()
        {
            int i = 0;
            foreach (Tuple<int, Vector3I, Vector3I, System.Drawing.Color, int> tuple in Server.Highlights.Values)
            {
                if (tuple.Item1 >= i)
                {
                    i = tuple.Item1 + 1;
                }
            }
            return i;
        }
    }
}


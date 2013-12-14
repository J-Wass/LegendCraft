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
    }
}


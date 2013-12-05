// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft
{

    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public partial struct Packet
    {
        public readonly byte[] Data;

        public OpCode OpCode
        {
            get { return (OpCode)Data[0]; }
        }

        public Packet([NotNull] byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            Data = data;
        }

        /// <summary> Creates a packet of correct size for a given opcode,
        /// and sets the first (opcode) byte. </summary>
        public Packet(OpCode opcode)
        {
            Data = new byte[PacketSizes[(int)opcode]];
            Data[0] = (byte)opcode;
        }


        /// <summary> Returns packet size (in bytes) for a given opcode.
        /// Size includes the opcode byte itself. </summary>
        public static int GetSize(OpCode opcode)
        {
            return PacketSizes[(int)opcode];
        }


        static readonly int[] PacketSizes = {
            131,    // Handshake
            1,      // Ping
            1,      // MapBegin
            1028,   // MapChunk
            7,      // MapEnd
            9,      // SetBlockClient
            8,      // SetBlockServer
            74,     // AddEntity
            10,     // Teleport
            7,      // MoveRotate
            5,      // Move
            4,      // Rotate
            2,      // RemoveEntity
            66,     // Message
            65,     // Kick
            2,      // SetPermission


// This entire section was taken from the FemtoCraft source Copyright 2012-2013 Matvei Stefarov
// The source can be found here: http://svn.fcraft.net:8080/svn/femtocraft/
            
            67,     // ExtInfo
            69,     // ExtEntry
            0,
            2,      // CustomBlockSupportLevel
            0,0,0,0,0,
            8,      //EnvSetColor, (this is 25 right?)
            0,
            0,
            4       // SetBlockPermission
        };
    }
}
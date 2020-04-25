// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// This entire file was taken from the FemtoCraft source Copyright 2012-2013 Matvei Stefarov
// The source can be found here: http://svn.fcraft.net:8080/svn/femtocraft/

using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft
{
    sealed class PacketReader : BinaryReader
    {
        public PacketReader([NotNull] Stream stream) :
            base(stream) { }


        public OpCode ReadOpCode()
        {
            return (OpCode)ReadByte();
        }


        public override short ReadInt16()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt16());
        }


        public override int ReadInt32()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt32());
        }

        char[] characters = new char[64];
        public override string ReadString()
        {
            int length = 0;
            byte[] data = ReadBytes(64);
            
            for (int i = 63; i >= 0; i--)
            {
                byte code = data[i];
                if (code == 0) code = 0x20; // NULL to space
                if (length == 0 && code != 0x20) { length = i + 1; }
                
                // Treat code as an index in code page 437
                if (code < 0x20) {
                    characters[i] = Chat.ControlCharReplacements[code];
                } else if (code < 0x7F) {
                    characters[i] = (char)code;
                } else {
                    characters[i] = Chat.ExtendedCharReplacements[code - 0x7F];
                }
            }
            return new string( characters, 0, length );
        }
    }
}
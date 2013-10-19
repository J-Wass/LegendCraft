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


        public override string ReadString()
        {
            return Encoding.ASCII.GetString(ReadBytes(64)).Trim();
        }
    }
}
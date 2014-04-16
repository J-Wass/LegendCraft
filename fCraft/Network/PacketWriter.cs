// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    // Protocol encoder for outgoing packets
    public sealed class PacketWriter : BinaryWriter {

        public PacketWriter( Stream stream ) : base( stream ) { }


        #region Direct Writing

        public void Write( OpCode opcode ) {
            Write( (byte)opcode );
        }

        /// <summary>  Writes a 16-bit short integer in Big-Endian order. </summary>
        public override void Write( short data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        /// <summary>  Writes a 32-bit integer in Big-Endian order. </summary>
        public override void Write( int data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        /// <summary> Writes a string in Minecraft protocol format.
        /// Maximum length: 64 characters. </summary>
        public override void Write( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > 64 ) throw new ArgumentException( "String is too long (>64).", "str" );
            Write( Encoding.ASCII.GetBytes( str.PadRight( 64 ) ) );
        }

        #endregion


        #region Direct Writing Whole Packets

        public void WritePing() {
            Write( OpCode.Ping );
        }

        public void WriteMapBegin() {
            Write( OpCode.MapBegin );
        }

        public void WriteMapChunk( [NotNull] byte[] chunk, int chunkSize, byte progress ) {
            if( chunk == null ) throw new ArgumentNullException( "chunk" );
            Write( OpCode.MapChunk );
            Write( (short)chunkSize );
            Write( chunk, 0, 1024 );
            Write( progress );
        }

        internal void WriteMapEnd( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            Write( OpCode.MapEnd );
            Write( (short)map.Width );
            Write( (short)map.Height );
            Write( (short)map.Length );
        }

        public void WriteAddEntity( byte id, [NotNull] Player player, Position pos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Write( OpCode.AddEntity );
            Write( id );
            Write( player.ListName );
            Write( pos.X );
            Write( pos.Z );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        public void WriteTeleport( byte id, Position pos ) {
            Write( OpCode.Teleport );
            Write( id );
            Write( pos.X );
            Write( pos.Z );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        #endregion


        #region Packet Making

        internal static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            packet.Data[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Data, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Data, 66 );
            packet.Data[130] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }


        internal static Packet MakeMessage( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );

            Packet packet = new Packet( OpCode.Message );
            packet.Data[1] = 0; // unused
            Encoding.ASCII.GetBytes( message.PadRight( 64 ), 0, 64, packet.Data, 2 );
            return packet;
        }


        internal static Packet MakeAddEntity( int id, [NotNull] string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( OpCode.AddEntity );
            packet.Data[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Data, 2 );
            ToNetOrder( pos.X, packet.Data, 66 );
            ToNetOrder( pos.Z, packet.Data, 68 );
            ToNetOrder( pos.Y, packet.Data, 70 );
            packet.Data[72] = pos.R;
            packet.Data[73] = pos.L;
            return packet;
        }


        internal static Packet MakeDisconnect( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );

            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Data, 1 );
            return packet;
        }


        internal static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Data[1] = (byte)id;
            return packet;
        }


        internal static Packet MakeTeleport( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Data[1] = (byte)id;
            ToNetOrder( pos.X, packet.Data, 2 );
            ToNetOrder( pos.Z, packet.Data, 4 );
            ToNetOrder( pos.Y, packet.Data, 6 );
            packet.Data[8] = pos.R;
            packet.Data[9] = pos.L;
            return packet;
        }


        internal static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( 255, pos.GetFixed() );
        }


        internal static Packet MakeMoveRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)(pos.X & 0xFF);
            packet.Data[3] = (byte)(pos.Z & 0xFF);
            packet.Data[4] = (byte)(pos.Y & 0xFF);
            packet.Data[5] = pos.R;
            packet.Data[6] = pos.L;
            return packet;
        }


        internal static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)pos.X;
            packet.Data[3] = (byte)pos.Z;
            packet.Data[4] = (byte)pos.Y;
            return packet;
        }


        internal static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Data[1] = (byte)id;
            packet.Data[2] = pos.R;
            packet.Data[3] = pos.L;
            return packet;
        }


        public static Packet MakeSetBlock( int x, int y, int z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Data, 1 );
            ToNetOrder( z, packet.Data, 3 );
            ToNetOrder( y, packet.Data, 5 );
            packet.Data[7] = (byte)type;
            return packet;
        }


        internal static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( coords.X, packet.Data, 1 );
            ToNetOrder( coords.Z, packet.Data, 3 );
            ToNetOrder( coords.Y, packet.Data, 5 );
            packet.Data[7] = (byte)type;
            return packet;
        }
        
        //Tested and works bby
        /// <param name="distance"> Default range is 160 (5 blocks). Multiply number of blocks of reach by 32.</param>
        public static Packet MakeSetClickDistance(int distance)
        {
            Packet packet = new Packet(OpCode.SetClickDistance);
            ToNetOrder((short)distance, packet.Data, 1);
            return packet;
        }
        
        /// <summary> Be sure to make sure the block is not undefined! If undefined, change to (byte)0 </summary>
        public static Packet MakeHoldThis(byte BlockToHold, bool preventChange)
        {
            Packet packet = new Packet(OpCode.HoldThis);
            packet.Data[1] = BlockToHold;
            packet.Data[2] = preventChange ? (byte)1 : (byte)0;
            return packet;
        }

        /// <param name="Label"> Name of hotkey shortcut </param>
        /// <param name="Action"> Action needing to be completed with hotkey use </param>
        /// <param name="KeyCode"> Keycode, refer here: http://minecraft.gamepedia.com/Key_Codes </param>
        /// <param name="KeyMods"> 0 - None, 1 - Ctrl, 2 - Shift, 3 - Alt </param>
        public static Packet MakeSetTextHotKey(string Label, string Action, int KeyCode, byte KeyMods)
        {
            Packet packet = new Packet(OpCode.SetTextHotKey);
            Encoding.ASCII.GetBytes(Label.PadRight(64), 0, 64, packet.Data, 1);
            Encoding.ASCII.GetBytes(Action.PadRight(64), 0, 64, packet.Data, 65);
            ToNetOrder(KeyCode, packet.Data, 129);
            packet.Data[133] = (byte)KeyMods;
            return packet;
        }

        /// <summary> Packet used to change players names/group in TabList as well as their autocomplete name. Color code friendly. </summary>
        /// <param name="NameID"> Name ID number from 0-255 </param>
        /// <param name="PlayerName"> Name used for autocompletion (can be null) </param>
        /// <param name="ListName"> Name displayed in Tab List </param>
        /// <param name="GroupName"> Name of group in Tab List </param>
        /// <param name="GroupRank"> Rank of group in Tab list (0 is highest) </param>
        public static Packet MakeExtAddPlayerName(short NameID, [CanBeNull]string PlayerName, string ListName, string GroupName, byte GroupRank)
        {
            Packet packet = new Packet(OpCode.ExtAddPlayerName); //0
            ToNetOrder((short)NameID, packet.Data, 1); //1
            Encoding.ASCII.GetBytes(PlayerName.PadRight(64), 0, 64, packet.Data, 3);  //2
            Encoding.ASCII.GetBytes(ListName.PadRight(64), 0, 64, packet.Data, 67); //67 
            Encoding.ASCII.GetBytes(GroupName.PadRight(64), 0, 64, packet.Data, 131); //131
            packet.Data[195] = (byte)GroupRank;
            return packet;
        }

        public static Packet MakeExtRemovePlayerName(short NameID)
        {
            Packet packet = new Packet(OpCode.ExtRemovePlayerName);
            ToNetOrder((short)NameID, packet.Data, 1);
            return packet;
        }

        public static Packet MakeExtAddEntity(byte EntityID, string entityName, string skinName)
        {
            Packet packet = new Packet(OpCode.ExtAddEntity);
            packet.Data[1] = EntityID;
            Encoding.ASCII.GetBytes(entityName.PadRight(64), 0, 64, packet.Data, 2);
            Encoding.ASCII.GetBytes(skinName.PadRight(64), 0, 64, packet.Data, 66);
            return packet;
        }

        public static Packet MakeEnvSetColor(byte selection, string colorcode)
        {
            System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(colorcode.ToUpper());
            Packet packet = new Packet(OpCode.EnvSetColor);
            packet.Data[1] = selection;
            ToNetOrder((short)(col.R), packet.Data, 2);
            ToNetOrder((short)(col.G), packet.Data, 4);
            ToNetOrder((short)(col.B), packet.Data, 6);
            return packet;
        }
        
        //Client does not support atm, test later when supported!!!
        /// <param name="weatherType"> 0 - Clear, 1 - Rain, 2 - Snow </param>
        public static Packet MakeEnvWeatherAppearance(byte weatherType)
        {
            Packet packet = new Packet(OpCode.EnvSetWeatherAppearance);
            packet.Data[1] = (byte)weatherType;
            return packet;
        }

        internal static Packet MakeSetPermission( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            packet.Data[1] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }

        public static Packet MakeChangeModel(byte EntityID, string modelName)
        {
            Packet packet = new Packet(OpCode.ChangeModel);
            packet.Data[1] = EntityID;
            Encoding.ASCII.GetBytes(modelName.PadRight(64), 0, 64, packet.Data, 2);
            return packet;
        }

        public static Packet MakeEnvSetMapAppearance(string textureURL, byte sideBlock, byte edgeBlock, short sideLevel)
        {
            Packet packet = new Packet(OpCode.EnvSetMapAppearance);
            Encoding.ASCII.GetBytes(textureURL.PadRight(64), 0, 64, packet.Data, 1);
            packet.Data[65] = sideBlock;
            packet.Data[66] = edgeBlock;
            ToNetOrder((short)sideLevel, packet.Data, 67);
            return packet;
        }
        
        public static Packet MakeSetBlockPermissions(byte BlockType, bool AllowPlacement, bool AllowDeletion)
        {
            Packet packet = new Packet(OpCode.SetBlockPermissions);
            packet.Data[1] = BlockType;
            packet.Data[2] = (AllowPlacement ? (byte)1 : (byte)0);
            packet.Data[3] = (AllowDeletion ? (byte)1 : (byte)0);
            return packet;
        }

        public static Packet MakeHackControl(byte Flying, byte NoClip, byte Speeding, byte SpawnControl, byte ThirdPerson, short JumpHeight)
        {
            Packet packet = new Packet(OpCode.HackControl);
            packet.Data[1] = Flying;
            packet.Data[2] = NoClip;
            packet.Data[3] = Speeding;
            packet.Data[4] = SpawnControl;
            packet.Data[5] = ThirdPerson;
            ToNetOrder(JumpHeight, packet.Data, 6);
            return packet;
        }

        //used for message types
        internal static Packet MakeSpecialMessage(byte messageType, [NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");

            Packet packet = new Packet(OpCode.Message);
            packet.Data[1] = messageType;
            Encoding.ASCII.GetBytes(message.PadRight(64), 0, 64, packet.Data, 2);
            return packet;
        }

        internal static Packet MakeSelectionCuboid(byte ID, string label, Vector3I start, Vector3I end, System.Drawing.Color color, int percentOpacity)
        {
            double opacity = Math.Round(255 * ((double)percentOpacity * 0.01), 1); //apply percent opaque to total opaque, create a whole number

            Packet packet = new Packet(OpCode.SelectionCuboid);
            packet.Data[1] = ID;
            Encoding.ASCII.GetBytes(label.PadRight(64), 0, 64, packet.Data, 2);
            ToNetOrder(start.X, packet.Data, 66);
            ToNetOrder(start.Z, packet.Data, 68);
            ToNetOrder(start.Y, packet.Data, 70);
            ToNetOrder(end.X, packet.Data, 72);
            ToNetOrder(end.Z, packet.Data, 74);
            ToNetOrder(end.Y, packet.Data, 76);
            ToNetOrder(color.R, packet.Data, 78);
            ToNetOrder(color.G, packet.Data, 80);
            ToNetOrder(color.B, packet.Data, 82);
            ToNetOrder((int)opacity, packet.Data, 84);
            return packet;
        }

        //easiest packet I ever made
        internal static Packet RemoveSelectionCuboid(byte ID)
        {
            Packet packet = new Packet(OpCode.RemoveSelectionCuboid);
            packet.Data[1] = ID;
            return packet;
        }

        #endregion


        internal static void ToNetOrder( int number, byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0xff00) >> 8);
            arr[offset + 1] = (byte)(number & 0x00ff);
        }
    }
}

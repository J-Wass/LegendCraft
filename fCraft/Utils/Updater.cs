// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Checks for updates, and keeps track of current version/revision. </summary>
    public static class Updater {

        public static readonly ReleaseInfo CurrentRelease = new ReleaseInfo(
            206,
            151,
            new DateTime( 2012, 07, 19, 1, 0, 0, DateTimeKind.Utc ),
            "", "",
            ReleaseFlags.Feature
#if DEBUG
            | ReleaseFlags.Dev
#endif
 );

        public static string UserAgent {
            get { return "LegendCraft " + LatestStable; }
        }

        public const string LatestStable = "2.3.0";

        public static string UpdateUrl { get; set; }

        public static bool RunAtShutdown { get; set; }
    }


    public sealed class ReleaseInfo {
        internal ReleaseInfo( int version, int revision, DateTime releaseDate,
                              string summary, string changeLog, ReleaseFlags releaseType ) {
            Version = version;
            Revision = revision;
            Date = releaseDate;
            Summary = summary;
            ChangeLog = changeLog.Split( new[] { '\n' } );
            Flags = releaseType;
        }

        public ReleaseFlags Flags { get; private set; }

        public string FlagsString { get { return ReleaseFlagsToString( Flags ); } }

        public string[] FlagsList { get { return ReleaseFlagsToStringArray( Flags ); } }

        public int Version { get; private set; }

        public int Revision { get; private set; }

        public DateTime Date { get; private set; }

        public TimeSpan Age {
            get {
                return DateTime.UtcNow.Subtract( Date );
            }
        }

        public string Summary { get; private set; }

        public string[] ChangeLog { get; private set; }

        public static ReleaseFlags StringToReleaseFlags( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            ReleaseFlags flags = ReleaseFlags.None;
            for( int i = 0; i < str.Length; i++ ) {
                switch( Char.ToUpper( str[i] ) ) {
                    case 'A':
                        flags |= ReleaseFlags.APIChange;
                        break;
                    case 'B':
                        flags |= ReleaseFlags.Bugfix;
                        break;
                    case 'C':
                        flags |= ReleaseFlags.ConfigFormatChange;
                        break;
                    case 'D':
                        flags |= ReleaseFlags.Dev;
                        break;
                    case 'F':
                        flags |= ReleaseFlags.Feature;
                        break;
                    case 'M':
                        flags |= ReleaseFlags.MapFormatChange;
                        break;
                    case 'P':
                        flags |= ReleaseFlags.PlayerDBFormatChange;
                        break;
                    case 'S':
                        flags |= ReleaseFlags.Security;
                        break;
                    case 'U':
                        flags |= ReleaseFlags.Unstable;
                        break;
                    case 'O':
                        flags |= ReleaseFlags.Optimized;
                        break;
                }
            }
            return flags;
        }

        public static string ReleaseFlagsToString( ReleaseFlags flags ) {
            StringBuilder sb = new StringBuilder();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) sb.Append( 'A' );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) sb.Append( 'B' );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) sb.Append( 'C' );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) sb.Append( 'D' );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) sb.Append( 'F' );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) sb.Append( 'M' );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) sb.Append( 'P' );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) sb.Append( 'S' );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) sb.Append( 'U' );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) sb.Append( 'O' );
            return sb.ToString();
        }

        public static string[] ReleaseFlagsToStringArray( ReleaseFlags flags ) {
            List<string> list = new List<string>();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) list.Add( "API Changes" );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) list.Add( "Fixes" );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) list.Add( "Config Changes" );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) list.Add( "Developer" );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) list.Add( "New Features" );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) list.Add( "Map Format Changes" );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) list.Add( "PlayerDB Changes" );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) list.Add( "Security Patch" );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) list.Add( "Unstable" );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) list.Add( "Optimized" );
            return list.ToArray();
        }

        public bool IsFlagged( ReleaseFlags flag ) {
            return (Flags & flag) == flag;
        }
    }


    #region Enums

    /// <summary> Updater behavior. </summary>
    public enum UpdaterMode {
        /// <summary> Does not check for updates. </summary>
        Disabled = 0,

        /// <summary> Checks for updates and notifies of availability (in console/log). </summary>
        Notify = 1,

        /// <summary> Checks for updates, downloads them if available, and prompts to install.
        /// Behavior is frontend-specific: in ServerGUI, a dialog is shown with the list of changes and
        /// options to update immediately or next time. In ServerCLI, asks to type in 'y' to confirm updating
        /// or press any other key to skip. '''Note: Requires user interaction
        /// (if you restart the server remotely while unattended, it may get stuck on this dialog).''' </summary>
        Prompt = 2,

        /// <summary> Checks for updates, automatically downloads and installs the updates, and restarts the server. </summary>
        Auto = 3,
    }


    /// <summary> A list of release flags/attributes.
    /// Use binary flag logic (value & flag == flag) or Release.IsFlagged() to test for flags. </summary>
    [Flags]
    public enum ReleaseFlags {
        None = 0,

        /// <summary> The API was notably changed in this release. </summary>
        APIChange = 1,

        /// <summary> Bugs were fixed in this release. </summary>
        Bugfix = 2,

        /// <summary> Config.xml format was changed (and version was incremented) in this release. </summary>
        ConfigFormatChange = 4,

        /// <summary> This is a developer-only release, not to be used on live servers.
        /// Untested/undertested releases are often marked as such. </summary>
        Dev = 8,

        /// <summary> A notable new feature was added in this release. </summary>
        Feature = 16,

        /// <summary> The map format was changed in this release (rare). </summary>
        MapFormatChange = 32,

        /// <summary> The PlayerDB format was changed in this release. </summary>
        PlayerDBFormatChange = 64,

        /// <summary> A security issue was addressed in this release. </summary>
        Security = 128,

        /// <summary> There are known or likely stability issues in this release. </summary>
        Unstable = 256,

        /// <summary> This release contains notable optimizations. </summary>
        Optimized = 512
    }

    #endregion
}


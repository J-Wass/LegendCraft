/*
 *  Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */

//Modified LegendCraft Team

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Net;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using fCraft.Events;

namespace fCraft.ServerCLI {

    static class Program {
        static bool useColor = true;

        static void Main( string[] args ) {
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;

            Console.Title = "LegendCraft " + fCraft.Updater.LatestStable + " - starting...";

#if !DEBUG
            try {
#endif
                Server.InitLibrary( args );
                useColor = !Server.HasArg( ArgKey.NoConsoleColor );

                Server.InitServer();

                if (ConfigKey.CheckForUpdates.GetString() == "True")
                {
                    CheckForUpdates();
                }
                Console.Title = "LegendCraft " + Updater.LatestStable + " - " + ConfigKey.ServerName.GetString();

                if( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch( Exception ) {
                        Logger.Log( LogType.Warning, "Program.Main: Could not set process priority, using defaults." );
                    }
                }

                if( Server.StartServer() ) {
                    Console.WriteLine( "** Running LegendCraft version {0} **", Updater.LatestStable );
                    Console.WriteLine( "** Server is now ready. Type /Shutdown to exit safely. **" );

                    while( !Server.IsShuttingDown ) {
                        string cmd = Console.ReadLine();
                        if( cmd.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                            Console.Clear();
                        } else {
                            try {
                                Player.Console.ParseMessage( cmd, true, false);
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "Error while executing a command from console", "ServerCLI", ex, false );
                            }
                        }
                    }

                } else {
                    ReportFailure( ShutdownReason.FailedToStart );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerCLI", "ServerCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            } finally {
                Console.ResetColor();
            }
#endif
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "LegendCraft {0} {1}", Updater.LatestStable, reason );
            if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( useColor ) Console.ResetColor();
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false, false ), true );
            if( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                Console.ReadLine();
            }
        }


        [DebuggerStepThrough]
        static void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            switch( e.MessageType ) {
                case LogType.Error:
                    if(useColor)Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.White;
                    if( useColor ) Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Warning:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Debug:
                case LogType.Trace:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }


        static void OnHeartbeatUriChanged( object sender, UriChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUri.ToString(), Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", e.NewUri );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }


        #region Updates


        static readonly AutoResetEvent UpdateDownloadWaiter = new AutoResetEvent( false );

        static readonly object progressReportLock = new object();
        static void OnUpdateDownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            lock( progressReportLock ) {
                Console.CursorLeft = 0;
                int maxProgress = Console.WindowWidth - 9;
                int progress = (int)Math.Round((e.ProgressPercentage / 100f) * (maxProgress - 1));
                Console.Write( "{0,3}% |", e.ProgressPercentage );
                Console.Write( new String( '=', progress ) );
                Console.Write( '>' );
                Console.Write( new String( ' ', maxProgress - progress ) );
                Console.Write( '|' );
            }
        }



        static void CheckForUpdates()
        {
            Console.WriteLine("Checking for LegendCraft updates...");
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://raw.githubusercontent.com/LeChosenOne/LegendCraft/master/README.md");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            StreamReader streamReader = new StreamReader(stream);
                            string version = streamReader.ReadLine();

                            //update is available, prompt for a download
                            if (version != null && version != fCraft.Updater.LatestStable)
                            {

                                Console.WriteLine("Server.Run: Your LegendCraft version is out of date. A LegendCraft Update is available!");
                                Console.WriteLine("Download the latest LegendCraft version and restart the server? (Y/N)");
                                string answer = Console.ReadLine();
                                if (answer.ToLower() == "y" || answer.ToLower() == "yes" || answer.ToLower() == "yup" || answer.ToLower() == "yeah")//preparedness at its finest
                                {
                                    using (var client = new WebClient())
                                    {
                                        try
                                        {
                                            //download new zip in current directory
                                            Process.Start("http://www.legend-craft.tk/download/latest");
                                            Console.WriteLine("Downloading the latest LegendCraft Version. Please replace all the files (not folders) in your current folder with the new ones after shutting down.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Update error: " + ex);
                                        }

                                    }

                                }
                                else
                                {
                                    Console.WriteLine("Update ignored. To ignore future LegendCraft update requests, uncheck the box in configGUI.");
                                }

                            }
                            else
                            {
                                Console.WriteLine("Your LegendCraft version is up to date!");
                            }
                        }
                    }
                }
            }

            catch (WebException error)
            {
                Console.WriteLine("There was an internet connection error. Server was unable to check for updates. Error: \n\r" + error);
            }
            catch (Exception e)
            {
                Console.WriteLine("There was an error in trying to check for updates:\n\r " + e);
            }
        }


        static void RestartForUpdate() {
            string restartArgs = String.Format( "{0} --restart=\"{1}\"",
                                                Server.GetArgString(),
                                                MonoCompat.PrependMono( "ServerCLI.exe" ) );
            MonoCompat.StartDotNetProcess( Paths.UpdaterFileName, restartArgs, true );
        }

        #endregion
    }
}
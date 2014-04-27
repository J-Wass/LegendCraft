using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace AutoUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            string updateTemp = "";
            Console.WriteLine("Checking for LegendCraft Updates...");
            Thread.Sleep(1000);//wait a second for ServerCLI or ServerGUI to fully close

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://legend-craft.tk/download/latest/update");
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

                                Console.WriteLine("Your LegendCraft version is out of date. A LegendCraft Update is available!");
                                Console.WriteLine("Download the latest LegendCraft version? (Y/N)");
                                string answer = Console.ReadLine();
                                if (answer.ToLower() == "y" || answer.ToLower() == "yes" || answer.ToLower() == "yup" || answer.ToLower() == "yeah")//preparedness at its finest
                                {
                                    Console.WriteLine("");
                                    Console.WriteLine("LegendCraft is now downloading. This may take a few moments...");
                                    using (var client = new WebClient())
                                    {
                                        try
                                        {
                                            if (File.Exists(Directory.GetCurrentDirectory() + "\\UPDATE.zip"))
                                            {
                                                File.Delete(Directory.GetCurrentDirectory() + "\\UPDATE.zip");
                                            }

                                            //download new zip in current directory
                                            client.DownloadFile("http://legend-craft.tk/download/latest", "UPDATE.zip");

                                            //open zipped file, extract LegendCraft folder and move into a non-zipped folder
                                            ZipStorer zip = ZipStorer.Open(Directory.GetCurrentDirectory() + "\\UPDATE.zip", FileAccess.ReadWrite);
                                            List<ZipStorer.ZipFileEntry> zipList = zip.ReadCentralDir();

                                            //get the update folder, get parent directory only
                                            updateTemp = (zipList.First().ToString()).Split('/')[0];


                                            //loop through all files
                                            Console.WriteLine("");
                                            Console.WriteLine("Extracting server update... please wait.");
                                            foreach (ZipStorer.ZipFileEntry fileEntry in zipList)
                                            {
                                                if (zip.ExtractFile(fileEntry, fileEntry.ToString()))
                                                {
                                                    //success, we extracted the folder out, loop through the remaining files  

                                                }
                                                else
                                                {
                                                    Console.WriteLine("");
                                                    Console.WriteLine("Error with file extraction. Please update server manually from UPDATE.zip in server folder.");
                                                    goto exit;
                                                }
                                            }

                                            Console.WriteLine("");
                                            Console.WriteLine("Extract complete! Updating server files...");


                                            //change update folder from a folder to a folder with a \ before it for directory work
                                            updateTemp = "\\" + updateTemp;

                                            //set target directory to update
                                            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory() + updateTemp);
                                            Console.WriteLine("Debug: " + Directory.GetCurrentDirectory() + updateTemp);

                                            //loop through all .dll's to replace
                                            foreach (FileInfo file in dir.GetFiles(".dll"))
                                            {

                                                //check if file exists
                                                if (File.Exists(Directory.GetCurrentDirectory() + "\\" + file.Name))
                                                {
                                                    //if it exists, replace
                                                    File.Replace((dir.FullName + "\\" + file.Name), (Directory.GetCurrentDirectory() + "\\" + file.Name), (Directory.GetCurrentDirectory() + "\\BACKUP" + file.Name));
                                                    File.Delete(Directory.GetCurrentDirectory() + "\\BACKUP" + file.Name);//backups are lame
                                                }
                                                else
                                                {
                                                    //move file to server folder
                                                    File.Move((dir.FullName + "\\" + file.Name), (Directory.GetCurrentDirectory() + "\\" + file.Name));
                                                }
                                            }

                                            //loop through all .exe's to replace
                                            foreach (FileInfo file in dir.GetFiles(".exe"))
                                            {

                                                //check if file exists
                                                if (File.Exists(Directory.GetCurrentDirectory() + "\\" + file.Name))
                                                {
                                                    //if it exists, replace
                                                    File.Replace((dir.FullName + "\\" + file.Name), (Directory.GetCurrentDirectory() + "\\" + file.Name), (Directory.GetCurrentDirectory() + "\\BACKUP" + file.Name));
                                                    File.Delete(Directory.GetCurrentDirectory() + "\\BACKUP" + file.Name);//backups are lame
                                                }
                                                else
                                                {
                                                    //move file to server folder
                                                    File.Move((dir.FullName + "\\" + file.Name), (Directory.GetCurrentDirectory() + "\\" + file.Name));
                                                }
                                            }

                                            //remove the temp update folder
                                            File.Delete(Directory.GetCurrentDirectory() + updateTemp);                                        
                                            Console.WriteLine("");
                                            Console.WriteLine("Your LegendCraft version has been updated!");

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Update error: " + ex);
                                            goto exit;
                                        }

                                    }

                                }
                                else
                                {
                                    Console.WriteLine("");
                                    Console.WriteLine("Update ignored. To ignore future LegendCraft update requests, uncheck the box in configGUI.");
                                    Console.WriteLine("");
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

            exit:

            //close here
            Console.WriteLine("");
            Console.WriteLine("Press any key to finish...");
            Console.ReadKey();       
        }
    }
}

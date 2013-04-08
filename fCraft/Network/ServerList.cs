using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace fCraft.Network
{
    public class ServerList
    {
        public static void sendData()
        {
            WebRequest request = WebRequest.Create("http://legendcraft.webuda.com/index.php"); 
            request.Method = "POST";
           
            //Create data, and convert it to a byte array
            string data = "$serverData=" +
                "<td>" +
                ConfigKey.ServerName.GetString() +
                "</td><td>" +
                Server.Players.Length.ToString() +
                "</td><td>" +
                DateTime.UtcNow.Subtract(Server.StartTime).TotalHours.ToString() +
                "</td></td>" +
                "2.0.0" + 
                "</td>";                                                                                    
            byte[] byteData = Encoding.UTF8.GetBytes(data);            
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteData.Length;
            
            //Send the byte array with Stream to the webserver
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteData, 0, byteData.Length);
            dataStream.Close();

        }
        public static void sendLastData()
        {
            WebRequest request = WebRequest.Create("http://legendcraft.webuda.com/index.php") as HttpWebRequest;
            request.Method = "POST";

            //Create data, and convert it to a byte array
            string data = "$serverData=" +
                "<td>" +
                ConfigKey.ServerName.GetString() +
                "</td><td>" +
                "0" +
                "</td><td>" +
                "0" +
                "</td></td>" +
                "2.0.0" +
                "</td>";     
            byte[] byteData = Encoding.UTF8.GetBytes(data);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteData.Length;

            //Send the byte array with Stream to the webserver
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteData, 0, byteData.Length);
            dataStream.Close();

        }
    }
}

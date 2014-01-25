using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;

namespace fCraft.ServerGUI
{
    public partial class Report : Form
    {
        public Report()
        {
            InitializeComponent();
        }

        private void tEmail_MouseClick(object sender, EventArgs e)
        {
            tEmail.ForeColor = System.Drawing.Color.Black;
            tEmail.Text = "";
        }

        private void tReport_MouseClick(object sender, EventArgs e)
        {
            tReport.ForeColor = System.Drawing.Color.Black;
            tReport.Text = "";
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void bSubmit_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(tEmail.Text) || string.IsNullOrEmpty(tReport.Text))
            {
                MessageBox.Show("Please fill out all fields before sending a report!");
                return;
            }

            Uri target = new Uri("http://legend-craft.tk/request");
            StringBuilder sb = new StringBuilder();

            sb.Append("os=").Append(Environment.OSVersion.Platform + " / " + Environment.OSVersion.VersionString);
            sb.Append("&runtime=");
            if (MonoCompat.IsMono)
            {
                sb.Append(Uri.EscapeDataString("Mono " + MonoCompat.MonoVersionString));
            }
            else
            {
                sb.Append(Uri.EscapeDataString(".Net " + Environment.Version.Major));
            }
            sb.Append("&message=" + tReport.Text);
            sb.Append("&email=" + tEmail.Text);
            sb.Append("&type=" + cType.SelectedItem.ToString());
            Logger.LogToConsole(sb.ToString());

            byte[] byteData = Encoding.UTF8.GetBytes(sb.ToString());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(target);
            request.Method = "POST";
            request.Timeout = 15000; // 15s timeout
            request.ContentType = "application/x-www-form-urlencoded";
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.ContentLength = byteData.Length;
            request.UserAgent = Updater.UserAgent;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(byteData, 0, byteData.Length);
                requestStream.Flush();
            }

            string responseString;
            try
            {
                using (HttpWebResponse res = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream response = res.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(response))
                        {
                            responseString = reader.ReadLine();//get the reponse
                        }
                    }
                }
                request.Abort();
            }
            catch(Exception ex)
            {
                Logger.LogToConsole("Error with reporter: " + ex);
                return;
            }
                    if (responseString != null && responseString.StartsWith("ERROR"))
                    {
                        Logger.Log(LogType.Error, "Crash report could not be processed by http://legend-craft.tk.");
                    }
                    else
                    {
                        int referenceNumber;
                        if (responseString != null && Int32.TryParse(responseString, out referenceNumber))
                        {
                            Logger.Log(LogType.SystemActivity, "Crash report submitted (Reference #{0})", referenceNumber);
                        }
                        else
                        {
                            Logger.Log(LogType.SystemActivity, "Crash report submitted.");
                        }
                    }
           
            Logger.LogToConsole("Report sent. Thank you for helping with the development of LegendCraft!");
            this.Close();

        }
    }
}

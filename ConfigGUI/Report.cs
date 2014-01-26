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
using System.Management;

namespace fCraft.ConfigGUI
{
    public partial class Report : Form
    {
        public Report()
        {
            InitializeComponent();
        }

        //it's actually this hard to get the name of the OS
        public static string GetOS()
        {
            string result = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (ManagementObject os in searcher.Get())
            {
                result = os["Caption"].ToString();
                break;
            }
            return result;
        }

        private void tEmail_MouseClick(object sender, EventArgs e)
        {
            tEmail.ForeColor = System.Drawing.Color.Black;
            if (tEmail.Text == "Email...")
            {
                tEmail.Text = "";
            }
        }

        private void tReport_MouseClick(object sender, EventArgs e)
        {
            tReport.ForeColor = System.Drawing.Color.Black;
            if (tReport.Text == "Type in your bug report/feature request here...")
            {
                tReport.Text = "";
            }
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void bSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tEmail.Text) || string.IsNullOrEmpty(tReport.Text))
            {
                MessageBox.Show("Please fill out all fields before sending a report!");
                return;
            }

            //otherwise, send!
            Uri target = new Uri("http://legend-craft.tk/request");
            StringBuilder sb = new StringBuilder();

            if (MonoCompat.IsMono)
            {
                sb.Append("os=").Append(Environment.OSVersion.VersionString);
                sb.Append("&runtime=").Append(Uri.EscapeDataString("Mono " + MonoCompat.MonoVersionString));
            }
            else
            {
                sb.Append("os=").Append(GetOS() + Environment.OSVersion.ServicePack);
                sb.Append("&runtime=").Append(Uri.EscapeDataString(".Net " + Environment.Version.Major + "." + Environment.Version.MajorRevision + "." + Environment.Version.Build));
            }
            sb.Append("&message=" + tReport.Text);
            sb.Append("&email=" + tEmail.Text);
            sb.Append("&type=" + cType.SelectedItem.ToString());

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
            catch (Exception ex)
            {
                MessageBox.Show("Error with reporter: " + ex);
                return;
            }
            if (responseString != null && responseString.StartsWith("ERROR"))
            {
                MessageBox.Show("Crash report could not be processed by http://legend-craft.tk.");
            }
            else
            {
                int referenceNumber;
                if (responseString != null && Int32.TryParse(responseString, out referenceNumber))
                {
                    MessageBox.Show("Crash report submitted, Reference #" + referenceNumber);
                }
                else
                {
                    MessageBox.Show( "Crash report submitted.");
                }
            }

            MessageBox.Show("Report sent. Thank you for helping with the development of LegendCraft!");
            this.Close();

        }

        private void Report_Load(object sender, EventArgs e)
        {
            cType.Text = "Feature";
        }
    }
}

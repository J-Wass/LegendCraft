﻿﻿/* Copyright (c) <2014> <LeChosenOne, DingusBungus>
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace AutoRank
{
    /// <summary>
    /// Handles the static variables, as well as xml loading/saving
    /// </summary>
    public static class Settings
    {
        public static bool multiLayered = false;
        public static List<TreeNode> tempChildNodes = new List<TreeNode>();
        public static List<string> usedConditionals = new List<string>();
        public static List<String> validRankList = new List<String>();

        //Save TreeList data to xml
        public static void Save()
        {
            if (File.Exists("Autorank.xml"))
            {
                File.Delete("Autorank.xml");
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter writer = XmlWriter.Create("Autorank.xml", settings);
            writer.WriteStartDocument();
            writer.WriteComment("This file was generated by the LegendCraft Autorank program.");

            writer.WriteStartElement("Autorank");//<Autorank>

            foreach (TreeNode mainNode in AutoRank.TreeList.Nodes)
            {
                writer.WriteStartElement(mainNode.Text.Replace(" ", ""));
                foreach (TreeNode childNode in mainNode.Nodes)
                {
                    string conditional;
                    string value;
                    if (childNode.Text.Contains("AND"))
                    {
                        string[] completely_Murdered_String_Of_Text = childNode.Text.Substring(childNode.Text.IndexOf(':') + 4).Split(new char[] { ' ' });//yup

                        conditional = completely_Murdered_String_Of_Text[1].Replace(" ", "");
                        value = (completely_Murdered_String_Of_Text[2] + completely_Murdered_String_Of_Text[3]).Replace(" ", "");
                        try
                        {
                            writer.WriteAttributeString(conditional, value);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occured while trying to write to xml!" + ex);
                        }
                    }
                    else
                    {
                        string[] slightly_Murdered_String_Of_Text = childNode.Text.Substring(3).Split(new char[] { ' ' });

                        conditional = slightly_Murdered_String_Of_Text[0].Replace(" ", "");
                        value = (slightly_Murdered_String_Of_Text[1] + slightly_Murdered_String_Of_Text[2]).Replace(" ", "");

                        try
                        {
                            writer.WriteAttributeString(conditional, value);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occured while trying to write to xml!" + ex);
                        }
                    }
                }
                writer.WriteEndElement();//</autorank>
            }

            writer.WriteEndElement();//</Autorank>

            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        //Load xml into TreeList
        public static void Load()
        {
            bool layered = false;
            //since Load() is more complicated than Save(), i'll use XDocument instead of XMLReader

            if (!File.Exists("Autorank.xml"))
            {
                MessageBox.Show("Autorank.xml not found, using defaults. Ignore this message if this is your first time running autorank.");
                return;
            }
            XDocument doc = XDocument.Load("Autorank.xml");
            XElement docConfig = doc.Root;

            //load each rank change
            foreach (XElement mainElement in docConfig.Elements())
            {
                //load each condition in each rank change
                foreach (XAttribute conditional in mainElement.Attributes())
                {
                    if (layered)
                    {
                        tempChildNodes.Add(new TreeNode("AND: If " + conditional.Name.ToString() + " " + applySpacing(conditional.Value)));
                    }
                    else
                    {
                        tempChildNodes.Add(new TreeNode("If " + conditional.Name.ToString() + " " + applySpacing(conditional.Value)));
                        layered = true;
                    }
                }
                TreeNode rankNode = new TreeNode(mainElement.Name.ToString(), tempChildNodes.ToArray());
                AutoRank.TreeList.Nodes.Add(rankNode);
                tempChildNodes = new List<TreeNode>();
                layered = false;
            }
        }

        /// <summary>
        /// Loads validRankList with the ranks of the server
        /// </summary>
        public static void LoadRankList()
        {
            if (!File.Exists("config.xml"))
            {
                MessageBox.Show("Error, config.xml is either missing or damaged. Please make sure configGUI.exe was run prior to autorank. Program will now close.");
                Application.Exit();
            }
            XDocument doc = XDocument.Load("config.xml");
            XElement docConfig = doc.Root;
            XElement rankList = docConfig.Element("Ranks");
            XElement[] rankDefinitionList = rankList.Elements("Rank").ToArray();
            foreach (XElement rankDefinition in rankDefinitionList)
            {
                foreach (XAttribute rankName in rankDefinition.Attributes())
                {
                    if (rankName.Name == "name")
                    {
                        validRankList.Add(rankName.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Apply the correct spacing to the conditional values found in Load. Used to change '>30' into '> 30' or '=/=590' into '=/= 590'.
        /// </summary>
        private static string applySpacing(String conditional)
        {
            string returnString;
            if (conditional.Contains("=/="))
            {
                returnString = conditional.Insert(3, " ");
                return returnString;
            }
            if (conditional.Contains(">=") || conditional.Contains("<="))
            {
                returnString = conditional.Insert(2, " ");
                return returnString;
            }
            else
            {
                returnString = conditional.Insert(1, " ");
                return returnString;
            }
        }

    }
}

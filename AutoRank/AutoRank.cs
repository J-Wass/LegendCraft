using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using fCraft;

namespace AutoRank
{
    public partial class AutoRank : Form
    {
        public AutoRank()
        {
            InitializeComponent();
        }

        private void AutoRank_Load(object sender, EventArgs e)
        {
            //Create tooltips here

            if (File.Exists("autorank.xml"))
            {
                //load xml
            }
            else
            {
                //MessageBox.Show("Warning: Cannot find autorank.xml. File is either corrupted or missing. Ignore this message if this is your first time running autorank.");
                //this.Close(); 
                //Commented out for quicker debugging purposes
            }

            //Load Ranks and IDs
            prevRank.Items.Clear();
            newRank.Items.Clear();

            XmlDocument doc = new XmlDocument();
            doc.Load("config.xml");
            XmlNodeList rankNodes = doc.SelectNodes("/fCraftConfig/Ranks/Rank");

            foreach (XmlNode rankNode in rankNodes)
            {
                prevRank.Items.Add(rankNode.Attributes["name"].Value);
                newRank.Items.Add(rankNode.Attributes["name"].Value);
            }
        }

        private void bCreate_Click(object sender, EventArgs e)
        {
            //Enable options to configure
            prevRank.Enabled = true;
            newRank.Enabled = true;
            condition.Enabled = true;
            value.Enabled = true;
            option.Enabled = true;
            bAdd.Enabled = true;
            op.Enabled = true;
        }

        private void bAdd_Click(object sender, EventArgs e)
        {
            double valCheck;
            bool isNum = double.TryParse(value.Text, out valCheck);
            if (!isNum)
            {
                MessageBox.Show("The value must be an integer!");
                return;
            }

            //Write to rankListings
            try
            {
                //if we are continuing
                if (Values.Or || Values.And || Values.ButNot)
                {
                    switch (option.SelectedItem.ToString())
                    {
                        case "Finished":
                            rankListings.Items.Add(Values.toAdd + ("If " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "\n\r"));
                            Values.And = false;
                            Values.Or = false;
                            Values.ButNot = false;
                            break;
                        case "Or":
                            Values.toAdd = Values.toAdd + ("If " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "Or\n\r");
                            Values.And = false;
                            Values.Or = true;
                            Values.ButNot = false;
                            break;
                        case "But not":
                            Values.toAdd = Values.toAdd + ("If " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "But not\n\r");
                            Values.And = true;
                            Values.Or = false;
                            Values.ButNot = true;
                            break;
                        case "And":
                            Values.toAdd = Values.toAdd + ("If " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "And\n\r");
                            Values.And = true;
                            Values.Or = false;
                            Values.ButNot = false;
                            break;
                        default:
                            MessageBox.Show("Make sure you fill out all fields before adding!");
                            break;
                    }        
                }

                switch (option.SelectedItem.ToString())
                {
                    case "Finished":
                        rankListings.Items.Add("--Rank from " + prevRank.SelectedItem.ToString() + " to " + newRank.SelectedItem.ToString() + " if " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "\n\r");
                        break;
                    case "Or":
                        Values.toAdd = ("--Rank from " + prevRank.SelectedItem.ToString() + " to " + newRank.SelectedItem.ToString() + " if " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "Or\n\r");
                        Values.Or = true;
                        break;
                    case "But not":
                        Values.toAdd = ("--Rank from " + prevRank.SelectedItem.ToString() + " to " + newRank.SelectedItem.ToString() + " if " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "But not\n\r");
                        Values.ButNot = true;
                        break;
                    case "And":
                        Values.toAdd = ("--Rank from " + prevRank.SelectedItem.ToString() + " to " + newRank.SelectedItem.ToString() + " if " + condition.SelectedItem + op.SelectedItem.ToString() + value.Text.ToString() + "And\n\r");
                        Values.And = true;
                        break;
                    default:
                        MessageBox.Show("Make sure you fill out all fields before adding!" + option.Text);
                        break;
                }                
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("Make sure you fill out all fields before adding!" + ex);
                return;
            }

            //Disable options if finished, also clear selections
            if (!Values.And && !Values.Or && !Values.ButNot)
            {
                prevRank.Enabled = false;
                newRank.Enabled = false;
                condition.Enabled = false;
                value.Enabled = false;
                option.Enabled = false;
                bAdd.Enabled = false;
                op.Enabled = false;

                newRank.Text = "";
                prevRank.Text = "";
                condition.Text = "";
                value.Clear();
                option.Text = "";
                op.Text = "";
            }
            //If a player wanted to continue with 'or', 'and', or 'butnot', we will disable the rank options, and clear the rest
            else
            {
                prevRank.Enabled = false;
                newRank.Enabled = false;

                condition.Text = "";
                value.Clear();
                option.Text = "";
                op.Text = "";
            }
        }

        private void bExit_Click(object sender, EventArgs e)
        {
            //write to xml
            this.Close();
        }

        private void bRemove_Click(object sender, EventArgs e)
        {
            if(rankListings.SelectedItem != null)
            {
                rankListings.Items.Remove(rankListings.SelectedItem);
            }
            else
            {
                MessageBox.Show("Please select an item from the list above to remove.");
            }
        }

        private void option_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (option.SelectedItem != "Finished")
            {
                bAdd.Text = "Next";
            }
            else
            {
                bAdd.Text = "Add";
            }
        }

    }
}

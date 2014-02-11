using System;
//Copyright (c) LegendCraft Team <LeChosenOne, DingusBungus>

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
            if (!File.Exists("config.xml"))
            {
                MessageBox.Show("Warning, config.xml not found. Closing Autorank program.");
                this.Close();
            }

            Settings.Load();
        }

        private void bCreate_Click(object sender, EventArgs e)
        {
            if (prevRank.Items.Count == 0)
            {
                List<Rank> validRankList = new List<Rank>();

                XDocument doc = XDocument.Load("config.xml");
                XElement docConfig = doc.Root;
                XElement rankList = docConfig.Element("Ranks");
                XElement[] rankDefinitionList = rankList.Elements("Rank").ToArray();
                foreach (XElement rankDefinition in rankDefinitionList)
                {
                    try
                    {
                        validRankList.Add(new Rank(rankDefinition));
                    }
                    catch (RankDefinitionException ex)
                    {
                        MessageBox.Show(ex + " " + ex.Data);
                    }
                }

                foreach (Rank rank in validRankList)
                {
                    prevRank.Items.Add(rank.Name);
                    newRank.Items.Add(rank.Name);
                }
            }

            if (Settings.usedConditionals.Contains(condition.Text))
            {
                MessageBox.Show("You have already used that conditional type in this rank node!");
                return;
            }
            Settings.usedConditionals.Add(condition.Text);

            prevRank.Enabled = true;
            newRank.Enabled = true;
            condition.Enabled = true;
            op.Enabled = true;
            value.Enabled = true;
            option.Enabled = true;
            bAdd.Enabled = true;
        }

        private void bAdd_Click(object sender, EventArgs e)
        {
            if (prevRank.Text == newRank.Text)
            {
                MessageBox.Show(String.Format("You cannot rank yourself from {0} to {0}!", prevRank.Text));
                return;
            }
            if(string.IsNullOrEmpty(prevRank.Text) || string.IsNullOrEmpty(newRank.Text) || string.IsNullOrEmpty(value.Text) ||
               string.IsNullOrEmpty(condition.Text) || string.IsNullOrEmpty(op.Text) || string.IsNullOrEmpty(option.Text))                            
            {
                MessageBox.Show("Oops... One or more of the fields were not filled out!");
                return;
            }

            double valueInt;
            if (!Double.TryParse(value.Text, out valueInt))
            {
                MessageBox.Show("Uh oh! The value textbox must be a whole number!");
                return;
            }

            //If this is a continuation, disable undeeded params
            if (bAdd.Text == "Continue")
            {
                bCreate.Enabled = false;
                prevRank.Enabled = false;
                newRank.Enabled = false;
            }

            //past this point, all fields are valid

            //add another condition
            if (option.Text != "Finished")
            {
                Settings.multiLayered = true;

                if (Settings.tempChildNodes.Count() > 0)
                {
                    Settings.tempChildNodes.Add(new TreeNode("AND: If " + condition.Text + " " + op.Text + " " + value.Text));
                }
                else
                {
                    Settings.tempChildNodes.Add(new TreeNode("If " + condition.Text + " " + op.Text + " " + value.Text));
                }
            }

            //finish the condition
            else
            {
                //if there was more than one condition
                if (Settings.multiLayered)
                {
                    Settings.tempChildNodes.Add(new TreeNode("AND: If " + condition.Text + " " + op.Text + " " + value.Text));
                    TreeNode[] MultiChildNode = Settings.tempChildNodes.ToArray();
                    TreeNode MultiFinalNode = new TreeNode(prevRank.Text + " - " + newRank.Text, MultiChildNode);
                    Settings.multiLayered = false;                 
                    TreeList.Nodes.Add(MultiFinalNode);
                }
                //only 1 condition
                else
                {
                    TreeNode ConditionNode = new TreeNode("If " + condition.Text + " " + op.Text + " " + value.Text);
                    TreeNode[] ChildNode = new TreeNode[] { ConditionNode };

                    TreeNode FinalNode = new TreeNode(prevRank.Text + " - " + newRank.Text, ChildNode);
                    TreeList.Nodes.Add(FinalNode);
                }
                bCreate.Enabled = true;

                prevRank.Enabled = false;
                newRank.Enabled = false;
                condition.Enabled = false;
                op.Enabled = false;
                value.Enabled = false;
                option.Enabled = false;
                bAdd.Enabled = false;

                value.Clear();
                prevRank.Text = "";
                newRank.Text = "";
                condition.Text = "";
                op.Text = "";
                option.Text = "";

                Settings.tempChildNodes = new List<TreeNode>();
            }

        }

        private void option_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (option.Text == "Finished")
            {
                bAdd.Text = "Add";
            }
            else
            {
                bAdd.Text = "Continue";
            }
        }

        private void condition_SelectedIndexChanged(object sender, EventArgs e)
        {
            //grueling code ahead, create units
            switch (condition.Text)
            {
                case "Since First Login":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Since Last Login":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Last Seen":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Total Time":
                    valueLabel.Text = "Value (Hours)";
                    break;

                case "Blocks Built":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Blocks Deleted":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Blocks Changed":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Blocks Drawn":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Visits":
                    valueLabel.Text = "Value (Visits)";
                    break;

                case "Messages":
                    valueLabel.Text = "Value (Sent)";
                    break;

                case "Times Kicked":
                    valueLabel.Text = "Value (Times)";
                    break;

                case "Since Rank Change":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Since Last Kick":
                    valueLabel.Text = "Value (Days)";
                    break;

                default:
                    //shouldn't happen
                    break;

            }
        }

        private void bExit_Click(object sender, EventArgs e)
        {
            Settings.Save();
            this.Close();
        }

        private void bRemove_Click(object sender, EventArgs e)
        {
            if (TreeList.SelectedNode == null)
            {
                //empty for sho`
                return;
            }

            TreeList.Nodes.Remove(TreeList.SelectedNode);

        }
    }
}

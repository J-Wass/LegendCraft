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
            if (!File.Exists("config.xml"))
            {
                MessageBox.Show("Warning, config.xml not found. Closing Autorank program.");
                this.Close();
            }
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
            if (option.Text != "Finished")
            {
                Values.multiLayered = true;
                Values.tempChildNodes.Add(new TreeNode(Values.prevOption + ": If " + condition.Text + " " + op.Text + " " + value.Text));
                Values.prevOption = option.Text;
            }
            else
            {
                if (Values.multiLayered)
                {
                    Values.tempChildNodes.Add(new TreeNode("If " + condition.Text + " " + op.Text + " " + value.Text));
                    TreeNode[] MultiChildNode = Values.tempChildNodes.ToArray();
                    TreeNode MultiFinalNode = new TreeNode(prevRank.Text + " - " + newRank.Text, MultiChildNode);
                    Values.multiLayered = false;
                    Values.prevOption = "";                     
                    TreeList.Nodes.Add(MultiFinalNode);
                }
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
                    valueLabel.Text = "Value (Sents)";
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
    }
}

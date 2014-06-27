/* Copyright (c) <2014> <LeChosenOne, DingusBungus>
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace AutoRank
{
    /// <summary>
    /// Handles the UI of the program
    /// </summary>

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
                Settings.LoadRankList();
                foreach (String rank in Settings.validRankList)
                {
                    prevRank.Items.Add(rank);
                    newRank.Items.Add(rank);
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
            if (prevRank.Text == newRank.Text)
            {
                MessageBox.Show(String.Format("You cannot rank yourself from {0} to {0}!", prevRank.Text));
                return;
            }
            if (string.IsNullOrEmpty(prevRank.Text) || string.IsNullOrEmpty(newRank.Text) || string.IsNullOrEmpty(value.Text) ||
               string.IsNullOrEmpty(condition.Text) || string.IsNullOrEmpty(op.Text) || string.IsNullOrEmpty(option.Text))
            {
                MessageBox.Show("Oops... One or more of the fields were not filled out!");
                return;
            }

            if (Settings.usedConditionals.Contains(condition.Text))
            {
                MessageBox.Show("You have already used that conditional type in this rank node!");
                return;
            }
            Settings.usedConditionals.Add(condition.Text);

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
            if (option.Text != "Submit")
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
                    TreeNode MultiFinalNode = new TreeNode(prevRank.Text + "-" + newRank.Text, MultiChildNode);
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

                value.Clear();
                prevRank.Text = "";
                newRank.Text = "";
                condition.Text = "";
                op.Text = "";
                option.Text = "";

                prevRank.Enabled = false;
                newRank.Enabled = false;
                condition.Enabled = false;
                op.Enabled = false;
                value.Enabled = false;
                option.Enabled = false;
                bAdd.Enabled = false;

                Settings.tempChildNodes = new List<TreeNode>();
                Settings.usedConditionals = new List<String>();
            }

        }

        private void option_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (option.Text == "Submit")
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
                case "Since_First_Login":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Since_Last_Login":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Last_Seen":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Total_Time":
                    valueLabel.Text = "Value (Hours)";
                    break;

                case "Blocks_Built":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Blocks_Deleted":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Blocks_Changed":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Blocks_Drawn":
                    valueLabel.Text = "Value (Blocks)";
                    break;

                case "Visits":
                    valueLabel.Text = "Value (Visits)";
                    break;

                case "Messages":
                    valueLabel.Text = "Value (Sent)";
                    break;

                case "Times_Kicked":
                    valueLabel.Text = "Value (Times)";
                    break;

                case "Since_Rank_Change":
                    valueLabel.Text = "Value (Days)";
                    break;

                case "Since_Last_Kick":
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

            DialogResult yesNo = MessageBox.Show("Are you sure you want to remove that rank node?", "Warning", MessageBoxButtons.YesNo);
            if (yesNo == DialogResult.Yes)
            {
                if (TreeList.SelectedNode.Parent != null)
                {
                    TreeList.Nodes.Remove(TreeList.SelectedNode.Parent);
                }
                else
                {
                    TreeList.Nodes.Remove(TreeList.SelectedNode);
                }
            }

        }
    }
}
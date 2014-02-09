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
            foreach (Rank rank in RankManager.Ranks)
            {
                prevRank.Items.Add(rank.Name);
                newRank.Items.Add(rank.Name);
            }
        }

        private void bCreate_Click(object sender, EventArgs e)
        {
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

            //past this point, all fields are valid
            TreeNode ConditionNode = new TreeNode("If " + condition.Text + " " + op.Text + " " + value.Text);
            TreeNode[] ChildNode = new TreeNode[] {ConditionNode};

            TreeNode FinalNode = new TreeNode(prevRank.Text + " - " + newRank.Text, ChildNode);
            TreeList.Nodes.Add(FinalNode);

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
}

namespace AutoRank
{
    partial class AutoRank
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoRank));
            this.bCreate = new System.Windows.Forms.Button();
            this.prevRank = new System.Windows.Forms.ComboBox();
            this.prevLabel = new System.Windows.Forms.Label();
            this.newLabel = new System.Windows.Forms.Label();
            this.newRank = new System.Windows.Forms.ComboBox();
            this.rankListings = new System.Windows.Forms.ListBox();
            this.conditionLabel = new System.Windows.Forms.Label();
            this.condition = new System.Windows.Forms.ComboBox();
            this.valueLabel = new System.Windows.Forms.Label();
            this.value = new System.Windows.Forms.TextBox();
            this.bAdd = new System.Windows.Forms.Button();
            this.option = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bExit = new System.Windows.Forms.Button();
            this.bRemove = new System.Windows.Forms.Button();
            this.boxSettings = new System.Windows.Forms.GroupBox();
            this.op = new System.Windows.Forms.ComboBox();
            this.boxSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // bCreate
            // 
            this.bCreate.Location = new System.Drawing.Point(7, 19);
            this.bCreate.Name = "bCreate";
            this.bCreate.Size = new System.Drawing.Size(110, 22);
            this.bCreate.TabIndex = 1;
            this.bCreate.Text = "Create New";
            this.bCreate.UseVisualStyleBackColor = true;
            this.bCreate.Click += new System.EventHandler(this.bCreate_Click);
            // 
            // prevRank
            // 
            this.prevRank.Enabled = false;
            this.prevRank.FormattingEnabled = true;
            this.prevRank.Location = new System.Drawing.Point(16, 65);
            this.prevRank.Name = "prevRank";
            this.prevRank.Size = new System.Drawing.Size(91, 21);
            this.prevRank.TabIndex = 2;
            // 
            // prevLabel
            // 
            this.prevLabel.AutoSize = true;
            this.prevLabel.Location = new System.Drawing.Point(15, 49);
            this.prevLabel.Name = "prevLabel";
            this.prevLabel.Size = new System.Drawing.Size(72, 13);
            this.prevLabel.TabIndex = 3;
            this.prevLabel.Text = "Starting Rank";
            // 
            // newLabel
            // 
            this.newLabel.AutoSize = true;
            this.newLabel.Location = new System.Drawing.Point(15, 99);
            this.newLabel.Name = "newLabel";
            this.newLabel.Size = new System.Drawing.Size(67, 13);
            this.newLabel.TabIndex = 5;
            this.newLabel.Text = "Target Rank";
            // 
            // newRank
            // 
            this.newRank.Enabled = false;
            this.newRank.FormattingEnabled = true;
            this.newRank.Location = new System.Drawing.Point(15, 115);
            this.newRank.Name = "newRank";
            this.newRank.Size = new System.Drawing.Size(91, 21);
            this.newRank.TabIndex = 4;
            // 
            // rankListings
            // 
            this.rankListings.FormattingEnabled = true;
            this.rankListings.HorizontalScrollbar = true;
            this.rankListings.Location = new System.Drawing.Point(141, 9);
            this.rankListings.Name = "rankListings";
            this.rankListings.Size = new System.Drawing.Size(218, 303);
            this.rankListings.TabIndex = 6;
            // 
            // conditionLabel
            // 
            this.conditionLabel.AutoSize = true;
            this.conditionLabel.Location = new System.Drawing.Point(15, 149);
            this.conditionLabel.Name = "conditionLabel";
            this.conditionLabel.Size = new System.Drawing.Size(51, 13);
            this.conditionLabel.TabIndex = 7;
            this.conditionLabel.Text = "Condition";
            // 
            // condition
            // 
            this.condition.Enabled = false;
            this.condition.FormattingEnabled = true;
            this.condition.Items.AddRange(new object[] {
            "Since First Login",
            "Since Last Login",
            "Last Seen",
            "Total Time",
            "Blocks Built",
            "Blocks Deleted",
            "Blocks Changed",
            "Blocks Drawn",
            "Visits",
            "Messages",
            "Times Kicked",
            "Since Rank Change",
            "Since Last Kick"});
            this.condition.Location = new System.Drawing.Point(15, 165);
            this.condition.Name = "condition";
            this.condition.Size = new System.Drawing.Size(91, 21);
            this.condition.TabIndex = 8;
            // 
            // valueLabel
            // 
            this.valueLabel.AutoSize = true;
            this.valueLabel.Location = new System.Drawing.Point(56, 199);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(34, 13);
            this.valueLabel.TabIndex = 9;
            this.valueLabel.Text = "Value";
            // 
            // value
            // 
            this.value.Enabled = false;
            this.value.Location = new System.Drawing.Point(59, 215);
            this.value.Name = "value";
            this.value.Size = new System.Drawing.Size(48, 20);
            this.value.TabIndex = 10;
            // 
            // bAdd
            // 
            this.bAdd.Enabled = false;
            this.bAdd.Location = new System.Drawing.Point(44, 292);
            this.bAdd.Name = "bAdd";
            this.bAdd.Size = new System.Drawing.Size(38, 27);
            this.bAdd.TabIndex = 11;
            this.bAdd.Text = "Add";
            this.bAdd.UseVisualStyleBackColor = true;
            this.bAdd.Click += new System.EventHandler(this.bAdd_Click);
            // 
            // option
            // 
            this.option.Enabled = false;
            this.option.FormattingEnabled = true;
            this.option.Items.AddRange(new object[] {
            "And",
            "Or",
            "But not",
            "Finished"});
            this.option.Location = new System.Drawing.Point(15, 265);
            this.option.Name = "option";
            this.option.Size = new System.Drawing.Size(91, 21);
            this.option.TabIndex = 13;
            this.option.SelectedIndexChanged += new System.EventHandler(this.option_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 249);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Option";
            // 
            // bExit
            // 
            this.bExit.Location = new System.Drawing.Point(312, 314);
            this.bExit.Name = "bExit";
            this.bExit.Size = new System.Drawing.Size(47, 27);
            this.bExit.TabIndex = 14;
            this.bExit.Text = "Save";
            this.bExit.UseVisualStyleBackColor = true;
            this.bExit.Click += new System.EventHandler(this.bExit_Click);
            // 
            // bRemove
            // 
            this.bRemove.Location = new System.Drawing.Point(141, 318);
            this.bRemove.Name = "bRemove";
            this.bRemove.Size = new System.Drawing.Size(155, 23);
            this.bRemove.TabIndex = 15;
            this.bRemove.Text = "Remove Selected  Condition";
            this.bRemove.UseVisualStyleBackColor = true;
            this.bRemove.Click += new System.EventHandler(this.bRemove_Click);
            // 
            // boxSettings
            // 
            this.boxSettings.Controls.Add(this.op);
            this.boxSettings.Controls.Add(this.option);
            this.boxSettings.Controls.Add(this.label1);
            this.boxSettings.Controls.Add(this.bCreate);
            this.boxSettings.Controls.Add(this.bAdd);
            this.boxSettings.Controls.Add(this.value);
            this.boxSettings.Controls.Add(this.valueLabel);
            this.boxSettings.Controls.Add(this.condition);
            this.boxSettings.Controls.Add(this.conditionLabel);
            this.boxSettings.Controls.Add(this.newLabel);
            this.boxSettings.Controls.Add(this.newRank);
            this.boxSettings.Controls.Add(this.prevLabel);
            this.boxSettings.Controls.Add(this.prevRank);
            this.boxSettings.Location = new System.Drawing.Point(12, 9);
            this.boxSettings.Name = "boxSettings";
            this.boxSettings.Size = new System.Drawing.Size(123, 335);
            this.boxSettings.TabIndex = 16;
            this.boxSettings.TabStop = false;
            this.boxSettings.Text = "Settings";
            // 
            // op
            // 
            this.op.Enabled = false;
            this.op.FormattingEnabled = true;
            this.op.Items.AddRange(new object[] {
            "=",
            ">",
            "=>",
            "<",
            "<=",
            "=/="});
            this.op.Location = new System.Drawing.Point(15, 214);
            this.op.Name = "op";
            this.op.Size = new System.Drawing.Size(33, 21);
            this.op.TabIndex = 15;
            // 
            // AutoRank
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Firebrick;
            this.ClientSize = new System.Drawing.Size(366, 353);
            this.Controls.Add(this.boxSettings);
            this.Controls.Add(this.bExit);
            this.Controls.Add(this.bRemove);
            this.Controls.Add(this.rankListings);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AutoRank";
            this.Text = "LegendCraft AutoRank Program";
            this.Load += new System.EventHandler(this.AutoRank_Load);
            this.boxSettings.ResumeLayout(false);
            this.boxSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bCreate;
        private System.Windows.Forms.ComboBox prevRank;
        private System.Windows.Forms.Label prevLabel;
        private System.Windows.Forms.Label newLabel;
        private System.Windows.Forms.ComboBox newRank;
        private System.Windows.Forms.ListBox rankListings;
        private System.Windows.Forms.Label conditionLabel;
        private System.Windows.Forms.ComboBox condition;
        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.TextBox value;
        private System.Windows.Forms.Button bAdd;
        private System.Windows.Forms.ComboBox option;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bExit;
        private System.Windows.Forms.Button bRemove;
        private System.Windows.Forms.GroupBox boxSettings;
        private System.Windows.Forms.ComboBox op;
    }
}


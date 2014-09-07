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
            this.TreeList = new System.Windows.Forms.TreeView();
            this.button1 = new System.Windows.Forms.Button();
            this.boxSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // bCreate
            // 
            this.bCreate.BackColor = System.Drawing.Color.WhiteSmoke;
            this.bCreate.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.bCreate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.bCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bCreate.Location = new System.Drawing.Point(15, 19);
            this.bCreate.Name = "bCreate";
            this.bCreate.Size = new System.Drawing.Size(121, 22);
            this.bCreate.TabIndex = 1;
            this.bCreate.Text = "Create New";
            this.bCreate.UseVisualStyleBackColor = false;
            this.bCreate.Click += new System.EventHandler(this.bCreate_Click);
            // 
            // prevRank
            // 
            this.prevRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.prevRank.Enabled = false;
            this.prevRank.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.prevRank.FormattingEnabled = true;
            this.prevRank.Location = new System.Drawing.Point(16, 65);
            this.prevRank.Name = "prevRank";
            this.prevRank.Size = new System.Drawing.Size(120, 21);
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
            this.newRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.newRank.Enabled = false;
            this.newRank.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.newRank.FormattingEnabled = true;
            this.newRank.Location = new System.Drawing.Point(15, 115);
            this.newRank.Name = "newRank";
            this.newRank.Size = new System.Drawing.Size(121, 21);
            this.newRank.TabIndex = 4;
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
            this.condition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.condition.Enabled = false;
            this.condition.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.condition.FormattingEnabled = true;
            this.condition.Items.AddRange(new object[] {
            "Since_First_Login",
            "Since_Last_Login",
            "Last_Seen",
            "Total_Time",
            "Blocks_Built",
            "Blocks_Deleted",
            "Blocks_Changed",
            "Blocks_Drawn",
            "Visits",
            "Messages",
            "Times_Kicked",
            "Since_Rank_Change",
            "Since_Last_Kick"});
            this.condition.Location = new System.Drawing.Point(15, 165);
            this.condition.Name = "condition";
            this.condition.Size = new System.Drawing.Size(121, 21);
            this.condition.TabIndex = 8;
            this.condition.SelectedIndexChanged += new System.EventHandler(this.condition_SelectedIndexChanged);
            // 
            // valueLabel
            // 
            this.valueLabel.AutoSize = true;
            this.valueLabel.Location = new System.Drawing.Point(67, 199);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(34, 13);
            this.valueLabel.TabIndex = 9;
            this.valueLabel.Text = "Value";
            // 
            // value
            // 
            this.value.Enabled = false;
            this.value.Location = new System.Drawing.Point(70, 215);
            this.value.Name = "value";
            this.value.Size = new System.Drawing.Size(66, 20);
            this.value.TabIndex = 10;
            // 
            // bAdd
            // 
            this.bAdd.BackColor = System.Drawing.Color.WhiteSmoke;
            this.bAdd.Enabled = false;
            this.bAdd.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.bAdd.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.bAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bAdd.Location = new System.Drawing.Point(32, 302);
            this.bAdd.Name = "bAdd";
            this.bAdd.Size = new System.Drawing.Size(91, 27);
            this.bAdd.TabIndex = 11;
            this.bAdd.Text = "Add";
            this.bAdd.UseVisualStyleBackColor = false;
            this.bAdd.Click += new System.EventHandler(this.bAdd_Click);
            // 
            // option
            // 
            this.option.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.option.Enabled = false;
            this.option.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.option.FormattingEnabled = true;
            this.option.Items.AddRange(new object[] {
            "Add Condition",
            "Submit"});
            this.option.Location = new System.Drawing.Point(15, 265);
            this.option.Name = "option";
            this.option.Size = new System.Drawing.Size(121, 21);
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
            this.bExit.BackColor = System.Drawing.Color.WhiteSmoke;
            this.bExit.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.bExit.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.bExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bExit.Location = new System.Drawing.Point(282, 314);
            this.bExit.Name = "bExit";
            this.bExit.Size = new System.Drawing.Size(47, 27);
            this.bExit.TabIndex = 14;
            this.bExit.Text = "Save";
            this.bExit.UseVisualStyleBackColor = false;
            this.bExit.Click += new System.EventHandler(this.bExit_Click);
            // 
            // bRemove
            // 
            this.bRemove.BackColor = System.Drawing.Color.WhiteSmoke;
            this.bRemove.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.bRemove.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.bRemove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bRemove.Location = new System.Drawing.Point(168, 314);
            this.bRemove.Name = "bRemove";
            this.bRemove.Size = new System.Drawing.Size(108, 27);
            this.bRemove.TabIndex = 15;
            this.bRemove.Text = "Remove Selected  ";
            this.bRemove.UseVisualStyleBackColor = false;
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
            this.boxSettings.Size = new System.Drawing.Size(150, 335);
            this.boxSettings.TabIndex = 16;
            this.boxSettings.TabStop = false;
            this.boxSettings.Text = "Settings";
            // 
            // op
            // 
            this.op.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.op.Enabled = false;
            this.op.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.op.FormattingEnabled = true;
            this.op.Items.AddRange(new object[] {
            "=",
            ">",
            ">=",
            "<",
            "<=",
            "=/="});
            this.op.Location = new System.Drawing.Point(15, 214);
            this.op.Name = "op";
            this.op.Size = new System.Drawing.Size(38, 21);
            this.op.TabIndex = 15;
            // 
            // TreeList
            // 
            this.TreeList.Location = new System.Drawing.Point(168, 17);
            this.TreeList.Name = "TreeList";
            this.TreeList.Size = new System.Drawing.Size(217, 284);
            this.TreeList.TabIndex = 17;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(336, 314);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 18;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // AutoRank
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Firebrick;
            this.ClientSize = new System.Drawing.Size(397, 353);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.TreeList);
            this.Controls.Add(this.boxSettings);
            this.Controls.Add(this.bExit);
            this.Controls.Add(this.bRemove);
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
        private System.Windows.Forms.Button button1;
        public System.Windows.Forms.TreeView TreeList;
    }
}

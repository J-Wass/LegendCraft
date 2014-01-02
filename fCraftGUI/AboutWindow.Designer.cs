namespace fCraft.GUI {
    sealed partial class AboutWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutWindow));
            this.lHeader = new System.Windows.Forms.Label();
            this.lSubheader = new System.Windows.Forms.Label();
            this.lfCraft = new System.Windows.Forms.LinkLabel();
            this.l800Craft = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lThank = new System.Windows.Forms.Label();
            this.lLegendCraft = new System.Windows.Forms.LinkLabel();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lHeader
            // 
            this.lHeader.AutoSize = true;
            this.lHeader.BackColor = System.Drawing.Color.Transparent;
            this.lHeader.Font = new System.Drawing.Font("Consolas", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lHeader.ForeColor = System.Drawing.Color.SlateGray;
            this.lHeader.Location = new System.Drawing.Point(6, 53);
            this.lHeader.Name = "lHeader";
            this.lHeader.Size = new System.Drawing.Size(180, 56);
            this.lHeader.TabIndex = 1;
            this.lHeader.Text = "Legend";
            // 
            // lSubheader
            // 
            this.lSubheader.AutoSize = true;
            this.lSubheader.BackColor = System.Drawing.Color.Transparent;
            this.lSubheader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lSubheader.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lSubheader.Location = new System.Drawing.Point(13, 160);
            this.lSubheader.Name = "lSubheader";
            this.lSubheader.Size = new System.Drawing.Size(321, 91);
            this.lSubheader.TabIndex = 2;
            this.lSubheader.Text = "Free, open-source Minecraft/ClassiCube game software\r\nBased on fCraft and 800Craf" +
    "t\r\nDeveloped by LeChosenOne and DingusBungus\r\n\r\n800Craft:\r\nfCraft:\r\nLegendCraft:" +
    "\r\n";
            // 
            // lfCraft
            // 
            this.lfCraft.AutoSize = true;
            this.lfCraft.BackColor = System.Drawing.Color.Transparent;
            this.lfCraft.Location = new System.Drawing.Point(98, 225);
            this.lfCraft.Name = "lfCraft";
            this.lfCraft.Size = new System.Drawing.Size(32, 13);
            this.lfCraft.TabIndex = 3;
            this.lfCraft.TabStop = true;
            this.lfCraft.Text = "fCraft";
            // 
            // l800Craft
            // 
            this.l800Craft.AutoSize = true;
            this.l800Craft.BackColor = System.Drawing.Color.Transparent;
            this.l800Craft.Location = new System.Drawing.Point(98, 212);
            this.l800Craft.Name = "l800Craft";
            this.l800Craft.Size = new System.Drawing.Size(47, 13);
            this.l800Craft.TabIndex = 4;
            this.l800Craft.TabStop = true;
            this.l800Craft.Text = "800Craft";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Consolas", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Firebrick;
            this.label1.Location = new System.Drawing.Point(174, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(154, 56);
            this.label1.TabIndex = 5;
            this.label1.Text = "Craft";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label3.Location = new System.Drawing.Point(6, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(279, 26);
            this.label3.TabIndex = 6;
            this.label3.Text = "A MineCraft Classic Software\r\nDeveloped By LeChosenOne and DingusBungus";
            // 
            // lThank
            // 
            this.lThank.AutoSize = true;
            this.lThank.BackColor = System.Drawing.Color.Transparent;
            this.lThank.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lThank.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lThank.Location = new System.Drawing.Point(13, 284);
            this.lThank.Name = "lThank";
            this.lThank.Size = new System.Drawing.Size(219, 13);
            this.lThank.TabIndex = 8;
            this.lThank.Text = "And thank you for using LegendCraft!\r\n";
            this.lThank.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lLegendCraft
            // 
            this.lLegendCraft.AutoSize = true;
            this.lLegendCraft.BackColor = System.Drawing.Color.Transparent;
            this.lLegendCraft.Location = new System.Drawing.Point(98, 238);
            this.lLegendCraft.Name = "lLegendCraft";
            this.lLegendCraft.Size = new System.Drawing.Size(65, 13);
            this.lLegendCraft.TabIndex = 11;
            this.lLegendCraft.TabStop = true;
            this.lLegendCraft.Text = "LegendCraft";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(3, 40);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(322, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "_____________________________________________";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(3, 109);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(322, 13);
            this.label8.TabIndex = 13;
            this.label8.Text = "_____________________________________________";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.DimGray;
            this.groupBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("groupBox1.BackgroundImage")));
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.lThank);
            this.groupBox1.Controls.Add(this.lLegendCraft);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.lHeader);
            this.groupBox1.Controls.Add(this.l800Craft);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.lfCraft);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.lSubheader);
            this.groupBox1.Location = new System.Drawing.Point(12, 33);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(369, 314);
            this.groupBox1.TabIndex = 14;
            this.groupBox1.TabStop = false;
            // 
            // AboutWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Firebrick;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(388, 359);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(200, 200);
            this.Name = "AboutWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "LegendCraft";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lHeader;
        private System.Windows.Forms.Label lSubheader;
        private System.Windows.Forms.LinkLabel lfCraft;
        private System.Windows.Forms.LinkLabel l800Craft;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lThank;
        private System.Windows.Forms.LinkLabel lLegendCraft;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}
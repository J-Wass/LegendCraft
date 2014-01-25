namespace fCraft.ServerGUI
{
    partial class Report
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
            this.panelRequest = new System.Windows.Forms.Panel();
            this.lRequest = new System.Windows.Forms.Label();
            this.tEmail = new System.Windows.Forms.TextBox();
            this.cType = new System.Windows.Forms.ComboBox();
            this.tReport = new System.Windows.Forms.TextBox();
            this.bSubmit = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.panelRequest.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelRequest
            // 
            this.panelRequest.BackColor = System.Drawing.SystemColors.Control;
            this.panelRequest.Controls.Add(this.bCancel);
            this.panelRequest.Controls.Add(this.bSubmit);
            this.panelRequest.Controls.Add(this.tReport);
            this.panelRequest.Controls.Add(this.cType);
            this.panelRequest.Controls.Add(this.tEmail);
            this.panelRequest.Location = new System.Drawing.Point(-7, 49);
            this.panelRequest.Name = "panelRequest";
            this.panelRequest.Size = new System.Drawing.Size(532, 238);
            this.panelRequest.TabIndex = 0;
            // 
            // lRequest
            // 
            this.lRequest.AutoSize = true;
            this.lRequest.Font = new System.Drawing.Font("Verdana", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lRequest.Location = new System.Drawing.Point(43, 9);
            this.lRequest.Name = "lRequest";
            this.lRequest.Size = new System.Drawing.Size(411, 23);
            this.lRequest.TabIndex = 1;
            this.lRequest.Text = "Request a feature or report a bug fix here";
            // 
            // tEmail
            // 
            this.tEmail.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tEmail.ForeColor = System.Drawing.SystemColors.ScrollBar;
            this.tEmail.Location = new System.Drawing.Point(19, 17);
            this.tEmail.Multiline = true;
            this.tEmail.Name = "tEmail";
            this.tEmail.Size = new System.Drawing.Size(181, 25);
            this.tEmail.TabIndex = 0;
            this.tEmail.Text = "Email...";
            this.tEmail.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tEmail_MouseClick);
            // 
            // cType
            // 
            this.cType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cType.FormattingEnabled = true;
            this.cType.Items.AddRange(new object[] {
            "Feature",
            "Bug"});
            this.cType.Location = new System.Drawing.Point(293, 17);
            this.cType.Name = "cType";
            this.cType.Size = new System.Drawing.Size(181, 21);
            this.cType.TabIndex = 1;
            // 
            // tReport
            // 
            this.tReport.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tReport.ForeColor = System.Drawing.SystemColors.ScrollBar;
            this.tReport.Location = new System.Drawing.Point(19, 62);
            this.tReport.Multiline = true;
            this.tReport.Name = "tReport";
            this.tReport.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tReport.Size = new System.Drawing.Size(455, 123);
            this.tReport.TabIndex = 2;
            this.tReport.Text = "Type in your bug report/feature request here...";
            this.tReport.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tReport_MouseClick);
            // 
            // bSubmit
            // 
            this.bSubmit.BackColor = System.Drawing.Color.LightSteelBlue;
            this.bSubmit.FlatAppearance.MouseDownBackColor = System.Drawing.Color.SlateGray;
            this.bSubmit.FlatAppearance.MouseOverBackColor = System.Drawing.Color.LightSlateGray;
            this.bSubmit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bSubmit.Location = new System.Drawing.Point(430, 198);
            this.bSubmit.Name = "bSubmit";
            this.bSubmit.Size = new System.Drawing.Size(72, 28);
            this.bSubmit.TabIndex = 3;
            this.bSubmit.Text = "Submit";
            this.bSubmit.UseVisualStyleBackColor = false;
            this.bSubmit.Click += new System.EventHandler(this.bSubmit_Click);
            // 
            // bCancel
            // 
            this.bCancel.BackColor = System.Drawing.Color.IndianRed;
            this.bCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Brown;
            this.bCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Firebrick;
            this.bCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bCancel.Location = new System.Drawing.Point(337, 198);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(72, 28);
            this.bCancel.TabIndex = 4;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = false;
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            // 
            // Report
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(509, 286);
            this.Controls.Add(this.lRequest);
            this.Controls.Add(this.panelRequest);
            this.Name = "Report";
            this.Text = "Report";
            this.panelRequest.ResumeLayout(false);
            this.panelRequest.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelRequest;
        private System.Windows.Forms.TextBox tEmail;
        private System.Windows.Forms.Label lRequest;
        private System.Windows.Forms.ComboBox cType;
        private System.Windows.Forms.TextBox tReport;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bSubmit;
    }
}
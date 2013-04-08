using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace fCraft.ServerGUI
{
    public partial class PlayerViewer : Form
    {
        private TextBox textBox1;
        private Label label1;
        private Button PMButton;
        private TextBox MTextBox;
        private Label ReasonLabel;
        private Label label2;
        PlayerInfo player;
        public PlayerViewer(fCraft.PlayerInfo player_)
        {
            InitializeComponent();
            player = player_;
            //PlayerInfo info = player;
            PlayerInfo info = player_;
            textBox1.Text = "";
            if (info.LastIP.Equals(System.Net.IPAddress.None))
            {
                textBox1.Text += String.Format("About {0}: \r\n Never seen before.", info.ClassyName);

            }
            else
            {
                if (info != null)
                {
                    TimeSpan idle = info.PlayerObject.IdleTime;
                    if (info.IsHidden)
                    {
                        if (idle.TotalMinutes > 2)
                        {
                            if (player.Can(Permission.ViewPlayerIPs))
                            {
                                textBox1.Text += String.Format("About {0}: HIDDEN from {1} (idle {2})\r\n",
                                                info.Name,
                                                info.LastIP,
                                                idle.ToMiniString());
                            }
                            else
                            {
                                textBox1.Text += String.Format("About {0}:\r\n HIDDEN (idle {1})\r\n",
                                                info.Name,
                                                idle.ToMiniString());
                            }
                        }
                        else
                        {
                            if (player.Can(Permission.ViewPlayerIPs))
                            {
                                textBox1.Text += String.Format("About {0}:\r\n HIDDEN. Online from {1}\r\n",
                                                info.Name,
                                                info.LastIP);
                            }
                            else
                            {
                                textBox1.Text += String.Format("About {0}: HIDDEN.\r\n",
                                                info.Name);
                            }
                        }
                    }
                    else
                    {
                        if (idle.TotalMinutes > 1)
                        {
                            if (player.Can(Permission.ViewPlayerIPs))
                            {
                                textBox1.Text += String.Format("About {0}:\r\n Online now from {1} (idle {2})\r\n",
                                                info.Name,
                                                info.LastIP,
                                                idle.ToMiniString());
                            }
                            else
                            {
                                textBox1.Text += String.Format("About {0}:\r\n Online now (idle {1})\r\n",
                                                info.Name,
                                                idle.ToMiniString());
                            }
                        }
                        else
                        {
                            if (player.Can(Permission.ViewPlayerIPs))
                            {
                                textBox1.Text += String.Format("About {0}:\r\n Online now from {1}\r\n",
                                                info.Name,
                                                info.LastIP);
                            }
                            else
                            {
                                textBox1.Text += String.Format("About {0}:\r\n Online now.\r\n",
                                                info.Name);
                            }
                        }
                    }
                }
                else
                {
                    if (player.Can(Permission.ViewPlayerIPs))
                    {
                        if (info.LeaveReason != LeaveReason.Unknown)
                        {
                            textBox1.Text += String.Format("About {0}:\r\n Last seen {1} ago from {2} ({3}).\r\n",
                                            info.Name,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP,
                                            info.LeaveReason);
                        }
                        else
                        {
                            textBox1.Text += String.Format("About {0}:\r\n Last seen {1} ago from {2}.\r\n",
                                            info.Name,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP);
                        }
                    }
                    else
                    {
                        if (info.LeaveReason != LeaveReason.Unknown)
                        {
                            textBox1.Text += String.Format("About {0}:\r\n Last seen {1} ago ({2}).\r\n",
                                            info.Name,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LeaveReason);
                        }
                        else
                        {
                            textBox1.Text += String.Format("About {0}:\r\n Last seen {1} ago.\r\n",
                                            info.Name,
                                            info.TimeSinceLastSeen.ToMiniString());
                        }
                    }
                }
                // Show login information
                textBox1.Text += String.Format("  Logged in {0} time(s) since {1:d MMM yyyy}.\r\n",
                                info.TimesVisited,
                                info.FirstLoginDate);
            }

            if (info.IsFrozen)
            {
                textBox1.Text += String.Format("  Frozen {0} ago by {1}\r\n",
                                info.TimeSinceFrozen.ToMiniString(),
                                info.FrozenByClassy);
            }

            if (info.IsMuted)
            {
                textBox1.Text += String.Format("  Muted for {0} by {1}\r\n",
                                info.TimeMutedLeft.ToMiniString(),
                                info.MutedByClassy);
                float blocks = ((info.BlocksBuilt + info.BlocksDrawn) - info.BlocksDeleted);
                if (blocks < 0)
                    textBox1.Text += String.Format("  &CWARNING! {0} has deleted more than built!\r\n", info.ClassyName);//<---- LinkLoftWing, lul
            }

            // Show ban information
            IPBanInfo ipBan = IPBanList.Get(info.LastIP);
            switch (info.BanStatus)
            {
                case BanStatus.Banned:
                    if (ipBan != null)
                    {
                        textBox1.Text += String.Format("  Account and IP are BANNED. See /BanInfo\r\n");
                    }
                    else
                    {
                        textBox1.Text += String.Format("  Account is BANNED. See /BanInfo\r\n");
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if (ipBan != null)
                    {
                        textBox1.Text += String.Format("  IP is BANNED, but account is exempt. See /BanInfo\r\n");
                    }
                    else
                    {
                        textBox1.Text += String.Format("  IP is not banned, and account is exempt. See /BanInfo\r\n");
                    }
                    break;
                case BanStatus.NotBanned:
                    if (ipBan != null)
                    {
                        textBox1.Text += String.Format("  IP is BANNED. See /BanInfo\r\n");

                    }
                    break;
            }


            if (!info.LastIP.Equals(System.Net.IPAddress.None))
            {
                // Show alts
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach (PlayerInfo playerFromSameIP in PlayerDB.FindPlayers(info.LastIP))
                {
                    if (playerFromSameIP == info) continue;
                    altNames.Add(playerFromSameIP);
                    if (playerFromSameIP.IsBanned)
                    {
                        bannedAltCount++;
                    }
                }


                // Stats
                if (info.BlocksDrawn > 500000000)
                {
                    textBox1.Text += String.Format("  Built {0} and deleted {1} blocks, drew {2}M blocks, wrote {3} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.BlocksDrawn / 1000000,
                                    info.MessagesWritten);
                }
                else if (info.BlocksDrawn > 500000)
                {
                    textBox1.Text += String.Format("  Built {0} and deleted {1} blocks, drew {2}K blocks, wrote {3} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.BlocksDrawn / 1000,
                                    info.MessagesWritten);
                }
                else if (info.BlocksDrawn > 0)
                {
                    textBox1.Text += String.Format("  Built {0} and deleted {1} blocks, drew {2} blocks, wrote {3} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.BlocksDrawn,
                                    info.MessagesWritten);
                }
                else
                {
                    textBox1.Text += String.Format("  Built {0} and deleted {1} blocks, wrote {2} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.MessagesWritten);
                }


                // More stats
                if (info.TimesBannedOthers > 0 || info.TimesKickedOthers > 0 || info.PromoCount > 0)
                {
                    textBox1.Text += String.Format("  Kicked {0}, Promoted {1} and banned {2} players.\r\n", info.TimesKickedOthers, info.PromoCount, info.TimesBannedOthers);
                }

                if (info.TimesKicked > 0)
                {
                    if (info.LastKickDate != DateTime.MinValue)
                    {
                        textBox1.Text += String.Format("  Got kicked {0} times. Last kick {1} ago by {2}\r\n",
                                        info.TimesKicked,
                                        info.TimeSinceLastKick.ToMiniString(),
                                        info.LastKickByClassy);
                    }
                    else
                    {
                        textBox1.Text += String.Format("  Got kicked {0} times.\r\n", info.TimesKicked);
                    }
                    if (info.LastKickReason != null)
                    {
                        textBox1.Text += String.Format("  Kick reason: {0}\r\n", info.LastKickReason);
                    }
                }


                // Promotion/demotion
                if (info.PreviousRank == null)
                {
                    if (info.RankChangedBy == null)
                    {
                        textBox1.Text += String.Format("  Rank is {0} (default).\r\n",
                                        info.Rank.Name);
                    }
                    else
                    {
                        textBox1.Text += String.Format("  Promoted to {0} by {1} {2} ago.\r\n",
                                        info.Rank.Name,
                                        info.RankChangedByClassy,
                                        info.TimeSinceRankChange.ToMiniString());
                        if (info.RankChangeReason != null)
                        {
                            textBox1.Text += String.Format("  Promotion reason: {0}\r\n", info.RankChangeReason);
                        }
                    }
                }
                else if (info.PreviousRank <= info.Rank)
                {
                    textBox1.Text += String.Format("  Promoted from {0} to {1} by {2} {3} ago.\r\n",
                                    info.PreviousRank.Name,
                                    info.Rank.Name,
                                    info.RankChangedByClassy,
                                    info.TimeSinceRankChange.ToMiniString());
                    if (info.RankChangeReason != null)
                    {
                        textBox1.Text += String.Format("  Promotion reason: {0}\r\n", info.RankChangeReason);
                    }
                }
                else
                {
                    textBox1.Text += String.Format("  Demoted from {0} to {1} by {2} {3} ago.\r\n",
                                    info.PreviousRank.Name,
                                    info.Rank.Name,
                                    info.RankChangedByClassy,
                                    info.TimeSinceRankChange.ToMiniString());
                    if (info.RankChangeReason != null)
                    {
                        textBox1.Text += String.Format("  Demotion reason: {0}\r\n", info.RankChangeReason);
                    }
                }

                if (!info.LastIP.Equals(System.Net.IPAddress.None))
                {
                    // Time on the server
                    TimeSpan totalTime = info.TotalTime;
                    if (info != null)
                    {
                        totalTime = totalTime.Add(info.TimeSinceLastLogin);
                    }
                    textBox1.Text += String.Format("  Spent a total of {0:F1} hours ({1:F1} minutes) here.\r\n",
                                    totalTime.TotalHours,
                                    totalTime.TotalMinutes);
                }
            }
        }

        #region PlayerViewerbuttons
        private void PMButton_Click(object sender, EventArgs e)
        {
            if (player.IsOnline)
            {
                if (MTextBox.Text.Trim() != "")
                {
                    Chat.SendPM(Player.Console, player.PlayerObject, MTextBox.Text);
                    MTextBox.Text = "";
                    return;
                }
                else
                {
                    MessageBox.Show("Please insert text to PM towards a player!");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Player has logged off. Exiting Player Viewer.");
                this.Close();
            }
        }
        /*private void KickButton_Click(object sender, EventArgs e)
        {
            player.PlayerObject.Kick(Player.Console, MTextBox.Text, LeaveReason.Kick, true, true, true);
        }
        private void BanButton_Click(object sender, EventArgs e)
        {
            player.Ban(Player.Console, MTextBox.Text, true, true);
        } */
    
        #endregion

        #region design
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlayerViewer));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PMButton = new System.Windows.Forms.Button();
            this.MTextBox = new System.Windows.Forms.TextBox();
            this.ReasonLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(15, 27);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(219, 201);
            this.textBox1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("MS Reference Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Player Information";
            // 
            // PMButton
            // 
            this.PMButton.Location = new System.Drawing.Point(86, 319);
            this.PMButton.Name = "PMButton";
            this.PMButton.Size = new System.Drawing.Size(73, 20);
            this.PMButton.TabIndex = 2;
            this.PMButton.Text = "Send";
            this.PMButton.UseVisualStyleBackColor = true;
            this.PMButton.Click += new System.EventHandler(this.PMButton_Click);
            // 
            // MTextBox
            // 
            this.MTextBox.Location = new System.Drawing.Point(15, 267);
            this.MTextBox.Multiline = true;
            this.MTextBox.Name = "MTextBox";
            this.MTextBox.Size = new System.Drawing.Size(219, 46);
            this.MTextBox.TabIndex = 5;
            // 
            // ReasonLabel
            // 
            this.ReasonLabel.AutoSize = true;
            this.ReasonLabel.Font = new System.Drawing.Font("MS Reference Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReasonLabel.Location = new System.Drawing.Point(10, 249);
            this.ReasonLabel.Name = "ReasonLabel";
            this.ReasonLabel.Size = new System.Drawing.Size(144, 15);
            this.ReasonLabel.TabIndex = 6;
            this.ReasonLabel.Text = "Send a Private Message";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 231);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(229, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "_____________________________________";
            // 
            // PlayerViewer
            // 
            this.BackColor = System.Drawing.Color.Firebrick;
            this.ClientSize = new System.Drawing.Size(251, 345);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ReasonLabel);
            this.Controls.Add(this.MTextBox);
            this.Controls.Add(this.PMButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PlayerViewer";
            this.Text = "PlayerViewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
    }
}

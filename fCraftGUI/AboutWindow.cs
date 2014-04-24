// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace fCraft.GUI {
    public sealed partial class AboutWindow : Form {
        public AboutWindow() {
            InitializeComponent();
            lSubheader.Text = String.Format( lSubheader.Text, Updater.LatestStable );
        }
        private void lfCraft_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e ) {
            try {
                Process.Start( "http://www.fcraft.net" );
            } catch { }
        }

        private void l800Craft_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/GlennMR/800craft");
            }
            catch { }
        }

        private void lLegendCraft_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/LegendCraft/LegendCraft");
            }
            catch { }

        }

    }
}
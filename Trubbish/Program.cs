using System;
using System.IO;
using System.Windows.Forms;
using Shell32;
using Trubbish.Properties;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace Trubbish
{
    static class Program
    {
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "Trubbish";
            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                return;
            }
            Notify icon = new Notify();
            Pixel pixel = new Pixel();
            if (File.Exists("config.ini"))
            {
                foreach (string line in File.ReadAllLines("config.ini"))
                {
                    if (line.ToLower().StartsWith("full="))
                    {
                        if (File.Exists(line.Substring(5)))
                        {
                            icon.icons[1] = Icon.ExtractAssociatedIcon(line.Substring(5));
                        }
                    }
                    if (line.ToLower().StartsWith("empty="))
                    {
                        if (File.Exists(line.Substring(6)))
                        {
                            icon.icons[0] = Icon.ExtractAssociatedIcon(line.Substring(6));
                        }
                    }
                    if (line =="[NO-PIXEL]")
                    {
                        pixel.Close();
                    }
                }
                icon.change();
            }
            Application.Run();
        }
    }
    class Notify
    {
        static Shell shell = new Shell();
        static Folder recycleBin = shell.NameSpace(10);
        NotifyIcon notify = new NotifyIcon();
        ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripMenuItem mItemQuit = new ToolStripMenuItem("Quit");
        ToolStripMenuItem mItemLogo = new ToolStripMenuItem("Trubbish's Trash", Resources.trubbishgif);
        ToolStripMenuItem mItemClear = new ToolStripMenuItem("Clear");
        public Icon[] icons = { Resources.trubbish, Resources.trubbish_shiny };
        public Notify()
        {
            mItemLogo.Click += notifyIcon_DoubleClick;
            notify.Icon = Resources.trubbish;
            notify.Text = "Trubbish's Trash";
            notify.Visible = true;
            notify.MouseDoubleClick += notifyIcon_DoubleClick;
            menu.Items.Add(mItemLogo);
            menu.Items.Add("-");
            mItemClear.Click += mItemClear_Click;
            menu.Items.Add(mItemClear);
            mItemQuit.Click += mItemQuit_Click;
            menu.Items.Add(mItemQuit);
            notify.MouseClick += beforeShow;
            notify.ContextMenuStrip = menu;
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (Directory.Exists($@"{drive.ToString()}$Recycle.Bin"))
                {
                    FileSystemWatcher fs = new FileSystemWatcher($@"{drive.ToString()}$Recycle.Bin");
                    fs.EnableRaisingEvents = true;
                    fs.IncludeSubdirectories = true;
                    fs.Changed += watcher_Changed;
                }
            }
        }
        public void change()
        {
            notify.Icon = recycleBin.Items().Count >= 1 ? icons[1] : icons[0];
        }
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            change();
        }
        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Process.Start("shell:RecycleBinFolder");
        }
        private void mItemQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void mItemClear_Click(object sender, EventArgs e)
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, 0);
        }
        private void beforeShow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mItemClear.Enabled = recycleBin.Items().Count >= 1;
                notify.ContextMenuStrip.Show();
            }
        }
        enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);
    }
    class Pixel : Form
    {
        static Folder recycleBin = new Shell().NameSpace(10);
        ToolTip tip = new ToolTip();
        public Pixel()
        {
            this.ShowInTaskbar = false;
            this.AllowDrop = true;
            this.Visible = false;
            this.Opacity = .01;
            this.MinimumSize = new Size(1, 1);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.TopLevel = true;
            this.Shown += shown;
            this.DragDrop += dragDrop;
            this.DragEnter += dragEnter;
            tip.ShowAlways = true;
            tip.SetToolTip(this, "Bring folders/files");
            this.Show();
        }
        private void shown(object sender, EventArgs e)
        {
            this.Width = 1;
            this.Height = 1;
        }

        private void dragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Move;
            }
        }
        private void dragDrop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string item in fileList)
            {
                recycleBin.MoveHere(item);
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
    }
}

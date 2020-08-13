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
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        /// 



        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "MyAppName";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                //app is already running! Exiting the application  
                return;
            }
            SomeClass sc = new SomeClass();
            Application.Run();
        }
    }
    class SomeClass
    {
        static Shell shell = new Shell();
        Folder recycleBin = shell.NameSpace(10);
        NotifyIcon notify = new NotifyIcon();
        ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripMenuItem mItemQuit = new ToolStripMenuItem("Quit");
        ToolStripMenuItem mItemLogo = new ToolStripMenuItem("Trubbish's Trash", Resources.trubbish1);
        ToolStripMenuItem mItemClear = new ToolStripMenuItem("Clear");
        ToolTip tip = new ToolTip();

        Form form = new Form();



        public SomeClass()
        {
            form.Icon = Resources.trubbish;
            form.Visible = false;
            form.ShowInTaskbar = false;
            form.AllowDrop = true;           
            form.Opacity = .01;
            form.MinimumSize = new Size(10, 45);
            form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            form.StartPosition = FormStartPosition.Manual;
            form.TopMost = true;
            form.TopLevel = true;
            form.Location = new Point(-33, -36);

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
                FileSystemWatcher fs = new FileSystemWatcher($@"{drive.ToString()}$Recycle.Bin");
                fs.EnableRaisingEvents = true;
                fs.IncludeSubdirectories = true;
                fs.Changed += watcher_Changed;
            }
            changed();

            tip.ShowAlways = true;
            tip.SetToolTip(form, "Bring folders/files");

            form.DragEnter += dragEnter;
            form.DragDrop += DragDrop;
            form.Load += onLoad;
            form.ShowDialog();
        }
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            changed();
        }
        private void changed()
        {
            notify.Icon = recycleBin.Items().Count >= 1 ? Resources.trubbish_shiny : Resources.trubbish;
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
        private void onLoad(object sender, EventArgs e)
        {
            form.Width = 1;
            form.Height = 1;
        }

        private void dragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (!e.Data.GetDataPresent(DataFormats.Text))
                {
                    e.Effect = DragDropEffects.Move;
                }
            }
        }

        private void DragDrop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string item in fileList)
            {
                recycleBin.MoveHere(item);
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

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr window, int index);


        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;
    }
}

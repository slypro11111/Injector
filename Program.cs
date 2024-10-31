using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using System.Drawing;

namespace FolderCopyApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        private string folderToCopy;
        private TextBox folderPathTextBox;
        private Button browseButton;
        private NotifyIcon trayIcon;
        private Label keybindsLabel;
        private DateTime lastToggleTime;
        private const int toggleDelayMilliseconds = 500;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private Button minimizeButton;
        private Button closeButton;

        public Form1()
        {
            Initialize();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += new EventHandler(CheckKeyPress);
            timer.Start();

            if (!IsRunningAsAdmin())
            {
                MessageBox.Show("If this program does not work properly, try running as Admin :)");
            }

            lastToggleTime = DateTime.Now.AddMilliseconds(-toggleDelayMilliseconds);
        }

        private bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Initialize()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Injectaa";
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;

            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);

            folderPathTextBox = new TextBox
            {
                Width = 600,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            this.Controls.Add(folderPathTextBox);

            browseButton = new Button
            {
                Text = "Browse",
                Width = 100,
                Height = 30,
                ForeColor = Color.White,
                BackColor = Color.DarkSlateGray
            };
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            keybindsLabel = new Label
            {
                Text = "Keybinds:\nCtrl + Shift + C: Copy folder\nCtrl + Shift + T: Toggle window",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            this.Controls.Add(keybindsLabel);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false,
                Text = "Injectaa"
            };
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            minimizeButton = new Button
            {
                Text = "−",
                Width = 30,
                Height = 30,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Click += MinimizeButton_Click;
            this.Controls.Add(minimizeButton);

            closeButton = new Button
            {
                Text = "×",
                Width = 30,
                Height = 30,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += CloseButton_Click;
            this.Controls.Add(closeButton);

            CenterUI();

            this.Resize += new EventHandler(Form1_Resize);

            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, 20, 20, 180, 90);
            path.AddArc(this.ClientSize.Width - 20, 0, 20, 20, 270, 90);
            path.AddArc(this.ClientSize.Width - 20, this.ClientSize.Height - 20, 20, 20, 0, 90);
            path.AddArc(0, this.ClientSize.Height - 20, 20, 20, 90, 90);
            path.CloseAllFigures();
            this.Region = new Region(path);
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    folderPathTextBox.Text = folderBrowser.SelectedPath;
                    folderToCopy = folderBrowser.SelectedPath;
                }
            }
        }

        private void CheckKeyPress(object sender, EventArgs e)
        {
            if (GetAsyncKeyState(Keys.ControlKey) < 0 &&
                GetAsyncKeyState(Keys.ShiftKey) < 0 &&
                GetAsyncKeyState(Keys.C) < 0)
            {
                if (!string.IsNullOrEmpty(folderToCopy))
                {
                    //C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag MAIN DIR
                    string destinationFolder = @"C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag";
                    string folderName = Path.GetFileName(folderToCopy);
                    string destinationPath = Path.Combine(destinationFolder, folderName);
                    CopyFolder(folderToCopy, destinationPath);
                }
            }

            if (GetAsyncKeyState(Keys.ControlKey) < 0 &&
                GetAsyncKeyState(Keys.ShiftKey) < 0 &&
                GetAsyncKeyState(Keys.T) < 0)
            {
                if ((DateTime.Now - lastToggleTime).TotalMilliseconds > toggleDelayMilliseconds)
                {
                    if (this.Visible)
                    {
                        HideForm();
                    }
                    else
                    {
                        ShowForm();
                    }
                    lastToggleTime = DateTime.Now;
                }
            }
        }

        private void HideForm()
        {
            Hide();
            trayIcon.Visible = true;
        }

        private void ShowForm()
        {
            Show();
            WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
        }

        private void CopyFolder(string sourceFolder, string destinationFolder)
        {
            try
            {
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                foreach (string file in Directory.GetFiles(sourceFolder))
                {
                    string destFile = Path.Combine(destinationFolder, Path.GetFileName(file));
                    File.Copy(file, destFile, true);

                }

                foreach (string dir in Directory.GetDirectories(sourceFolder))
                {
                    string destDir = Path.Combine(destinationFolder, Path.GetFileName(dir));
                    CopyFolder(dir, destDir);
                }


                DirectoryInfo directoryInfo = new DirectoryInfo(destinationFolder);
                directoryInfo.Attributes |= FileAttributes.Hidden;
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }
        }

        private void CenterUI()
        {
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            int shiftLeft = 50;

            folderPathTextBox.Location = new Point((formWidth - folderPathTextBox.Width) / 2 - shiftLeft, formHeight / 2 - 50);
            browseButton.Location = new Point(folderPathTextBox.Right + 10, folderPathTextBox.Top);
            keybindsLabel.Location = new Point((formWidth - keybindsLabel.Width) / 2 - shiftLeft, folderPathTextBox.Bottom + 20);

            minimizeButton.Location = new Point(formWidth - 70, 10);
            closeButton.Location = new Point(formWidth - 35, 10);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            CenterUI();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawString("Injectaa", new Font("Arial", 14), Brushes.White, new PointF(10, 10));
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }
    }
}

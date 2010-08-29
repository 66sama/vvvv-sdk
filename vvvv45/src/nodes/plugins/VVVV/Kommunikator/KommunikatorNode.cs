#region usings
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Net;
using System.IO;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Crypto;
using VVVV.Utils.ManagedVCL;
#endregion usings

namespace VVVV.Nodes.Kommunikator
{
    [PluginInfo(Name = "Kommunikator",
                Category = "VVVV",
                Ignore = true,
                Author = "vvvv group",
                Help = "Communicator to vvvv.org",
                InitialBoxWidth = 400,
                InitialBoxHeight = 300,
                InitialWindowWidth = 500,
                InitialWindowHeight = 400,
                InitialComponentMode = TComponentMode.InAWindow)]
    public class KommunikatorPluginNode: TopControl, IKommunikator
    {
        #region field declaration
        
        //the host (mandatory)
        [Import]
        protected IKommunikatorHost FKommunikatorHost;
        // Track whether Dispose has been called.
        private bool FDisposed = false;
        
        private StringHasher FStringHasher = new StringHasher();
        private Graphics FOverlay;
        private Image FOriginal;
        private Point FMouseDownPoint, FMouseCurrentPoint;
        private bool FDrawRect;
        private Rectangle FCropRect;
        private Rectangle FZoomedImage;
        private double FOriginalAspect;
        private double FPictureBoxAspect;
        private Pen FRectPen;
        
        private const int CHeaderWidth = 900;
        private const int CHeaderHeight = 200;
        
        private System.Uri CPictureUploadUri = new Uri("http://vvvv.org/web-api/picture-upload");
        
        #endregion field declaration
        
        #region constructor/destructor
        public KommunikatorPluginNode()
        {
            // The InitializeComponent() call is required for Windows Forms designer support.
            InitializeComponent();
            
            FRectPen = new Pen(Color.Black);
            FRectPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        }
        
        private void InitializeComponent()
        {
        	this.panelScreenshot = new System.Windows.Forms.Panel();
        	this.ScreenshotPictureBox = new System.Windows.Forms.PictureBox();
        	this.ScreenshotInfoLabel = new System.Windows.Forms.Label();
        	this.panel2 = new System.Windows.Forms.Panel();
        	this.ScreenshotDescriptionTextBox = new System.Windows.Forms.TextBox();
        	this.ScreenshotTitleTextBox = new System.Windows.Forms.TextBox();
        	this.panel1 = new System.Windows.Forms.Panel();
        	this.panel3 = new System.Windows.Forms.Panel();
        	this.UploadButton = new System.Windows.Forms.Button();
        	this.SaveButton = new System.Windows.Forms.Button();
        	this.CloseButton = new System.Windows.Forms.Button();
        	this.UseAsHeaderCheckBox = new System.Windows.Forms.CheckBox();
        	this.PasswordTextBox = new System.Windows.Forms.TextBox();
        	this.UsernameTextBox = new System.Windows.Forms.TextBox();
        	this.ConsoleTextBox = new System.Windows.Forms.TextBox();
        	this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
        	this.panelScreenshot.SuspendLayout();
        	((System.ComponentModel.ISupportInitialize)(this.ScreenshotPictureBox)).BeginInit();
        	this.panel2.SuspendLayout();
        	this.panel1.SuspendLayout();
        	this.panel3.SuspendLayout();
        	this.SuspendLayout();
        	// 
        	// panelScreenshot
        	// 
        	this.panelScreenshot.Controls.Add(this.ScreenshotPictureBox);
        	this.panelScreenshot.Controls.Add(this.ScreenshotInfoLabel);
        	this.panelScreenshot.Controls.Add(this.panel2);
        	this.panelScreenshot.Controls.Add(this.ConsoleTextBox);
        	this.panelScreenshot.Dock = System.Windows.Forms.DockStyle.Fill;
        	this.panelScreenshot.Location = new System.Drawing.Point(0, 0);
        	this.panelScreenshot.Name = "panelScreenshot";
        	this.panelScreenshot.Size = new System.Drawing.Size(489, 402);
        	this.panelScreenshot.TabIndex = 2;
        	// 
        	// ScreenshotPictureBox
        	// 
        	this.ScreenshotPictureBox.BackColor = System.Drawing.Color.Black;
        	this.ScreenshotPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
        	this.ScreenshotPictureBox.Cursor = System.Windows.Forms.Cursors.Cross;
        	this.ScreenshotPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
        	this.ScreenshotPictureBox.InitialImage = null;
        	this.ScreenshotPictureBox.Location = new System.Drawing.Point(0, 0);
        	this.ScreenshotPictureBox.Name = "ScreenshotPictureBox";
        	this.ScreenshotPictureBox.Size = new System.Drawing.Size(489, 279);
        	this.ScreenshotPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        	this.ScreenshotPictureBox.TabIndex = 3;
        	this.ScreenshotPictureBox.TabStop = false;
        	this.ScreenshotPictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureBoxScreenshotMouseMove);
        	this.ScreenshotPictureBox.Resize += new System.EventHandler(this.PictureBoxScreenshotResize);
        	this.ScreenshotPictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PictureBoxScreenshotMouseDown);
        	this.ScreenshotPictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PictureBoxScreenshotMouseUp);
        	// 
        	// ScreenshotInfoLabel
        	// 
        	this.ScreenshotInfoLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
        	this.ScreenshotInfoLabel.Location = new System.Drawing.Point(0, 279);
        	this.ScreenshotInfoLabel.Name = "ScreenshotInfoLabel";
        	this.ScreenshotInfoLabel.Size = new System.Drawing.Size(489, 18);
        	this.ScreenshotInfoLabel.TabIndex = 10;
        	this.ScreenshotInfoLabel.Text = "ScreenshotInfo";
        	// 
        	// panel2
        	// 
        	this.panel2.Controls.Add(this.ScreenshotDescriptionTextBox);
        	this.panel2.Controls.Add(this.ScreenshotTitleTextBox);
        	this.panel2.Controls.Add(this.panel1);
        	this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
        	this.panel2.Location = new System.Drawing.Point(0, 297);
        	this.panel2.Name = "panel2";
        	this.panel2.Size = new System.Drawing.Size(489, 85);
        	this.panel2.TabIndex = 9;
        	// 
        	// ScreenshotDescriptionTextBox
        	// 
        	this.ScreenshotDescriptionTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        	this.ScreenshotDescriptionTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        	this.ScreenshotDescriptionTextBox.Location = new System.Drawing.Point(0, 20);
        	this.ScreenshotDescriptionTextBox.Multiline = true;
        	this.ScreenshotDescriptionTextBox.Name = "ScreenshotDescriptionTextBox";
        	this.ScreenshotDescriptionTextBox.Size = new System.Drawing.Size(344, 65);
        	this.ScreenshotDescriptionTextBox.TabIndex = 1;
        	// 
        	// ScreenshotTitleTextBox
        	// 
        	this.ScreenshotTitleTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        	this.ScreenshotTitleTextBox.Dock = System.Windows.Forms.DockStyle.Top;
        	this.ScreenshotTitleTextBox.Location = new System.Drawing.Point(0, 0);
        	this.ScreenshotTitleTextBox.Name = "ScreenshotTitleTextBox";
        	this.ScreenshotTitleTextBox.Size = new System.Drawing.Size(344, 20);
        	this.ScreenshotTitleTextBox.TabIndex = 0;
        	// 
        	// panel1
        	// 
        	this.panel1.Controls.Add(this.panel3);
        	this.panel1.Controls.Add(this.UseAsHeaderCheckBox);
        	this.panel1.Controls.Add(this.PasswordTextBox);
        	this.panel1.Controls.Add(this.UsernameTextBox);
        	this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
        	this.panel1.Location = new System.Drawing.Point(344, 0);
        	this.panel1.Name = "panel1";
        	this.panel1.Size = new System.Drawing.Size(145, 85);
        	this.panel1.TabIndex = 9;
        	// 
        	// panel3
        	// 
        	this.panel3.Controls.Add(this.UploadButton);
        	this.panel3.Controls.Add(this.SaveButton);
        	this.panel3.Controls.Add(this.CloseButton);
        	this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
        	this.panel3.Location = new System.Drawing.Point(0, 63);
        	this.panel3.Name = "panel3";
        	this.panel3.Size = new System.Drawing.Size(145, 22);
        	this.panel3.TabIndex = 17;
        	// 
        	// UploadButton
        	// 
        	this.UploadButton.Dock = System.Windows.Forms.DockStyle.Fill;
        	this.UploadButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        	this.UploadButton.Location = new System.Drawing.Point(0, 0);
        	this.UploadButton.Name = "UploadButton";
        	this.UploadButton.Size = new System.Drawing.Size(55, 22);
        	this.UploadButton.TabIndex = 5;
        	this.UploadButton.Text = "Upload";
        	this.UploadButton.UseVisualStyleBackColor = true;
        	this.UploadButton.Click += new System.EventHandler(this.UploadButtonClick);
        	// 
        	// SaveButton
        	// 
        	this.SaveButton.Dock = System.Windows.Forms.DockStyle.Right;
        	this.SaveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        	this.SaveButton.Location = new System.Drawing.Point(55, 0);
        	this.SaveButton.Name = "SaveButton";
        	this.SaveButton.Size = new System.Drawing.Size(45, 22);
        	this.SaveButton.TabIndex = 6;
        	this.SaveButton.Text = "Save";
        	this.SaveButton.UseVisualStyleBackColor = true;
        	this.SaveButton.Click += new System.EventHandler(this.SaveButtonClick);
        	// 
        	// CloseButton
        	// 
        	this.CloseButton.Dock = System.Windows.Forms.DockStyle.Right;
        	this.CloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        	this.CloseButton.Location = new System.Drawing.Point(100, 0);
        	this.CloseButton.Name = "CloseButton";
        	this.CloseButton.Size = new System.Drawing.Size(45, 22);
        	this.CloseButton.TabIndex = 7;
        	this.CloseButton.Text = "Close";
        	this.CloseButton.UseVisualStyleBackColor = true;
        	this.CloseButton.Click += new System.EventHandler(this.CloseButtonClick);
        	// 
        	// UseAsHeaderCheckBox
        	// 
        	this.UseAsHeaderCheckBox.Dock = System.Windows.Forms.DockStyle.Top;
        	this.UseAsHeaderCheckBox.Enabled = false;
        	this.UseAsHeaderCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        	this.UseAsHeaderCheckBox.Location = new System.Drawing.Point(0, 40);
        	this.UseAsHeaderCheckBox.Name = "UseAsHeaderCheckBox";
        	this.UseAsHeaderCheckBox.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
        	this.UseAsHeaderCheckBox.Size = new System.Drawing.Size(145, 21);
        	this.UseAsHeaderCheckBox.TabIndex = 4;
        	this.UseAsHeaderCheckBox.Text = "use image as header";
        	this.UseAsHeaderCheckBox.UseVisualStyleBackColor = true;
        	this.UseAsHeaderCheckBox.CheckedChanged += new System.EventHandler(this.UseAsHeaderCheckBoxCheckedChanged);
        	// 
        	// PasswordTextBox
        	// 
        	this.PasswordTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        	this.PasswordTextBox.Dock = System.Windows.Forms.DockStyle.Top;
        	this.PasswordTextBox.Location = new System.Drawing.Point(0, 20);
        	this.PasswordTextBox.Name = "PasswordTextBox";
        	this.PasswordTextBox.PasswordChar = '*';
        	this.PasswordTextBox.Size = new System.Drawing.Size(145, 20);
        	this.PasswordTextBox.TabIndex = 3;
        	this.PasswordTextBox.Text = "guest";
        	// 
        	// UsernameTextBox
        	// 
        	this.UsernameTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        	this.UsernameTextBox.Dock = System.Windows.Forms.DockStyle.Top;
        	this.UsernameTextBox.Location = new System.Drawing.Point(0, 0);
        	this.UsernameTextBox.Name = "UsernameTextBox";
        	this.UsernameTextBox.Size = new System.Drawing.Size(145, 20);
        	this.UsernameTextBox.TabIndex = 2;
        	this.UsernameTextBox.Text = "guest";
        	// 
        	// ConsoleTextBox
        	// 
        	this.ConsoleTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        	this.ConsoleTextBox.Dock = System.Windows.Forms.DockStyle.Bottom;
        	this.ConsoleTextBox.Location = new System.Drawing.Point(0, 382);
        	this.ConsoleTextBox.Name = "ConsoleTextBox";
        	this.ConsoleTextBox.Size = new System.Drawing.Size(489, 20);
        	this.ConsoleTextBox.TabIndex = 11;
        	this.ConsoleTextBox.TabStop = false;
        	// 
        	// saveFileDialog
        	// 
        	this.saveFileDialog.Filter = "\"PNG (*.png)|*.png|All Files (*.*)|*.*";
        	this.saveFileDialog.Title = "Save Screenshot As...";
        	// 
        	// KommunikatorPluginNode
        	// 
        	this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
        	this.Controls.Add(this.panelScreenshot);
        	this.Name = "KommunikatorPluginNode";
        	this.Size = new System.Drawing.Size(489, 402);
        	this.panelScreenshot.ResumeLayout(false);
        	this.panelScreenshot.PerformLayout();
        	((System.ComponentModel.ISupportInitialize)(this.ScreenshotPictureBox)).EndInit();
        	this.panel2.ResumeLayout(false);
        	this.panel2.PerformLayout();
        	this.panel1.ResumeLayout(false);
        	this.panel1.PerformLayout();
        	this.panel3.ResumeLayout(false);
        	this.ResumeLayout(false);
        }
        private System.Windows.Forms.PictureBox ScreenshotPictureBox;
        private System.Windows.Forms.TextBox ScreenshotDescriptionTextBox;
        private System.Windows.Forms.TextBox PasswordTextBox;
        private System.Windows.Forms.TextBox UsernameTextBox;
        private System.Windows.Forms.Button UploadButton;
        private System.Windows.Forms.TextBox ScreenshotTitleTextBox;
        private System.Windows.Forms.Label ScreenshotInfoLabel;
        private System.Windows.Forms.CheckBox UseAsHeaderCheckBox;
        private System.Windows.Forms.TextBox ConsoleTextBox;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panelScreenshot;
        
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected override void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if(!FDisposed)
            {
                if(disposing)
                {
                    // Dispose managed resources.
                }
                // Release unmanaged resources. If disposing is false,
                // only the following code is executed.

                // Note that this is not thread safe.
                // Another thread could start disposing the object
                // after the managed resources are disposed,
                // but before the disposed flag is set to true.
                // If thread safety is necessary, it must be
                // implemented by the client.
            }
            FDisposed = true;
        }
        
        #endregion constructor/destructor

        #region IKommunikator
        public void Initialize(string title, string description)
        {
            ScreenshotTitleTextBox.Text = title;
            ScreenshotDescriptionTextBox.Text = description;
            
            FOriginal = Clipboard.GetImage();
            ScreenshotPictureBox.BackgroundImage = FOriginal;
            
            //create overlay image that holds the crop selection
            Image img = new Bitmap(FOriginal.Width, FOriginal.Height, PixelFormat.Format32bppArgb);
            FOverlay = Graphics.FromImage(img);
            ScreenshotPictureBox.Image = img;
            
            if ((FOriginal.Width >= CHeaderWidth) && (FOriginal.Height >= CHeaderHeight))
                UseAsHeaderCheckBox.Enabled = true;
            else
                UseAsHeaderCheckBox.Enabled = false;
            
            FOriginalAspect = FOriginal.Width / (double) FOriginal.Height;
            FCropRect = new Rectangle(0, 0, FOriginal.Width, FOriginal.Height);
            
            UpdateZoomedImageRect();
            UpdateScreenshotInfo();
        }
        
        public void SaveCurrentImage(string filename)
        {
            FOriginal = Clipboard.GetImage();
            SaveToFile(FOriginal, filename);
        }
        #endregion IKommunikator
        
        #region PictureBox
        void PictureBoxScreenshotMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                FDrawRect = true;
                FMouseDownPoint = e.Location;
            }
            else
            {
                FCropRect = new Rectangle(0, 0, FOriginal.Width, FOriginal.Height);
                UpdateScreenshotInfo();
                UpdateOverlay();
            }
        }
        
        void PictureBoxScreenshotMouseMove(object sender, MouseEventArgs e)
        {
            if (FDrawRect)
            {
                FMouseCurrentPoint = e.Location;
                UpdateOverlay();
            }
        }
        
        void PictureBoxScreenshotMouseUp(object sender, MouseEventArgs e)
        {
            FDrawRect = false;
        }
        
        void PictureBoxScreenshotResize(object sender, EventArgs e)
        {
            UpdateZoomedImageRect();
        }
        
        private void UpdateScreenshotInfo()
        {
            ScreenshotInfoLabel.Text = "Original: " + FOriginal.Width.ToString() + " x " + FOriginal.Height.ToString() + " Cropped: " + FCropRect.Width.ToString() + " x " + FCropRect.Height.ToString();
            ScreenshotInfoLabel.Invalidate();
        }
        
        private void UpdateZoomedImageRect()
        {
            int left, top, width, height;
            FPictureBoxAspect = ScreenshotPictureBox.Width / (double) ScreenshotPictureBox.Height;
            
            //aspect > 1 is landscape
            //aspect <= 1 is portrait
            
            if (FPictureBoxAspect > FOriginalAspect)
            {
                height = ScreenshotPictureBox.Height;
                width = (int)Math.Round(height * FOriginalAspect);
            }
            else
            {
                width = ScreenshotPictureBox.Width;
                height = (int)Math.Round(width / FOriginalAspect);
            }
            
            left = ScreenshotPictureBox.Width / 2 - width / 2;
            top = ScreenshotPictureBox.Height / 2 - height / 2;
            
            FZoomedImage = new Rectangle(left, top, width, height);
        }
        
        private void UpdateOverlay()
        {
            FOverlay.Clear(Color.Transparent);
            double xScale = FZoomedImage.Width / (double) FOriginal.Width;
            double yScale = FZoomedImage.Height / (double) FOriginal.Height;
            
            int left, top, width, height;
            if ((UseAsHeaderCheckBox.Enabled && UseAsHeaderCheckBox.Checked))
            {
                left = Math.Max(0, (int) ((FMouseCurrentPoint.X - FZoomedImage.Left) / xScale) - CHeaderWidth/2);
                left = Math.Min(left, FOriginal.Width - CHeaderWidth);
                top = Math.Max(0, (int) ((FMouseCurrentPoint.Y - FZoomedImage.Top) / yScale) - CHeaderHeight/2);
                top = Math.Min(top, FOriginal.Height - CHeaderHeight);
                width = CHeaderWidth;
                height = CHeaderHeight;
            }
            else
            {
                left = Math.Max(0, (int) ((FMouseDownPoint.X - FZoomedImage.Left) / xScale));
                top = Math.Max(0, (int) ((FMouseDownPoint.Y - FZoomedImage.Top) / yScale));
                width = Math.Max(10, (int) ((FMouseCurrentPoint.X - FMouseDownPoint.X) / xScale));
                width = Math.Min(width, FOriginal.Width - left);
                height = Math.Max(10, (int) ((FMouseCurrentPoint.Y - FMouseDownPoint.Y) / yScale));
                height = Math.Min(height, FOriginal.Height - top);
            }
            
            FCropRect = new Rectangle(left, top, width, height);
            
            FOverlay.DrawRectangle(FRectPen, FCropRect);
            UpdateScreenshotInfo();
            ScreenshotPictureBox.Invalidate();
        }
        #endregion PictureBox
        
        private Bitmap CropImage()
        {
            Bitmap target = new Bitmap(FCropRect.Width, FCropRect.Height);
            using(Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(FOriginal, new Rectangle(0, 0, target.Width, target.Height), FCropRect, GraphicsUnit.Pixel);
            }
            
            return target;
        }
        
        private void SaveToFile(Image image, string filename)
        {
            image.Save(filename, ImageFormat.Png);  
        }
         
        void UploadButtonClick(object sender, EventArgs e)
        {
        	ConsoleTextBox.Text = "";
            
            //crop the image
            Bitmap target = CropImage();

            MemoryStream fileData = new MemoryStream();
            target.Save(fileData, System.Drawing.Imaging.ImageFormat.Png);
            fileData.Seek(0, SeekOrigin.Begin);

            //add post-header
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("name", UsernameTextBox.Text);
            nvc.Add("pass", FStringHasher.ToMD5(PasswordTextBox.Text));
            nvc.Add("header", (UseAsHeaderCheckBox.Enabled && UseAsHeaderCheckBox.Checked).ToString().ToLower());
            nvc.Add("title", ScreenshotTitleTextBox.Text);
            nvc.Add("description", ScreenshotDescriptionTextBox.Text);

            using (WebResponse response = Upload.PostFile(CPictureUploadUri, nvc, fileData, ScreenshotTitleTextBox.Text + ".png", null, null, null, null))
            {
                // the stream returned by WebResponse.GetResponseStream
                // will contain any content returned by the server after upload

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string result = reader.ReadToEnd();
                    if (result.Contains("OK"))
                        ConsoleTextBox.Text = "Upload Successful.";
                    else if (result.Contains("LOGIN FAILED"))
                        ConsoleTextBox.Text = "Login failed!";
                    else if (result.Contains("SERVER BUSY"))
                        ConsoleTextBox.Text = "Server is busy, please try again later.";
                    else
                        ConsoleTextBox.Text = "ERROR: " + result;
                }
            }
        }
        
        void SaveButtonClick(object sender, EventArgs e)
        {
        	string filename = ScreenshotTitleTextBox.Text;
            char[] invalids = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalids)
                filename = filename.Replace(c, '_');
            saveFileDialog.FileName = filename;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Bitmap target = CropImage();
                SaveToFile(target, saveFileDialog.FileName);
            }
        }
        
        void CloseButtonClick(object sender, EventArgs e)
        {
        	FKommunikatorHost.HideMe();
        }
        
        void UseAsHeaderCheckBoxCheckedChanged(object sender, EventArgs e)
        {
        	if (UseAsHeaderCheckBox.Checked)
            {
                FMouseCurrentPoint = new Point(ScreenshotPictureBox.Width/2, ScreenshotPictureBox.Height/2);
                UpdateOverlay();
            }
        }
    }
}

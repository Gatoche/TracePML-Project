namespace Notify
{
    partial class frmToast
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            rctToast = new RoundedControl();
            pictureBox1 = new PictureBox();
            tmrShow = new System.Windows.Forms.Timer(components);
            tmrHide = new System.Windows.Forms.Timer(components);
            tmrWait = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // rctToast
            // 
            rctToast.CornerRadius = 50;
            rctToast.TargetControl = this;
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1200, 120);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // tmrShow
            // 
            tmrShow.Enabled = true;
            tmrShow.Interval = 10;
            tmrShow.Tick += tmrShow_Tick;
            // 
            // tmrHide
            // 
            tmrHide.Interval = 10;
            tmrHide.Tick += tmrHide_Tick;
            // 
            // tmrWait
            // 
            tmrWait.Interval = 5000;
            tmrWait.Tick += tmrWait_Tick;
            // 
            // frmToast
            // 
            AutoScaleDimensions = new SizeF(192F, 192F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.Control;
            BackgroundImageLayout = ImageLayout.None;
            ClientSize = new Size(1200, 120);
            ControlBox = false;
            Controls.Add(pictureBox1);
            Font = new Font("Tahoma", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmToast";
            ShowIcon = false;
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmTost";
            TopMost = true;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private RoundedControl rctToast;
        private System.Windows.Forms.Timer tmrShow;
        private System.Windows.Forms.Timer tmrHide;
        private System.Windows.Forms.Timer tmrWait;
        private PictureBox pictureBox1;
    }
}

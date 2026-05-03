namespace Notify
{
    partial class frmMain
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            button1 = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            panel1 = new Panel();
            pictureBox1 = new PictureBox();
            roundedControl1 = new RoundedControl();
            radioButton1 = new RadioButton();
            radioButton2 = new RadioButton();
            radioButton3 = new RadioButton();
            checkBox1 = new CheckBox();
            notifyIcon1 = new NotifyIcon(components);
            contextMenuStrip1 = new ContextMenuStrip(components);
            montrerGTracePMLToolStripMenuItem = new ToolStripMenuItem();
            quitterToolStripMenuItem = new ToolStripMenuItem();
            textBox1 = new TextBox();
            button2 = new Button();
            txbHook = new TextBox();
            tmrForegroundWindow = new System.Windows.Forms.Timer(components);
            btnClear = new Button();
            timer2 = new System.Windows.Forms.Timer(components);
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(45, 21);
            button1.Name = "button1";
            button1.Size = new Size(217, 46);
            button1.TabIndex = 0;
            button1.Text = "Test Pop-Up";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // timer1
            // 
            timer1.Interval = 10000;
            timer1.Tick += timer1_Tick;
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Control;
            panel1.Controls.Add(pictureBox1);
            panel1.Location = new Point(45, 103);
            panel1.Name = "panel1";
            panel1.Size = new Size(1200, 120);
            panel1.TabIndex = 2;
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1200, 120);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // roundedControl1
            // 
            roundedControl1.CornerRadius = 25;
            roundedControl1.TargetControl = panel1;
            // 
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.Location = new Point(83, 262);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new Size(252, 36);
            radioButton1.TabIndex = 4;
            radioButton1.Text = "Commande validée";
            radioButton1.UseVisualStyleBackColor = true;
            radioButton1.CheckedChanged += radioButton1_CheckedChanged;
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.Location = new Point(487, 262);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new Size(262, 36);
            radioButton2.TabIndex = 5;
            radioButton2.Text = "Commande partielle";
            radioButton2.UseVisualStyleBackColor = true;
            radioButton2.CheckedChanged += radioButton2_CheckedChanged;
            // 
            // radioButton3
            // 
            radioButton3.AutoSize = true;
            radioButton3.Location = new Point(912, 262);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new Size(262, 36);
            radioButton3.TabIndex = 6;
            radioButton3.Text = "Commande annulée";
            radioButton3.UseVisualStyleBackColor = true;
            radioButton3.CheckedChanged += radioButton3_CheckedChanged;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(912, 27);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(170, 36);
            checkBox1.TabIndex = 7;
            checkBox1.Text = "Thème clair";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "Notify";
            notifyIcon1.Visible = true;
            notifyIcon1.Click += notifyIcon1_Click;
            notifyIcon1.MouseClick += notifyIcon1_MouseClick;
            notifyIcon1.MouseDoubleClick += notifyIcon1_MouseDoubleClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(32, 32);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { montrerGTracePMLToolStripMenuItem, quitterToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(298, 80);
            contextMenuStrip1.Opening += contextMenuStrip1_Opening;
            // 
            // montrerGTracePMLToolStripMenuItem
            // 
            montrerGTracePMLToolStripMenuItem.Name = "montrerGTracePMLToolStripMenuItem";
            montrerGTracePMLToolStripMenuItem.Size = new Size(297, 38);
            montrerGTracePMLToolStripMenuItem.Text = "Montrer gTracePML";
            montrerGTracePMLToolStripMenuItem.Click += montrerGTracePMLToolStripMenuItem_Click;
            // 
            // quitterToolStripMenuItem
            // 
            quitterToolStripMenuItem.Name = "quitterToolStripMenuItem";
            quitterToolStripMenuItem.Size = new Size(297, 38);
            quitterToolStripMenuItem.Text = "Quitter";
            quitterToolStripMenuItem.Click += quitterToolStripMenuItem_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(45, 348);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(1200, 451);
            textBox1.TabIndex = 8;
            // 
            // button2
            // 
            button2.Location = new Point(314, 21);
            button2.Name = "button2";
            button2.Size = new Size(194, 46);
            button2.TabIndex = 9;
            button2.Text = "Test PML";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // txbHook
            // 
            txbHook.Location = new Point(1299, 103);
            txbHook.Multiline = true;
            txbHook.Name = "txbHook";
            txbHook.ScrollBars = ScrollBars.Vertical;
            txbHook.Size = new Size(825, 696);
            txbHook.TabIndex = 10;
            // 
            // tmrForegroundWindow
            // 
            tmrForegroundWindow.Enabled = true;
            tmrForegroundWindow.Tick += tmrForegroundWindow_Tick;
            // 
            // btnClear
            // 
            btnClear.Location = new Point(1309, 21);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(150, 46);
            btnClear.TabIndex = 11;
            btnClear.Text = "Effacer";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // timer2
            // 
            timer2.Interval = 1000;
            timer2.Tick += timer2_Tick;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(192F, 192F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.ControlLight;
            ClientSize = new Size(2165, 851);
            Controls.Add(btnClear);
            Controls.Add(txbHook);
            Controls.Add(button2);
            Controls.Add(textBox1);
            Controls.Add(checkBox1);
            Controls.Add(radioButton3);
            Controls.Add(radioButton2);
            Controls.Add(radioButton1);
            Controls.Add(panel1);
            Controls.Add(button1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmMain";
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.Manual;
            Text = "Notify";
            FormClosing += frmMain_FormClosing;
            FormClosed += frmMain_FormClosed;
            Load += frmMain_Load_1;
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private System.Windows.Forms.Timer timer1;
        private Panel panel1;
        private PictureBox pictureBox1;
        private RoundedControl roundedControl1;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
        private RadioButton radioButton3;
        private CheckBox checkBox1;
        private NotifyIcon notifyIcon1;
        private TextBox textBox1;
        private Button button2;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem quitterToolStripMenuItem;
        private ToolStripMenuItem montrerGTracePMLToolStripMenuItem;
        private TextBox txbHook;
        private System.Windows.Forms.Timer tmrForegroundWindow;
        private Button btnClear;
        private System.Windows.Forms.Timer timer2;
    }
}
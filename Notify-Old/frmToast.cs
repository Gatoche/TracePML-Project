

using Svg;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Svg;

//Text id tspan1 x-> 20 mais à centrer

//Ok
//fond vert :	fill: #44aa00	init form fill:228B22
//encre grise : 	fill: #4a4a4a;

//Modifié
//fond orange: fill: #FF9800
//encre grise : 	fill: #4a4a4a;

//Annulé
//fond rouge: fill: #ff4d00
//gris clair : 	fill: #dfdfdf

namespace Notify
{
    public partial class frmToast : Form
    {
        private SvgDocument _svgDoc;

        int toastX, toastY;
        int ScreenWidth = Screen.PrimaryScreen.WorkingArea.Width;
        //int ScreenHeight = Screen.PrimaryScreen.WorkingArea.Height;

        //public frmToast()
        //{
        //    InitializeComponent(); 
        //}

        public frmToast(SvgDocument svgDoc)
        {
            InitializeComponent();
            this._svgDoc = svgDoc;
        }

        private void tmrShow_Tick(object sender, EventArgs e)
        {
            toastY += 10;
            this.Opacity = (double)(toastY + 100) / 110;
            this.Location = new Point(toastX, toastY);
            if (toastY >= 10)
            {
                tmrShow.Stop();
                tmrWait.Start();
            }
        }

        private void tmrHide_Tick(object sender, EventArgs e)
        {
            toastY -= 10;
            this.Opacity = (double)(toastY + 100) / 110;
            this.Location = new Point(toastX, toastY);
            if (toastY <= -this.Height)
            {
                tmrHide.Stop();
                this.Close();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Position();

            pictureBox1.Image = _svgDoc.Draw();
        }

        private void Position()
        {
            rctToast.CornerRadius = (50 * this.Width / 1200);
            toastX = (ScreenWidth - this.Width) / 2;
            toastY = -this.Height;
            this.Location = new Point(toastX, toastY);
        }

        private void tmrWait_Tick(object sender, EventArgs e)
        {
            tmrWait.Stop();
            tmrHide.Start(); 
        }
    }
}

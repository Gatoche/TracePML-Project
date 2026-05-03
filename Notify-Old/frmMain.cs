using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Svg;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ExCSS;
using System.Collections;
using Svg.FilterEffects;
using Color = System.Drawing.Color;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Diagnostics;
using static Notify.frmMain;
using static System.Net.Mime.MediaTypeNames;
using static wipisoft.GlobalKeyboardHook;
using static wipisoft.WindowEnumerator;
using static Notify.CDMessage;
using wipisoft;

namespace Notify
{
    public partial class frmMain : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, String lpWindowName);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);


        public bool Closing;
        private const int WM_COPYDATA = 0x004A;
        private const int WM_QUERYENDSESSION = 0x0011;
        private const int WM_ENDSESSION = 0x0016;
        public uint gTracePML_SHOW_WM;

        public SvgDocument svg1 = SvgDocument.FromSvg<SvgDocument>(System.Text.Encoding.UTF8.GetString(Resource1.Notify_v3));
        public SvgDocument svg2 = SvgDocument.FromSvg<SvgDocument>(System.Text.Encoding.UTF8.GetString(Resource1.Notify_v3));

        //[StructLayout(LayoutKind.Sequential)]
        //public struct COPYDATASTRUCT
        //{
        //    public IntPtr dwData;
        //    public int cbData;
        //    public IntPtr lpData;
        //}

        //public enum OrderStatus
        //{
        //    osConfirmed,
        //    osModifed,
        //    osCancelled
        //}

        //[JsonObject(ItemRequired = Required.Always)]
        //private struct CommandeTransmise
        //{
        //    public OrderStatus Status;
        //    public string Title;
        //    public string Detail;
        //}

        //[JsonObject(ItemRequired = Required.Always)]
        //private struct SearchPDF
        //{
        //    public string Command;
        //    public string Value;
        //}

        // Hook wp
        private GlobalKeyboardHook _hook;
        private IntPtr hwndWinpharmaOrder = IntPtr.Zero;
        private IntPtr hwndWinpharmaCtrl645 = IntPtr.Zero; // Ligne affichant la sélection
        private IntPtr hwndWinpharmaCtrl643 = IntPtr.Zero; // Fenêtre des lignes produits
        private bool isWinpharmaOrderFG = false;
        private bool isWinpharmaOrderItemFocused = false;
        private bool isWinpharmaOrderEdit = false;
        private bool isOuiPDFSearchActive = false;
        private string OuiPDFSearchText = string.Empty;
        private const string SearchActiveTitleExt = " - [Recherche OuiPDF - Ctrl+F pour désactiver]";
        private string DefaultWinpharmaOrderTitle = string.Empty;

        public frmMain()
        {
            InitializeComponent();

            _hook = new GlobalKeyboardHook();
            _hook.KeyDown += Hook_KeyDown;

            gTracePML_SHOW_WM = RegisterWindowMessage("gTracePML_SHOW_WM");
            //MessageBox.Show(Convert.ToInt32(gTracePML_SHOW_WM).ToString());

            // Register the custom window class

            roundedControl1.CornerRadius = (50 * panel1.Width / 1200);

            // Validée
            radioButton1.Checked = true;

            //frmToast frm = new frmToast();
            //frm.Show();
        }

        private bool Hook_KeyDown(Keys key)
        {
            // Fenêtre de commande Winpharma active (focus)
            if (isWinpharmaOrderFG)
            {
                // CTRL+F
                if (key == Keys.F && (Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    //LogMessage("CTRL+F was pressed from Winpharma order window !");

                    if (!isOuiPDFSearchActive)
                    {
                        isOuiPDFSearchActive = true;
                        OuiPDFSearchText = string.Empty;
                        SetWindowText(hwndWinpharmaOrder, DefaultWinpharmaOrderTitle + SearchActiveTitleExt);
                        LogMessage("Recherche PDF activée");
                        // L'envoi du msg se fait dans le timer
                    }
                    else
                    {
                        isOuiPDFSearchActive = false;
                        SetWindowText(hwndWinpharmaOrder, DefaultWinpharmaOrderTitle);
                        LogMessage("Recherche PDF désactivée");

                        var search = new SearchPDF(
                             Command: "FindStop",
                             Value: "");
                        string jsonSearch = JsonConvert.SerializeObject(search);
                        SendCopyDataMessage(null, "OuiPDF", jsonSearch, 2);
                    }
                    return true; // (Handled) 
                }

                // Barre espace
                else if (key == Keys.Space && isOuiPDFSearchActive &&
                    isWinpharmaOrderItemFocused && !isWinpharmaOrderEdit)
                {
                    LogMessage("Recherche PDF: " + OuiPDFSearchText + " (actualisation / barre espace)");

                    // Actualisation de la recherche PDF
                    var search = new SearchPDF(
                            Command: "Find",
                            Value: OuiPDFSearchText);
                    string jsonSearch = JsonConvert.SerializeObject(search);
                    SendCopyDataMessage(null, "OuiPDF", jsonSearch, 2);
                    return true; // (Handled) 
                }

                // Flèche gauche ou droite
                else if ((key == Keys.Left || key == Keys.Right) && isOuiPDFSearchActive &&
                    isWinpharmaOrderItemFocused && !isWinpharmaOrderEdit)
                {
                    string searchDir = key == Keys.Left ? "Previous" : "Next";
                    LogMessage("Recherche PDF multi: " + searchDir);

                    // Actualisation de la recherche PDF
                    var search = new SearchPDF(
                            Command: "FindMulti",
                            Value: searchDir);
                    string jsonSearch = JsonConvert.SerializeObject(search);
                    SendCopyDataMessage(null, "OuiPDF", jsonSearch, 2);
                    return true; // (Handled) 
                }
            }

            return false; // (not Handled)
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_COPYDATA)
            {
                COPYDATASTRUCT cds = (COPYDATASTRUCT)m.GetLParam(typeof(COPYDATASTRUCT));
                string? jsonData = "";

                // dwData distingue le type de message (CommandeTransmise ou RechercheAuto)
                // et l'encodage (Unicode/C# app. ou AnsiChar/Delphi app.)
                switch (cds.dwData)
                {
                    case 0:
                    case 2: // UNICode
                        jsonData = Marshal.PtrToStringUni(cds.lpData);
                        break;

                    case 1: // ANSICHAR
                        jsonData = Marshal.PtrToStringUTF8(cds.lpData);
                        break;

                    default:
                        LogMessage("Message WM_COPYDATA non conforme (cds.dwData étape 1)"); // Invoke intégré
                        return;
                }

                // Désérialisation
                if (jsonData != null)
                {
                    LogMessage(jsonData);
                    try
                    {
                        switch (cds.dwData)
                        {
                            case 0:
                            case 1:
                                CommandeTransmise commandetransmise = JsonConvert.DeserializeObject<CommandeTransmise>(jsonData);
                                textBox1.AppendText(commandetransmise.Status.ToString() + Environment.NewLine);
                                textBox1.AppendText(commandetransmise.Title + Environment.NewLine);
                                textBox1.AppendText(commandetransmise.Detail + Environment.NewLine);

                                svg2 = UpdateSvg(svg2,
                                    commandetransmise.Status,
                                    commandetransmise.Title,
                                    commandetransmise.Detail
                                );
                                frmToast frm = new frmToast(svg2);
                                nint hwnd = GetForegroundWindow();
                                frm.Show();
                                SetForegroundWindow(hwnd.ToInt32());
                                break;

                            case 2:
                                SearchPDF searchpdf = JsonConvert.DeserializeObject<SearchPDF>(jsonData);
                                LogMessage("Command: " + searchpdf.Command + " Value: " + searchpdf.Value); // Invoke intégré
                                this.Invoke((MethodInvoker)delegate
                                {
                                    // TODO my own
                                });
                                break;

                            default:
                                LogMessage("Message WM_COPYDATA non conforme (cds.dwData étape 2)"); // Invoke intégré
                                return;
                        }
                    }
                    catch (Exception e)
                    {
                        LogMessage("Erreur réception WM_COPYDATA: " + e.Message + Environment.NewLine); // Invoke intégré
                    }
                }
            }
            else if (m.Msg == WM_QUERYENDSESSION || m.Msg == WM_ENDSESSION)
            {
                Closing = true;
                System.Windows.Forms.Application.Exit();
            }
        }

        //protected override void WndProc(ref Message m)
        //{
        //    switch (m.Msg)
        //    {
        //        case WM_COPYDATA:
        //            HandleCopyDataMessage(ref m);
        //            break;

        //        case WM_QUERYENDSESSION:
        //        case WM_ENDSESSION:
        //            HandleSessionEndMessage();
        //            break;

        //        default:
        //            base.WndProc(ref m);
        //            break;
        //    }
        //}

        //private void HandleCopyDataMessage(ref Message m)
        //{
        //    try
        //    {
        //        var cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT))!;

        //        string jsonData = cds.dwData switch
        //        {
        //            0 or 2 => Marshal.PtrToStringUni(cds.lpData),
        //            1 => Marshal.PtrToStringUTF8(cds.lpData),
        //            _ => throw new InvalidOperationException("Format de données non supporté")
        //        };

        //        if (string.IsNullOrEmpty(jsonData))
        //            throw new ArgumentNullException(nameof(jsonData));

        //        ProcessJsonData((int)cds.dwData, jsonData);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogError($"Erreur WM_COPYDATA: {ex.Message}");
        //        m.Result = (IntPtr)0; // Indique un échec au sender
        //    }
        //}

        //private void ProcessJsonData(int dataType, string jsonData)
        //{
        //    switch (dataType)
        //    {
        //        case 0 or 1:
        //            var command = JsonConvert.DeserializeObject<CommandeTransmise>(jsonData);
        //            UpdateUI(() => DisplayCommand(command));
        //            break;

        //        case 2:
        //            var search = JsonConvert.DeserializeObject<SearchPDF>(jsonData);
        //            LogMessage($"Recherche: {search.Command}={search.Value}");
        //            UpdateUI(() => ProcessSearch(search));
        //            break;

        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(dataType));
        //    }
        //}

        //private void UpdateUI(Action action)
        //{
        //    if (InvokeRequired)
        //        Invoke(action);
        //    else
        //        action();
        //}

        //private void DisplayCommand(CommandeTransmise command)
        //{
        //    textBox1.AppendText($"{command.Status}{Environment.NewLine}");
        //    textBox1.AppendText($"{command.Title}{Environment.NewLine}");
        //    textBox1.AppendText($"{command.Detail}{Environment.NewLine}");

        //    svg2 = UpdateSvg(svg2,
        //        command.Status,
        //        command.Title,
        //        command.Detail
        //    );
        //    frmToast frm = new frmToast(svg2);
        //    nint hwnd = GetForegroundWindow();
        //    frm.Show();
        //    SetForegroundWindow(hwnd.ToInt32());
        //}

        //private void ProcessSearch(SearchPDF search)
        //{
        //    // Implémentation spécifique
        //}

        //private void HandleSessionEndMessage()
        //{
        //    Closing = true;
        //    BeginInvoke(new Action(() => System.Windows.Forms.Application.Exit()));
        //}

        //private void LogError(string message)
        //{
        //    // Implémentation centralisée des logs
        //}

        private void button1_Click(object sender, EventArgs e)
        {
            frmToast frm = new frmToast(svg1);
            nint hwnd = GetForegroundWindow();
            frm.Show();
            SetForegroundWindow(hwnd.ToInt32());
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            frmToast frm = new frmToast(svg1);
            nint hwnd = GetForegroundWindow();
            frm.Show();
            SetForegroundWindow(hwnd.ToInt32());
        }

        public SvgDocument UpdateSvg(SvgDocument svg, OrderStatus orderStatus, string titleText, string detailText)
        {
            SvgColourServer? BgColor = null;
            SvgColourServer? TextColor = null;

            switch (orderStatus)
            {
                // old green #44aa00
                case OrderStatus.osConfirmed:
                    BgColor = checkBox1.Checked ?
                        new SvgColourServer(ControlPaint.Light(System.Drawing.ColorTranslator.FromHtml("#55D500"))) :
                        new SvgColourServer(System.Drawing.ColorTranslator.FromHtml("#55D500"));
                    TextColor =
                        new SvgColourServer(System.Drawing.ColorTranslator.FromHtml("#4a4a4a"));
                    break;

                case OrderStatus.osModifed:
                    BgColor = checkBox1.Checked ?
                       new SvgColourServer(ControlPaint.Light(System.Drawing.ColorTranslator.FromHtml("#FF9800"))) :
                       new SvgColourServer(System.Drawing.ColorTranslator.FromHtml("#FF9800"));
                    TextColor =
                        new SvgColourServer(System.Drawing.ColorTranslator.FromHtml("#4a4a4a"));
                    break;

                case OrderStatus.osCancelled:
                    BgColor = checkBox1.Checked ?
                        new SvgColourServer(ControlPaint.Light(System.Drawing.ColorTranslator.FromHtml("#ff4d00"))) :
                        new SvgColourServer(System.Drawing.ColorTranslator.FromHtml("#ff4d00"));
                    TextColor =
                        new SvgColourServer(System.Drawing.ColorTranslator.FromHtml("#ffffff")); // "#dfdfdf"
                    break;
            }

            svg.GetElementById<SvgRectangle>("background").Fill = BgColor;
            svg.GetElementById<SvgTextSpan>("application").Fill = TextColor;
            svg.GetElementById<SvgTextSpan>("title").Fill = TextColor;
            svg.GetElementById<SvgTextSpan>("title").Text = titleText;
            svg.GetElementById<SvgTextSpan>("detail").Fill = TextColor;
            svg.GetElementById<SvgTextSpan>("detail").Text = detailText;

            return svg;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            // Confirmée

            svg1 = UpdateSvg(svg1,
                 OrderStatus.osConfirmed,
                 "Commande CERP : Confirmée",
                 "KETOCONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmdé 1 / livré 1"
                 );
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = svg1.Draw();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            // Modifiée
            svg1 = UpdateSvg(svg1,
                OrderStatus.osModifed,
                "Commande CERP : partielle",
                "KETOCONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmdé 2 / livré 1"
                 );
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = svg1.Draw();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            // Annulée
            svg1 = UpdateSvg(svg1,
                OrderStatus.osCancelled,
                "Commande CERP: annulée",
                "KETOCONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmdé 2 / livré 0"
                 );
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = svg1.Draw();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton1_CheckedChanged(null, null);
            }
            else if (radioButton2.Checked)
            {
                radioButton2_CheckedChanged(null, null);
            }
            else if (radioButton3.Checked)
            {
                radioButton3_CheckedChanged(null, null);
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            var eventArgs = e as MouseEventArgs;
            switch (eventArgs.Button)
            {
                // Left click to reactivate
                case MouseButtons.Right:
                    // Do your stuff
                    //Console.Beep();
                    break;

                // Left click to reactivate
                case MouseButtons.Left:
                    // Do your stuff
                    //Console.Beep();
                    WindowState = FormWindowState.Normal;
                    if (this.Top < 0)
                    {
                        this.Top = this.Top + 5000;
                        SetForegroundWindow(Handle.ToInt32());
                    }
                    else
                    {
                        this.Top = this.Top - 5000;
                    }
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            svg2 = UpdateSvg(svg2,
                OrderStatus.osModifed,
                "Commande CERP : Confirmée ..................Test",
                "KAKACONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmdé 1 / livré 1"
                );
            frmToast frm = new frmToast(svg2);
            nint hwnd = GetForegroundWindow();
            frm.Show();
            SetForegroundWindow(hwnd.ToInt32());
        }

        private void quitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Closing = true;
            System.Windows.Forms.Application.Exit();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {


        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //WindowState = FormWindowState.Minimized;
            this.Top = this.Top - 5000;
            e.Cancel = !Closing;
        }

        private void montrerGTracePMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IntPtr hwnd = FindWindow("TfrmMain", "gTracePML 0.81");
            if (hwnd != IntPtr.Zero)
            {
                //MessageBox.Show(hwnd.ToString());
                //MessageBox.Show(Convert.ToInt32(gTracePML_SHOW_WM).ToString());
                SendMessage(hwnd, Convert.ToInt32(gTracePML_SHOW_WM), new IntPtr(0), new IntPtr(0));
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void frmMain_Load_1(object sender, EventArgs e)
        {
            this.Top = this.Top - 5000;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            _hook.Dispose();
            base.OnFormClosed(e);
        }

        private void tmrForegroundWindow_Tick(object sender, EventArgs e)
        {
            // Désactivation du timer pour éviter les appels multiples
            tmrForegroundWindow.Enabled = false;
            try
            {
                IntPtr currentForegroundWindow = GetForegroundWindow();

                // Vérification initiale de l'état de la fenêtre Winpharma
                if (hwndWinpharmaOrder != IntPtr.Zero && !IsWindowValid(hwndWinpharmaOrder))
                {
                    HandleWinpharmaWindowClosed();
                    return;
                }

                // Cas où la fenêtre Winpharma est au premier plan
                if (currentForegroundWindow == hwndWinpharmaOrder)
                {
                    if (isOuiPDFSearchActive)
                    {
                        //LogMessage("Winpharma Order is in foreground with PDF search active.");

                        // Mode Edit ?
                        bool isEdit = CaretManager.IsCaretVisible(hwndWinpharmaCtrl643);
                        if (isWinpharmaOrderEdit != isEdit)
                        {
                            isWinpharmaOrderEdit = isEdit;
                            //LogMessage($"Mode Edit: {isWinpharmaOrderEdit}");
                        }

                        // Changement de référence ?
                        if (hwndWinpharmaCtrl645 != IntPtr.Zero && IsWindowValid(hwndWinpharmaCtrl645))
                        {
                            string txt645 = WindowEnumerator.GetWindowText(hwndWinpharmaCtrl645);
                            txt645 = ExtractCodeFromCtrl645(txt645);
                            if (OuiPDFSearchText != txt645)
                            {
                                OuiPDFSearchText = txt645;
                                LogMessage("Recherche PDF: " + OuiPDFSearchText);

                                // Envoi de la recherche PDF
                                var search = new SearchPDF(
                                Command: "Find",
                                Value: OuiPDFSearchText);
                                string jsonSearch = JsonConvert.SerializeObject(search);
                                SendCopyDataMessage(null, "OuiPDF", jsonSearch, 2);
                            }
                        }

                        if (hwndWinpharmaCtrl643 != IntPtr.Zero && IsWindowValid(hwndWinpharmaCtrl643))
                        {
                            bool hasFocus = WindowEnumerator.IsExternalChildFocused(hwndWinpharmaCtrl643);
                            if (isWinpharmaOrderItemFocused != hasFocus)
                            {
                                isWinpharmaOrderItemFocused = hasFocus;
                                //LogMessage("Winpharma order item focused: " + (isWinpharmaOrderItemFocused ? "Yes" : "No"));
                            }
                        }
                    }

                    if (!isWinpharmaOrderFG)
                    {
                        HandleWinpharmaWindowForeground();
                    }
                    return;
                }

                // Cas où une autre fenêtre est au premier plan
                if (isWinpharmaOrderFG)
                {
                    HandleWinpharmaWindowBackground();
                }
                else
                {
                    CheckForNewWinpharmaWindow(currentForegroundWindow);
                }
            }
            finally
            {
                // Assurez-vous que le timer continue de fonctionner
                tmrForegroundWindow.Enabled = true;
            }
        }

        // Méthodes helper pour une meilleure modularité
        private bool IsWindowValid(IntPtr hWnd)
        {
            return hWnd != IntPtr.Zero && IsWindow(hWnd) && GetWindowProcessId(hWnd) != 0;
        }

        private void HandleWinpharmaWindowClosed()
        {
            // Réinitialisation de toutes les variables
            hwndWinpharmaOrder = IntPtr.Zero;
            hwndWinpharmaCtrl645 = IntPtr.Zero;
            hwndWinpharmaCtrl643 = IntPtr.Zero;
            isWinpharmaOrderFG = false;
            isWinpharmaOrderItemFocused = false;
            isOuiPDFSearchActive = false;
            OuiPDFSearchText = string.Empty;
            DefaultWinpharmaOrderTitle = string.Empty;
            LogMessage("Recherche PDF désactivée");
            LogMessage("Fenêtre de commande de Winpharma fermée.");

            var search = new SearchPDF(
                             Command: "FindStop",
                             Value: "");
            string jsonSearch = JsonConvert.SerializeObject(search);
            SendCopyDataMessage(null, "OuiPDF", jsonSearch, 2);
        }

        private void HandleWinpharmaWindowForeground()
        {
            isWinpharmaOrderFG = true;
            LogMessage("Fenêtre de commande Winpharma au premier plan");
        }

        private void HandleWinpharmaWindowBackground()
        {
            isWinpharmaOrderFG = false;
            LogMessage("Fenêtre de commande Winpharma en arrière plan");
        }

        private void CheckForNewWinpharmaWindow(IntPtr hWnd)
        {
            string windowClass = GetWindowClass(hWnd);
            if (windowClass != "#32770") return;

            uint processId = GetWindowProcessId(hWnd);
            if (!IsWinpharmaProcess(processId)) return;

            string windowTitle = WindowEnumerator.GetWindowText(hWnd);
            if (!windowTitle.Contains("Commande")) return;

            // Nouvelle fenêtre Winpharma détectée
            hwndWinpharmaOrder = hWnd;
            hwndWinpharmaCtrl645 = GetControlFromId(hwndWinpharmaOrder, 645);
            //LogMessage(hwndWinpharmaCtrl645.ToString());
            hwndWinpharmaCtrl643 = GetControlFromId(hwndWinpharmaOrder, 643);
            DefaultWinpharmaOrderTitle = windowTitle;
            isWinpharmaOrderFG = true;
            LogMessage("Nouvelle fenêtre de commande Winpharma détectée et au premier plan");
        }

        private bool IsWinpharmaProcess(uint processId)
        {
            try
            {
                var process = Process.GetProcessById((int)processId);
                return process.MainModule.ModuleName.Equals("WPHARMA.EXE", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void LogMessage(string message)
        {
            if (txbHook.InvokeRequired)
            {
                txbHook.Invoke(new Action(() => txbHook.AppendText(message + Environment.NewLine)));
            }
            else
            {
                txbHook.AppendText(message + Environment.NewLine);
            }
        }

        public string ExtractCodeFromCtrl645(string txt645)
        {
            int spaceIndex = txt645.IndexOf(' ');
            if (spaceIndex > 0)
            {
                string firstNumber = txt645.Substring(0, spaceIndex);
                // Vérification supplémentaire que c'est bien un nombre
                if (long.TryParse(firstNumber, out long numericValue))
                {
                    return firstNumber;
                }
                return string.Empty;
            }
            return string.Empty;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txbHook.Clear();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //  LogMessage(CaretManager.IsCaretVisible(GetForegroundWindow()).ToString());
        }



        //[JsonObject(ItemRequired = Required.Always)]
        //public struct wpsMessage
        //{
        //    public string Msg;
        //    public string Data;

        //    public static string ToJson(string msg, string data)
        //    {
        //        wpsMessage message;
        //        message.Msg = msg;
        //        message.Data = data;
        //        return JsonConvert.SerializeObject(message) as string;
        //    }

        //    public static wpsMessage FromJson(string text)
        //    {
        //        return JsonConvert.DeserializeObject<wpsMessage>(text);
        //    }
        //}

        //public static class wpsCopyDataSender
        //{
        //    public static void Send(string guid, wpsCDMessage msg, string data = "", string? customPayload = null, bool showPayload = false)
        //    {
        //        const UInt32 WM_COPYDATA = 0x004A;

        //        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        //        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        //        var message = new wpsMessage
        //        {
        //            Msg = msg.ToString(),
        //            Data = data
        //        };

        //        string payload = customPayload == null ? guid + JsonConvert.SerializeObject(message) as string : customPayload;
        //        int payloadLength = (payload.Length + 1) * 2;

        //        if (showPayload) MessageBox.Show(payload);

        //        COPYDATASTRUCT cds;
        //        cds.dwData = IntPtr.Zero;
        //        cds.cbData = payloadLength;
        //        cds.lpData = Marshal.StringToHGlobalUni(payload);

        //        SendMessage(wpsApp.HWnd, WM_COPYDATA, IntPtr.Zero, ref cds);

        //        Marshal.FreeHGlobal(cds.lpData);
        //    }
        //}

        //public static class wpsCopyDataReceiver
        //{
        //    public static string? GetPayload(ref Message m)
        //    {
        //        string? res = null;

        //        try
        //        {
        //            COPYDATASTRUCT cds = (COPYDATASTRUCT)m.GetLParam(typeof(COPYDATASTRUCT));
        //            res = Marshal.PtrToStringUni(cds.lpData);
        //        }
        //        catch { }

        //        return res;
        //    }

        //    public static wpsCDMessage ParseCDM(string payload, out string? data)
        //    {
        //        wpsCDMessage res = wpsCDMessage.Undefined;
        //        data = null;

        //        try
        //        {
        //            wpsMessage message = JsonConvert.DeserializeObject<wpsMessage>(payload);
        //            res = (wpsCDMessage)Enum.Parse(typeof(wpsCDMessage), message.Msg);
        //            data = message.Data;
        //        }
        //        catch { }

        //        return res;
        //    }
        //}
    }
}



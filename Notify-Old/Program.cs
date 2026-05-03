using System.Resources;
using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Notify
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            Application.Run(new frmMain());
            //Application.Run(new MyCustomApplicationContext());
        }

    }

    //public class MyCustomApplicationContext : ApplicationContext
    //{
    //    private NotifyIcon trayIcon;

    //    public static byte[] IconToBytes(Icon icon)
    //    {
    //        using (MemoryStream ms = new MemoryStream())
    //        {
    //            icon.Save(ms);
    //            return ms.ToArray();
    //        }
    //    }

    //    public static Icon BytesToIcon(byte[] bytes)
    //    {
    //        using (MemoryStream ms = new MemoryStream(bytes))
    //        {
    //            return new Icon(ms);
    //        }
    //    }

    //    public MyCustomApplicationContext()
    //    {
    //        trayIcon = new NotifyIcon()
    //        {
    //            Icon = BytesToIcon( Resource1.Icon33),
    //            ContextMenuStrip = new ContextMenuStrip()
    //            {
    //                Items = { new ToolStripMenuItem("Exit", null, Exit) }
    //            },
    //            Visible = true
    //        };
    //    }

    //    void Exit(object? sender, EventArgs e)
    //    {
    //        trayIcon.Visible = false;
    //        Application.Exit();
    //    }
    //}
}
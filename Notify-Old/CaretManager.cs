using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Notify
{
    public static class CaretManager
    {
        // Struct pour GetGUIThreadInfo
        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Vérifie si un caret est affiché dans le thread d’une fenêtre donnée
        public static bool IsCaretVisible(IntPtr windowHandle)
        {
            uint pid;
            uint threadId = GetWindowThreadProcessId(windowHandle, out pid);

            GUITHREADINFO info = new GUITHREADINFO();
            info.cbSize = Marshal.SizeOf(info);

            if (GetGUIThreadInfo(threadId, ref info))
            {
                return info.rcCaret.Right > info.rcCaret.Left && info.rcCaret.Bottom > info.rcCaret.Top;
            }

            return false;
        }

        // Exemple : Hook des touches flèches conditionné au caret
        //public static void OnKeyDown(Keys key, IntPtr windowHandle)
        //{
        //    if (key == Keys.Left || key == Keys.Right)
        //    {
        //        bool caretVisible = IsCaretVisible(windowHandle);

        //        if (!caretVisible)
        //        {
        //            // Traitement personnalisé ici
        //            Console.WriteLine($"Touche {key} interceptée (pas en édition).");
        //        }
        //    }
        //}
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Notify.frmMain;

namespace Notify
{
    public class CDMessage
    {
        public const int WM_COPYDATA = 0x004A;

        public enum OrderStatus
        {
            osConfirmed,
            osModifed,
            osCancelled
        }

        [JsonObject(ItemRequired = Required.Always)]
        public record CommandeTransmise(
            OrderStatus Status,
            string Title,
            string Detail);

        [JsonObject(ItemRequired = Required.Always)]
        public record SearchPDF(
            string Command,
            string Value);

        // Définition de la structure COPYDATASTRUCT
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        // Importation de la fonction Windows pour trouver une fenêtre
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Importation de la fonction Windows pour envoyer un message
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        // Méthode pour envoyer un message WM_COPYDATA
        public static bool SendCopyDataMessage(string targetWindowClassName, string targetWindowTitle, string jsonData, int dataType)
        {
            // Trouver la fenêtre cible
            IntPtr targetWindowHandle = FindWindow(targetWindowClassName, targetWindowTitle);

            if (targetWindowHandle == IntPtr.Zero)
            {
                //MessageBox.Show("Fenêtre cible introuvable.");
                return false;
            }

            // Préparer la structure COPYDATASTRUCT
            COPYDATASTRUCT cds = new COPYDATASTRUCT();
            cds.dwData = (IntPtr)dataType; // Type de données (0, 1 ou 2 comme dans votre code)

            // Convertir la chaîne en tableau de bytes selon l'encodage
            byte[] dataBytes;
            if (dataType == 1) // ANSICHAR
            {
                dataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
            }
            else // UNICODE (cas 0 et 2)
            {
                dataBytes = System.Text.Encoding.Unicode.GetBytes(jsonData);
            }

            // Allouer de la mémoire pour les données
            cds.lpData = Marshal.AllocCoTaskMem(dataBytes.Length);
            Marshal.Copy(dataBytes, 0, cds.lpData, dataBytes.Length);
            cds.cbData = dataBytes.Length;

            try
            {
                // Envoyer le message
                IntPtr result = SendMessage(targetWindowHandle, WM_COPYDATA, IntPtr.Zero, ref cds);

                // Vérifier le résultat (optionnel)
                return result != IntPtr.Zero;
            }
            finally
            {
                // Libérer la mémoire allouée
                Marshal.FreeCoTaskMem(cds.lpData);
            }
        }

        // Exemple d'utilisation
        public static void ExampleUsage()
        {
            // Pour envoyer un message de type CommandeTransmise (type 0 ou 1)
            var commande = new CommandeTransmise(
                Status: OrderStatus.osModifed,
                Title: "Test commande",
                Detail: "Détails de la commande");

            string jsonCommande = JsonConvert.SerializeObject(commande);
            SendCopyDataMessage(null, "Titre de la fenêtre cible", jsonCommande, 0);

            // Pour envoyer un message de type SearchPDF (type 2)
            var search = new SearchPDF(
                Command: "Recherche",
                Value: "Valeur recherchée");

            string jsonSearch = JsonConvert.SerializeObject(search);
            SendCopyDataMessage(null, "Titre de la fenêtre cible", jsonSearch, 2);
        }
    }
}

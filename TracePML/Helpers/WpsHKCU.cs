using Microsoft.Win32;

namespace wipisoft
{
    public class WpsHKCU
    {
        public static bool HKCU_GetBoolean(string compactKeyName, string valueName, bool defaultValue)
        {
            bool result;
            string? regValue =
                Registry.GetValue($@"HKEY_CURRENT_USER\Software\{compactKeyName}", valueName, "")
                as string;

            if (regValue == null) result = defaultValue;
            else if (regValue == "1") result = true;
            else if (regValue.ToLower() == "o") result = true;
            else if (regValue.ToLower() == "oui") result = true;
            else if (regValue.ToLower() == "true") result = true;
            else result = defaultValue;

            if (regValue != result.ToString())
                Registry.SetValue($@"HKEY_CURRENT_USER\Software\{compactKeyName}", valueName, result);

            return result;
        }

        public static DateTime HKCU_GetDate(string compactKeyName, string valueName, DateTime defaultValue)
        {
            try
            {
                string fullKeyPath = $@"HKEY_CURRENT_USER\Software\{compactKeyName}";
                string? regValue = Registry.GetValue(fullKeyPath, valueName, null) as string;

                if (DateTime.TryParse(regValue, out DateTime result))
                {
                    return result;
                }

                Registry.SetValue(fullKeyPath, valueName, defaultValue.ToString("o")); // Fmt ISO 8601
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string? HKCU_GetString(string compactKeyName, string valueName, string? defaultValue = null)
        {
            string fullKeyPath = $@"HKEY_CURRENT_USER\Software\{compactKeyName}";
            string? regValue = Registry.GetValue(fullKeyPath, valueName, null) as string;

            if (regValue != null) return regValue;

            if (defaultValue != null)
                Registry.SetValue(fullKeyPath, valueName, defaultValue);

            return defaultValue;
        }

        public static void HKCU_SetString(string compactKeyName, string valueName, string value)
        {
            string fullKeyPath = $@"HKEY_CURRENT_USER\Software\{compactKeyName}";
            Registry.SetValue(fullKeyPath, valueName, value);
        }
    }
}

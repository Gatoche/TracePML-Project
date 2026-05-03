using System.Reflection;

namespace Notify
{
    internal static class OwnResources
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        internal static byte[] GetEmbeddedBytes(string resourceName)
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        internal static string GetEmbeddedString(string resourceName)
        {
            return System.Text.Encoding.UTF8.GetString(GetEmbeddedBytes(resourceName));
        }

        internal static string[] ListResources()
        {
            return _assembly.GetManifestResourceNames();
        }
    }
}

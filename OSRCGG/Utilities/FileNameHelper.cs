using System.IO;

namespace OSRCGG
{
    internal static class FileNameHelper
    {
        public static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "untitled";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Trim();
        }
    }
}

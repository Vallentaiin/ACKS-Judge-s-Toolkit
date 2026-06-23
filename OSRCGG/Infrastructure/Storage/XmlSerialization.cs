using System.IO;
using System.Xml.Serialization;

namespace OSRCGG
{
    internal static class XmlSerialization
    {
        public static T DeserializeFile<T>(string path) where T : class
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream stream = File.OpenRead(path))
            {
                return (T)serializer.Deserialize(stream);
            }
        }

        public static void SerializeFile<T>(string path, T value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream stream = File.Create(path))
            {
                serializer.Serialize(stream, value);
            }
        }

        public static T Clone<T>(T value) where T : class
        {
            if (value == null) return null;

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, value);
                stream.Position = 0;
                return (T)serializer.Deserialize(stream);
            }
        }
    }
}

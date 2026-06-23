using System.Collections.Generic;
using System.Linq;

namespace OSRCGG
{
    internal sealed class XmlRecordStore<T> : IRecordStore<T> where T : class
    {
        private readonly string path;

        public XmlRecordStore(string path)
        {
            this.path = path;
        }

        public List<T> Load()
        {
            try
            {
                return XmlSerialization.DeserializeFile<List<T>>(path) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        public void Save(IEnumerable<T> records)
        {
            XmlSerialization.SerializeFile(path, records == null ? new List<T>() : records.ToList());
        }
    }
}

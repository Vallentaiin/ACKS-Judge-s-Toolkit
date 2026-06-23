using System.Collections.Generic;

namespace OSRCGG
{
    internal interface IRecordStore<T> where T : class
    {
        List<T> Load();
        void Save(IEnumerable<T> records);
    }
}

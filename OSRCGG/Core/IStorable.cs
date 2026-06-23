using System;

namespace OSRCGG
{
    // Контракт для сущностей, которые сохраняются в библиотеку или внешний файл.
    public interface IStorable
    {
        string Id { get; set; }
        DateTime UpdatedAt { get; set; }
    }

    // Контракт для записей, которые безопасно показывать в списках UI.
    public interface IDisplayRecord
    {
        string DisplayName { get; }
    }
}

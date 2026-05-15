using Task3.Web.Models;

namespace Task3.Web.Services;

public sealed class ItemStore
{
    private readonly List<Item> _items = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public IReadOnlyList<Item> GetAll()
    {
        lock (_lock)
        {
            return _items.ToList();
        }
    }

    public Item Add(string name)
    {
        lock (_lock)
        {
            var item = new Item
            {
                Id = _nextId++,
                Name = name,
                CreatedAtUtc = DateTime.UtcNow
            };

            _items.Add(item);
            return item;
        }
    }
}


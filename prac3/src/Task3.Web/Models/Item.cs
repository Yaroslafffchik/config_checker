namespace Task3.Web.Models;

public sealed class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}


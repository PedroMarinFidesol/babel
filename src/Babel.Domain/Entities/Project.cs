namespace Babel.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

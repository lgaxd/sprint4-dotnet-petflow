namespace PetFlowAPI.Domain.Entities;

/// <summary>
/// Anotação clínica livre sobre um pet. Persistida em MongoDB (NoSQL) por ser um documento
/// de estrutura flexível (tags e conteúdo variam por anotação).
/// </summary>
public class PetNote
{
    public string Id { get; set; } = string.Empty;
    public long PetId { get; set; }
    public long AuthorTutorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

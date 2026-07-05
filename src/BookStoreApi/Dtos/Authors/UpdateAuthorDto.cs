namespace BookStoreApi.Dtos.Authors;

public class UpdateAuthorDto
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

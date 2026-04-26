namespace ContaMec.Api.Domain.Entities;

public class UserRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}

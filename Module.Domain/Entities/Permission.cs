namespace Module.Domain.Entities;

public class Permission
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Title { get; set; } = default!;
    public List<Role> Roles { get; set; } = [];
}

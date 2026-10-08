using Module.Domain.Common;

namespace Module.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = default!;
    public bool Active { get; set; } = true;

    public List<User> Users { get; set; } = [];
    public List<Permission> Permissions { get; set; } = [];
}

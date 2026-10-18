using PetFlowAPI.Domain.Enums;

namespace PetFlowAPI.Domain.Entities;

public class Tutor
{
    public long Id { get; set; }
    public string Role { get; set; } = UserRoles.Tutor;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    public bool IsAdmin => string.Equals(Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
}

public class Address
{
    public long Id { get; set; }
    public long TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

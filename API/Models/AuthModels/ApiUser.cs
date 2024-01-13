using Microsoft.AspNetCore.Identity;

namespace API.Models.AuthModels;

public class ApiUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

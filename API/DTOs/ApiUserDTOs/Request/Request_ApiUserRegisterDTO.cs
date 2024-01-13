using System.ComponentModel.DataAnnotations;

namespace API.DTOs.ApiUserDTOs.Request;

public class Request_ApiUserRegisterDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [StringLength(20, ErrorMessage = "Password must be between 4 and 20 characters", MinimumLength = 4)]
    public string Password { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
}

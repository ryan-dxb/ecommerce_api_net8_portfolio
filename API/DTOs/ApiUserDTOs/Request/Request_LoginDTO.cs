using System.ComponentModel.DataAnnotations;

namespace API.DTOs.ApiUserDTOs.Request;


public class Request_LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}


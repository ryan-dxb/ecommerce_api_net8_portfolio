namespace API.DTOs.ApiUserDTOs.Request;

public class Request_TokenDto
{
    public string UserId { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}

using API.DTOs.ApiUserDTOs.Request;
using API.DTOs.ApiUserDTOs.Response;

namespace API.Repositories.AuthRepository;

public interface IAuthRepository
{
    Task<Response_ApiUserRegisterDTO> Register(Request_ApiUserRegisterDTO userDTO);
    Task<Response_ApiUserRegisterDTO> RegisterAdmin(Request_ApiUserRegisterDTO userDTO, int secretKey);
    Task<Response_LoginDTO> Login(Request_LoginDto login);
    Task<Response_LoginDTO> VerifyAndGenerateTokens(Request_TokenDto tokenDto);
    Task<bool> LogoutDeleteRefreshToken(string userId);
}

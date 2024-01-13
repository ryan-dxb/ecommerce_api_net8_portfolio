using API.Constants;
using API.Data;
using API.DTOs.ApiUserDTOs.Request;
using API.DTOs.ApiUserDTOs.Response;
using API.Models.AuthModels;
using Microsoft.AspNetCore.Identity;

namespace API.Repositories.AuthRepository;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<ApiUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public AuthRepository(
        UserManager<ApiUser> userManager,
        IConfiguration configuration,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<Response_ApiUserRegisterDTO> Register(Request_ApiUserRegisterDTO userDTO)
    {
        var user = new ApiUser()
        {
            Email = userDTO.Email,
            FirstName = userDTO.FirstName,
            LastName = userDTO.LastName,
        };

        user.UserName = userDTO.Email;
        user.EmailConfirmed = true;

        var result = await _userManager.CreateAsync(user, userDTO.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, Roles.Customer);

            return new Response_ApiUserRegisterDTO()
            {
                isSuccess = true,
                apiUser = user
            };
        }

        return new Response_ApiUserRegisterDTO()
        {
            isSuccess = false,
            message = result.Errors.Select(x => x.Description).ToList()
        };
    }

    public async Task<Response_ApiUserRegisterDTO> RegisterAdmin(Request_ApiUserRegisterDTO userDTO, int secretKey)
    {
        if (secretKey != 12345)
        {
            return new Response_ApiUserRegisterDTO()
            {
                isSuccess = false,
                message = new List<string>() { "Invalid Secret Key" }
            };
        }
        var user = new ApiUser()
        {
            Email = userDTO.Email,
            FirstName = userDTO.FirstName,
            LastName = userDTO.LastName,
        };

        user.UserName = userDTO.Email;
        user.EmailConfirmed = true;

        var result = await _userManager.CreateAsync(user, userDTO.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, Roles.Administrator);

            return new Response_ApiUserRegisterDTO()
            {
                isSuccess = true,
                apiUser = user
            };
        }

        return new Response_ApiUserRegisterDTO()
        {
            isSuccess = false,
            message = result.Errors.Select(x => x.Description).ToList()
        };
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Constants;
using API.Data;
using API.DTOs.ApiUserDTOs.Request;
using API.DTOs.ApiUserDTOs.Response;
using API.Models.AuthModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

    public async Task<Response_LoginDTO> Login(Request_LoginDto login)
    {
        bool isValidUser = false;
        var user = await _userManager.FindByEmailAsync(login.Email);
        if (user == null)
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>() { "Invalid Authentication" }
            };
        }

        bool isCorrectPassword = await _userManager.CheckPasswordAsync(user, login.Password);

        if (isCorrectPassword)
        {
            isValidUser = true;
        }

        if (!isValidUser)
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>() { "Invalid Authentication" }
            };
        }

        var token = await GenerateToken(user);
        var refreshToken = await CreateRefreshToken(user, token);

        return new Response_LoginDTO()
        {
            UserId = user.Id,
            Token = token,
            RefreshToken = refreshToken,
            Result = true
        };
    }

    public async Task<Response_LoginDTO> VerifyAndGenerateTokens(Request_TokenDto tokenDto)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(tokenDto.Token);

        var tokenIssuer = tokenContent.Issuer;
        if (tokenIssuer != _configuration["JwtSettings:Issuer"])
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>()
                    {
                        "Invalid token"
                    }
            };
        }
        var tokenAudience = tokenContent.Audiences.ToList();

        if (!tokenAudience.Contains(_configuration["JwtSettings:Audience"]))
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>()
                    {
                        "Invalid token"
                    }
            };
        }

        var userName = tokenContent.Claims.ToList()
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || user.Id != tokenDto.UserId)
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>()
                    {
                        "Invalid token"
                    }
            };
        }

        //REFRESH TOKEN VALIDATIONS
        var refreshTokenFromDb = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rf => rf.Token == tokenDto.RefreshToken);
        if (refreshTokenFromDb == null)
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>()
                    {
                        "Invalid refresh token"
                    }
            };
        }

        if (refreshTokenFromDb.JwtId != tokenContent.Id)
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>()
                    {
                        "Invalid refresh token"
                    }
            };
        }

        if (refreshTokenFromDb.ExpireDate < DateTime.UtcNow)
        {
            return new Response_LoginDTO()
            {
                Result = false,
                Errors = new List<string>()
                    {
                        "Refresh token expired"
                    }
            };
        }

        var newToken = await GenerateToken(user);
        var newRefreshToken = await CreateRefreshToken(user, newToken);

        return new Response_LoginDTO()
        {
            Result = true,
            Errors = new List<string>(),
            Token = newToken,
            RefreshToken = newRefreshToken,
            UserId = user.Id,
        };
    }

    public async Task<bool> LogoutDeleteRefreshToken(string userId)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId);
        if (refreshToken == null)
        {
            return true;
        }
        _dbContext.RefreshTokens.Remove(refreshToken);
        await _dbContext.SaveChangesAsync();

        return true;
    }


    //PRIVATE FUNCTIONS

    private async Task<string> GenerateToken(ApiUser user)
    {
        //GET DATA READY
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:SecretKey"]));
        var credentials = new SigningCredentials(securityKey,
            SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(role => new Claim(
            ClaimTypes.Role,
            role));

        var userClaims = await _userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id),
            }.Union(userClaims).Union(roleClaims);

        //GENETRATE TOKEN
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToInt32(_configuration["JwtSettings:DurationInMinutes"])),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshToken(ApiUser user, string token)
    {
        var existingRefreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        if (existingRefreshToken != null)
        {
            _dbContext.RefreshTokens.Remove(existingRefreshToken);
            await _dbContext.SaveChangesAsync();
        }

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var tokenContent = jwtSecurityTokenHandler.ReadJwtToken(token);

        var refreshToken = new RefreshToken()
        {
            JwtId = tokenContent.Id,
            Token = RandomStringGeneration(23),
            AddedDate = DateTime.UtcNow,
            ExpireDate = DateTime.UtcNow.AddMinutes(110),
            UserId = user.Id,
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken.Token;
    }

    private string RandomStringGeneration(int length)
    {
        var random = new Random();
        var chars = "ABCDEFGHJKLMNOPQRSTUVWYZ123456789abvdefghklmnoprstuvwyz_";

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

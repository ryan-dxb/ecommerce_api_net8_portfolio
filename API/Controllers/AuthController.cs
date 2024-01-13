using System.Security.Claims;
using API.DTOs.ApiUserDTOs.Request;
using API.DTOs.ApiUserDTOs.Response;
using API.Models.AuthModels;
using API.Repositories.AuthRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepository;
    private readonly UserManager<ApiUser> _userManager;

    public AuthController(IAuthRepository authRepository,
    UserManager<ApiUser> userManager,
    IConfiguration configuration)
    {
        _authRepository = authRepository;
        _userManager = userManager;
    }

    [HttpPost]
    [Route("register")]
    public async Task<ActionResult<Response_ApiUserRegisterDTO>> Register([FromBody] Request_ApiUserRegisterDTO userDTO)
    {
        if (ModelState.IsValid)
        {
            var result = await _authRepository.Register(userDTO);

            if (result.isSuccess == false)
            {
                return BadRequest(new Response_ApiUserRegisterDTO()
                {
                    isSuccess = false,
                    message = result.message
                });
            }

            // Email Confirmation
            // var token = await _userManager.GenerateEmailConfirmationTokenAsync(result.apiUser);
            // var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = result.apiUser.Id, token = token }, Request.Scheme);

            // Send Email
            // await _emailSender.SendEmailAsync(result.apiUser.Email, "Confirm your email", $"Please confirm your email by clicking this link: <a href='{confirmationLink}'>link</a>");

            return Ok(new Response_ApiUserRegisterDTO()
            {
                isSuccess = true,
                apiUser = result.apiUser,
                message = new List<string>() { "User created successfully!" }
            });
        }

        return BadRequest(new Response_ApiUserRegisterDTO()
        {
            isSuccess = false,
            message = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList()
        });
    }


    [HttpPost]
    [Route("register-admin/{secretKey}")]
    public async Task<ActionResult<Response_ApiUserRegisterDTO>> RegisterAdmin([FromBody] Request_ApiUserRegisterDTO userDTO, [FromRoute] int secretKey)
    {
        if (ModelState.IsValid)
        {
            var result = await _authRepository.RegisterAdmin(userDTO, secretKey);

            if (result.isSuccess == false)
            {
                return BadRequest(new Response_ApiUserRegisterDTO()
                {
                    isSuccess = false,
                    message = result.message
                });
            }

            // Email Confirmation
            // var token = await _userManager.GenerateEmailConfirmationTokenAsync(result.apiUser);
            // var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = result.apiUser.Id, token = token }, Request.Scheme);

            // Send Email
            // await _emailSender.SendEmailAsync(result.apiUser.Email, "Confirm your email", $"Please confirm your email by clicking this link: <a href='{confirmationLink}'>link</a>");

            return Ok(new Response_ApiUserRegisterDTO()
            {
                isSuccess = true,
                apiUser = result.apiUser,
                message = new List<string>() { "User created successfully!" }
            });
        }

        return BadRequest(new Response_ApiUserRegisterDTO()
        {
            isSuccess = false,
            message = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList()
        });
    }


    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<Response_LoginDTO>> Login([FromBody] Request_LoginDto login)
    {
        if (ModelState.IsValid)
        {
            var result = await _authRepository.Login(login);

            if (result.Result == false)
            {
                return BadRequest(new Response_LoginDTO()
                {
                    Result = false,
                    Errors = result.Errors
                });
            }

            return Ok(result);

        }

        return BadRequest(new Response_LoginDTO()
        {
            Result = false,
            Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList()
        });
    }

    [HttpPost]
    [Route("generate-token")]
    public async Task<ActionResult<Response_LoginDTO>> GenerateToken([FromBody] Request_TokenDto tokenDto)
    {
        if (ModelState.IsValid)
        {
            var result = await _authRepository.VerifyAndGenerateTokens(tokenDto);

            if (result.Result == false)
            {
                return BadRequest(new Response_LoginDTO()
                {
                    Result = false,
                    Errors = result.Errors
                });
            }

            return Ok(result);

        }

        return BadRequest(new Response_LoginDTO()
        {
            Result = false,
            Errors = ModelState.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)).ToList()
        });
    }

    [HttpPost]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.Claims.FirstOrDefault(x => x.Type == "UserId").Value;
        // var userId = HttpContext.User.FindFirstValue("uid"); // From JWT Token

        var result = await _authRepository.LogoutDeleteRefreshToken(userId);

        if (result)
        {
            return Ok();
        }

        return BadRequest();
    }

    [HttpPost]
    [Route("update-user-details")]
    public async Task<IActionResult> UpdateUserDetails([FromBody] Request_ApiUserDetailsUpadate userDTO)
    {
        // var userId = User.Claims.FirstOrDefault(x => x.Type == "UserId").Value;
        var userId = HttpContext.User.FindFirstValue("uid"); // From JWT Token

        var result = await _userManager.FindByIdAsync(userId);

        if (result == null)
        {
            return BadRequest(new Response_ApiUserRegisterDTO()
            {
                isSuccess = false,
                message = new List<string>() { "User not found!" }
            });
        }

        result.FirstName = userDTO.FirstName;
        result.LastName = userDTO.LastName;

        var updateResult = await _userManager.UpdateAsync(result);

        if (updateResult.Succeeded)
        {
            return Ok(new Response_ApiUserRegisterDTO()
            {
                isSuccess = true,
                apiUser = result,
                message = new List<string>() { "User details updated successfully!" }
            });
        }

        return BadRequest(new Response_ApiUserRegisterDTO()
        {
            isSuccess = false,
            message = updateResult.Errors.Select(x => x.Description).ToList()
        });
    }
}

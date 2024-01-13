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
}

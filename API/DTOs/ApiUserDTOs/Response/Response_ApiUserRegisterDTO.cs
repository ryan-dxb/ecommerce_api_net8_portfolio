using API.Models.AuthModels;

namespace API.DTOs.ApiUserDTOs.Response
{
    public class Response_ApiUserRegisterDTO
    {
        public bool isSuccess { get; set; }
        public List<string> message { get; set; } = new List<string>();

        public ApiUser apiUser { get; set; }

    }
}

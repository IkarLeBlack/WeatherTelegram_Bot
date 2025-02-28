using Microsoft.AspNetCore.Mvc;
using WeatherBot.Service;


namespace WeatherBotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly BotHostedService _telegramBotService;
        private readonly DatabaseService _dbService;

        public WeatherController(BotHostedService telegramBotService, DatabaseService dbService)
        {
            _telegramBotService = telegramBotService;
            _dbService = dbService;
        }
        
        public class WeatherRequestDto
        {
            public string City { get; set; }
        }

        [HttpPost("sendWeatherToAll")]
        public async Task<IActionResult> SendWeatherToAll([FromBody] WeatherRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.City))
            {
                return BadRequest(new { Message = "City is required" });
            }

            var users = await _dbService.GetAllUsersAsync();
            foreach (var user in users)
            {
                var weatherInfo = await _telegramBotService.GetWeatherAsync(request.City);
                await _telegramBotService.SendMessageToUser(user.ChatId, weatherInfo);
            }

            return Ok(new { Message = "Weather sent to all users." });
        }

        [HttpPost("sendWeatherToUser/{userId}")]
        public async Task<IActionResult> SendWeatherToUser(long userId, [FromBody] WeatherRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.City))
            {
                return BadRequest(new { Message = "City is required" });
            }

            var user = await _dbService.GetUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var weatherInfo = await _telegramBotService.GetWeatherAsync(request.City);
            await _telegramBotService.SendMessageToUser(user.ChatId, weatherInfo);

            return Ok(new { Message = "Weather sent to user." });
        }

    }
}

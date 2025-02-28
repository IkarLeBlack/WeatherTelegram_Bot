using Microsoft.AspNetCore.Mvc;
using WeatherBot.Service;

namespace WeatherBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DatabaseService _dbService;

        public UsersController(DatabaseService dbService)
        {
            _dbService = dbService;
        }
        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserHistory(long userId)
        {
            var user = await _dbService.GetUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var history = await _dbService.GetUserWeatherHistoryAsync(userId);
            return Ok(new 
            { 
                User = user,
                WeatherHistory = history
            });
        }
    }
}
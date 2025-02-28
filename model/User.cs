namespace WeatherBot.model;

public class User
{
    public long UserId { get; set; }
    public string UserName { get; set; }
    public long ChatId { get; set; }
    public DateTime CreatedAt { get; set; }
}

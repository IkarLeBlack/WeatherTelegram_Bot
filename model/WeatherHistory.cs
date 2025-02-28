namespace WeatherBot.model;

public class WeatherHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string City { get; set; }
    public string Temperature { get; set; }
    public DateTime RequestTime { get; set; }
}


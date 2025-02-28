using System.Text.Json;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using WeatherBot.Inteface;

namespace WeatherBot.Service;

public class BotHostedService : IHostedService
{
    private readonly TelegramBotClient _botClient;
    private readonly DatabaseService _dbService;
    private readonly HttpClient _httpClient;
    private CancellationTokenSource _cts;
    private static string _weatherApiKey;

    public BotHostedService(TelegramBotClient botClient, DatabaseService dbService, HttpClient httpClient, IOptions<ApiSettings> apiSettings)
    {
        _botClient = botClient;
        _dbService = dbService;
        _httpClient = httpClient;
        _weatherApiKey = apiSettings.Value.WeatherApiKey;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: _cts.Token
        );

        Console.WriteLine("Telegram бот запущено!");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        long chatId = message.Chat.Id;
        string userName = message.From?.Username ?? "Unknown";
        
        long userId = await _dbService.AddUserAsync(chatId, userName, chatId);

        switch (messageText)
        {
            case "/start":
                await SendStartMessage(botClient, chatId, cancellationToken);
                break;

            case "/weather":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Введіть місто після команди /weather, наприклад: `/weather Kyiv`",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                break;

            case "Погода в Києві":
                await SendWeather(botClient, chatId, userId, "Kyiv", cancellationToken);
                break;

            case "Погода у Львові":
                await SendWeather(botClient, chatId, userId, "Lviv", cancellationToken);
                break;

            default:
                if (messageText.StartsWith("/weather "))
                {
                    string city = messageText.Replace("/weather", "").Trim();
                    await SendWeather(botClient, chatId, userId, city, cancellationToken);
                }
                break;
        }
    }

    public async Task SendMessageToUser(long chatId, string message)
    {
        try
        {
            await _botClient.SendTextMessageAsync(chatId, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке сообщения пользователю с chatId {chatId}: {ex.Message}");
            throw;
        }
    }

    private async Task SendStartMessage(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("Погода в Києві"),
            new KeyboardButton("Погода у Львові")
        })
        {
            ResizeKeyboard = true
        };

        await botClient.SendTextMessageAsync(
            chatId,
            "Оберіть місто для отримання погоди або використовуйте команду `/weather Місто`.",
            replyMarkup: keyboard,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }

    private async Task SendWeather(ITelegramBotClient botClient, long chatId, long userId, string city, CancellationToken cancellationToken)
    {
        string weatherInfo = await GetWeatherAsync(city);
        await _dbService.AddWeatherHistoryAsync(userId, city, weatherInfo);
        await botClient.SendTextMessageAsync(chatId, weatherInfo, cancellationToken: cancellationToken);
    }

    public async Task<string> GetWeatherAsync(string city)
    {
        try
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={_weatherApiKey}&units=metric&lang=ua";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"Помилка3: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("cod", out JsonElement codElement) && codElement.GetInt32() != 200)
            {
                return $"Помилка2: {root.GetProperty("message").GetString()}";
            }

            string cityName = root.GetProperty("name").GetString();
            string country = root.GetProperty("sys").GetProperty("country").GetString();
            string weatherDescription = root.GetProperty("weather")[0].GetProperty("description").GetString();
            double temperature = root.GetProperty("main").GetProperty("temp").GetDouble();
            double feelsLike = root.GetProperty("main").GetProperty("feels_like").GetDouble();
            int humidity = root.GetProperty("main").GetProperty("humidity").GetInt32();
            double windSpeed = root.GetProperty("wind").GetProperty("speed").GetDouble();
            int cloudiness = root.GetProperty("clouds").GetProperty("all").GetInt32();
            int pressure = root.GetProperty("main").GetProperty("pressure").GetInt32();

            return $"Погода у місті {cityName}, {country}\n" +
                   $"{weatherDescription}\n" +
                   $"Температура: {temperature}°C (відчувається як {feelsLike}°C)\n" +
                   $"Вологість: {humidity}%\n" +
                   $"Вітер: {windSpeed} м/с\n" +
                   $"Хмарність: {cloudiness}%\n" +
                   $"Тиск: {pressure} гПа\n\n";
        }
        catch (Exception ex)
        {
            return $"Помилка при отриманні даних: {ex.Message}";
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Помилка1: {exception.Message}");
        return Task.CompletedTask;
    }
}
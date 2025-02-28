using Telegram.Bot;
using WeatherBot.Service;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using WeatherBot.Inteface;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("BotSettings"));
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddSingleton<TelegramBotClient>(serviceProvider =>
{
    var botSettings = serviceProvider.GetRequiredService<IOptions<BotSettings>>().Value;
    return new TelegramBotClient(botSettings.BotToken);
});

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<DatabaseService>(serviceProvider =>
{
    var connectionStrings = serviceProvider.GetRequiredService<IOptions<ConnectionStrings>>().Value;
    return new DatabaseService(connectionStrings.DefaultConnection);
});

builder.Services.AddSingleton<BotHostedService>();
builder.Services.AddHostedService<BotHostedService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeatherBot API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherBot API v1");
    });
}

app.UseRouting();
app.MapControllers();

await app.RunAsync();
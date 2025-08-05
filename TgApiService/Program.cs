using Microsoft.Extensions.Options;
using RabbitMqMessaging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TgApiService.Options;
using TgApiService.Services;

namespace TgApiService;

internal class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));

        builder.Services.AddHttpClient("tg_bot_client").RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramOptions? telegramOptions = sp.GetService<IOptions<TelegramOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(telegramOptions);
                TelegramBotClientOptions options = new(telegramOptions.Token);
                return new TelegramBotClient(options, httpClient);
            });

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IUserStateStorage, MemoryUserStateStorage>();

        builder.Services.AddRabbitMqMessaging(builder.Configuration);

        builder.Services.AddScoped<IUpdateHandler, UpdateHandler>();
        builder.Services.AddHostedService<BotHostedService>();

        IHost host = builder.Build();
        await host.RunAsync();
    }
}

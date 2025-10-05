using MassTransit;
using Microsoft.Extensions.Options;
using Serilog;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TgApiService.Cache;
using TgApiService.Options;
using TgApiService.Scenes.Common;
using TgApiService.Services;

namespace TgApiService;

internal class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog(lc => lc.ReadFrom.Configuration(builder.Configuration));

        builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));
        builder.Services.Configure<MessageBrokerOptions>(builder.Configuration.GetSection(MessageBrokerOptions.MessageBroker));

        builder.Services.AddHttpClient("tg_bot_client").RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramOptions? tgOptions = sp.GetService<IOptions<TelegramOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(tgOptions);
                TelegramBotClientOptions options = new(tgOptions.Token);
                return new TelegramBotClient(options, httpClient);
            });

        builder.Services.AddMassTransit(busCfg =>
        {
            MessageBrokerOptions? mbOptions =
                builder.Configuration.GetSection(MessageBrokerOptions.MessageBroker).Get<MessageBrokerOptions>();
            ArgumentNullException.ThrowIfNull(mbOptions);

            busCfg.SetKebabCaseEndpointNameFormatter();

            // Установка таймаута для клиентов запросов
            TimeSpan requestTimeout = TimeSpan.FromSeconds(10);

            // Траты
            busCfg.AddRequestClient<AddExpenseRequest>(requestTimeout);
            busCfg.AddRequestClient<GetExpensesRequest>(requestTimeout);

            // Категории
            busCfg.AddRequestClient<AddCategoryRequest>(requestTimeout);
            busCfg.AddRequestClient<GetCategoriesRequest>(requestTimeout);
            busCfg.AddRequestClient<DeleteCategoryRequest>(requestTimeout);

            busCfg.AddConsumer<UserCategoriesChangedConsumer>();

            busCfg.UsingRabbitMq((context, configuration) =>
            {
                configuration.Host(mbOptions.HostName, h =>
                {
                    h.Username(mbOptions.UserName);
                    h.Password(mbOptions.Password);
                });

                configuration.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(3)));

                configuration.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IStateCache, StateMemoryCache>();

        builder.Services.AddScoped<UpdateProcessor>();
        builder.Services.AddScoped<ISpendingTrackerGateway, SpendingTrackerGateway>();

        builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();
        builder.Services.AddHostedService<BotHostedService>();

        SceneRegistry.Bootstrap();

        IHost host = builder.Build();
        await host.RunAsync();
    }
}

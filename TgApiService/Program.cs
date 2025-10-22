using MassTransit;
using Microsoft.Extensions.Options;
using Serilog;
using SharedTypes;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TgApiService.Application.Abstractions;
using TgApiService.Configuration.Options;
using TgApiService.Infrastructure.Caching;
using TgApiService.Infrastructure.Gateways;
using TgApiService.Infrastructure.Messaging.Consumers;
using TgApiService.Presentation.Telegram.Common;
using TgApiService.Presentation.Telegram.Services;

namespace TgApiService;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog(lc => lc.ReadFrom.Configuration(builder.Configuration));

        builder.Services.AddOptions<TelegramOptions>()
            .Bind(builder.Configuration.GetRequiredSection(TelegramOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<MessageBrokerOptions>()
            .Bind(builder.Configuration.GetRequiredSection(MessageBrokerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpClient("tg_bot_client")
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramOptions telegram = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
                TelegramBotClientOptions botOptions = new(telegram.Token);
                return new TelegramBotClient(botOptions, httpClient);
            });

        builder.Services.AddMassTransit(busCfg =>
        {
            busCfg.SetKebabCaseEndpointNameFormatter();

            TimeSpan requestTimeout = TimeSpan.FromSeconds(10);

            // Клиенты для RPC-запросов
            busCfg.AddRequestClient<AddExpenseRequest>(requestTimeout);
            busCfg.AddRequestClient<GetExpensesRequest>(requestTimeout);
            busCfg.AddRequestClient<AddCategoryRequest>(requestTimeout);
            busCfg.AddRequestClient<GetCategoriesRequest>(requestTimeout);
            busCfg.AddRequestClient<DeleteCategoryRequest>(requestTimeout);
            busCfg.AddRequestClient<GetStatsFullWeekRequest>(requestTimeout);

            // Подписчики
            busCfg.AddConsumer<UserCategoriesChangedConsumer>();

            // Конфигурация RabbitMQ
            busCfg.UsingRabbitMq((context, cfg) =>
            {
                MessageBrokerOptions mq = context.GetRequiredService<IOptions<MessageBrokerOptions>>().Value;

                cfg.Host(mq.HostName, h =>
                {
                    h.Username(mq.UserName);
                    h.Password(mq.Password);
                });

                cfg.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(3)));

                cfg.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IStateCache, StateMemoryCache>();

        builder.Services.AddScoped<UpdateProcessor>();
        builder.Services.AddScoped<ISpendingTrackerGateway, SpendingTrackerGateway>();

        builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();
        builder.Services.AddHostedService<BotHostedService>();

        SceneRegistry.Bootstrap();

        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }));

        using IHost host = builder.Build();
        await host.RunAsync();
    }
}

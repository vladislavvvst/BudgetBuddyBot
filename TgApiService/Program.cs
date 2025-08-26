using MassTransit;
using Microsoft.Extensions.Options;
using SharedTypes;
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

            busCfg.AddRequestClient<GetExpenses>(new Uri($"queue:{mbOptions.GetExpensesQueueName}"));

            busCfg.UsingRabbitMq((context, configuration) =>
            {
                configuration.Host(mbOptions.HostName, h =>
                {
                    h.Username(mbOptions.UserName);
                    h.Password(mbOptions.Password);
                });
                configuration.ConfigureEndpoints(context);
            });

            EndpointConvention.Map<AddExpense>(new Uri($"queue:{mbOptions.AddExpenseQueueName}"));
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IUserStateStorage, MemoryUserStateStorage>();

        builder.Services.AddScoped<UpdateProcessor>();
        builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();
        builder.Services.AddHostedService<BotHostedService>();

        IHost host = builder.Build();
        await host.RunAsync();
    }
}

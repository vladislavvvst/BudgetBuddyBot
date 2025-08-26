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
                TelegramOptions? telegramOptions = sp.GetService<IOptions<TelegramOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(telegramOptions);
                TelegramBotClientOptions options = new(telegramOptions.Token);
                return new TelegramBotClient(options, httpClient);
            });

        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, configuration) =>
            {
                MessageBrokerOptions? messageBrokerOptions = context.GetService<IOptions<MessageBrokerOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(messageBrokerOptions);

                configuration.Host(messageBrokerOptions.HostName, h =>
                {
                    h.Username(messageBrokerOptions.UserName);
                    h.Password(messageBrokerOptions.Password);
                });

                EndpointConvention.Map<AddExpense>(new Uri($"queue:{messageBrokerOptions.AddExpenseQueueName}"));

                configuration.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IUserStateStorage, MemoryUserStateStorage>();

        builder.Services.AddScoped<IUpdateHandler, UpdateHandler>();
        builder.Services.AddHostedService<BotHostedService>();

        IHost host = builder.Build();
        await host.RunAsync();
    }
}

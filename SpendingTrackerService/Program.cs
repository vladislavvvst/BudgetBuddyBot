using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedTypes;
using SpendingTrackerService.Consumers;
using SpendingTrackerService.Database;

namespace SpendingTrackerService;

internal class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddDbContext<ApplicationDbContext>(
            options => { options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ApplicationDbContext))); });

        builder.Services.Configure<MessageBrokerOptions>(builder.Configuration.GetSection(MessageBrokerOptions.MessageBroker));

        builder.Services.AddMassTransit(busCfg =>
        {
            busCfg.SetKebabCaseEndpointNameFormatter();

            busCfg.AddConsumer<AddExpenseConsumer>();
            busCfg.AddConsumer<GetExpensesConsumer>();

            busCfg.AddConsumer<AddCategoryConsumer>();
            busCfg.AddConsumer<GetCategoriesConsumer>();
            busCfg.AddConsumer<DeleteCategoryConsumer>();

            busCfg.UsingRabbitMq((context, configuration) =>
            {
                MessageBrokerOptions? messageBrokerOptions = context.GetService<IOptions<MessageBrokerOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(messageBrokerOptions);

                configuration.Host(messageBrokerOptions.HostName, h =>
                {
                    h.Username(messageBrokerOptions.UserName);
                    h.Password(messageBrokerOptions.Password);
                });

                configuration.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(3)));

                configuration.UseInMemoryOutbox(context);

                configuration.PrefetchCount = 32;
                configuration.ConcurrentMessageLimit = 16;

                configuration.ConfigureEndpoints(context);
            });
        });

        IHost host = builder.Build();
        host.Run();
    }
}

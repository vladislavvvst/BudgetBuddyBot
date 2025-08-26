using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedTypes;
using SpendingTrackerService.Database;
using SpendingTrackerService.Services;

namespace SpendingTrackerService;

internal class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddDbContext<ApplicationDbContext>(
            options => { options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ApplicationDbContext))); });

        builder.Services.Configure<MessageBrokerOptions>(builder.Configuration.GetSection(MessageBrokerOptions.MessageBroker));

        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumer<AddExpenseConsumer>();
            busConfigurator.AddConsumer<GetExpensesConsumer>();

            busConfigurator.UsingRabbitMq((context, configuration) =>
            {
                MessageBrokerOptions? messageBrokerOptions = context.GetService<IOptions<MessageBrokerOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(messageBrokerOptions);

                configuration.Host(messageBrokerOptions.HostName, h =>
                {
                    h.Username(messageBrokerOptions.UserName);
                    h.Password(messageBrokerOptions.Password);
                });

                configuration.ReceiveEndpoint(messageBrokerOptions.AddExpenseQueueName, e =>
                {
                    e.ConfigureConsumer<AddExpenseConsumer>(context);
                    e.PrefetchCount = 4;
                    e.ConcurrentMessageLimit = 2;
                    e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)));
                });

                configuration.ReceiveEndpoint(messageBrokerOptions.GetExpensesQueueName, e =>
                {
                    e.ConfigureConsumer<GetExpensesConsumer>(context);
                    e.PrefetchCount = 4;
                    e.ConcurrentMessageLimit = 2;
                    e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)));
                });

                configuration.ConfigureEndpoints(context);
            });
        });

        IHost host = builder.Build();
        host.Run();
    }
}

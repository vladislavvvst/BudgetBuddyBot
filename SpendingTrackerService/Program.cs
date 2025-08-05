using RabbitMqMessaging;
using SpendingTrackerService.Services;

namespace SpendingTrackerService;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddRabbitMqMessaging(builder.Configuration);
        builder.Services.AddHostedService<SpendTrackerService>();

        var host = builder.Build();
        host.Run();
    }
}

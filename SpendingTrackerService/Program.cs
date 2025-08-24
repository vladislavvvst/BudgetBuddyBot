using Microsoft.EntityFrameworkCore;
using RabbitMqMessaging;
using SpendingTrackerService.Database;
using SpendingTrackerService.Services;

namespace SpendingTrackerService;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddDbContext<ExpenseDbContext>(
            options => { options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ExpenseDbContext))); });

        builder.Services.AddRabbitMqMessaging(builder.Configuration);

        builder.Services.AddScoped<ExpensesRepository>();
        builder.Services.AddHostedService<SpendTrackerService>();

        var host = builder.Build();
        host.Run();
    }
}

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using SharedTypes;
using SpendingTrackerService.Api.Consumers;
using SpendingTrackerService.Infrastructure.Persistence;
using SpendingTrackerService.Infrastructure.Repositories;

namespace SpendingTrackerService;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog(lc => lc.ReadFrom.Configuration(builder.Configuration));

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(ApplicationDbContext)));
        });

        builder.Services.Configure<MessageBrokerOptions>(
            builder.Configuration.GetSection(MessageBrokerOptions.SectionName));

        builder.Services.AddMassTransit(busCfg =>
        {
            busCfg.SetKebabCaseEndpointNameFormatter();

            busCfg.AddEntityFrameworkOutbox<ApplicationDbContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            busCfg.AddConsumer<AddExpenseConsumer>();
            busCfg.AddConsumer<GetExpensesConsumer>();
            busCfg.AddConsumer<AddCategoryConsumer>();
            busCfg.AddConsumer<GetCategoriesConsumer>();
            busCfg.AddConsumer<DeleteCategoryConsumer>();

            busCfg.UsingRabbitMq((context, cfg) =>
            {
                MessageBrokerOptions? opts = context.GetService<IOptions<MessageBrokerOptions>>()?.Value;
                ArgumentNullException.ThrowIfNull(opts);

                cfg.Host(opts.HostName, h =>
                {
                    h.Username(opts.UserName);
                    h.Password(opts.Password);
                });

                cfg.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(3)));

                cfg.PrefetchCount = 32;
                cfg.ConcurrentMessageLimit = 16;

                cfg.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();

        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }));

        using IHost host = builder.Build();
        await host.RunAsync();
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqMessaging.Connection;
using RabbitMqMessaging.Publisher;
using RabbitMqMessaging.Subscriber;

namespace RabbitMqMessaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<RabbitMqOptions>(cfg.GetSection(RabbitMqOptions.RabbitMq));
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddSingleton(typeof(IMessageSubscriber<>), typeof(RabbitMqSubscriber<>));
        return services;
    }
}

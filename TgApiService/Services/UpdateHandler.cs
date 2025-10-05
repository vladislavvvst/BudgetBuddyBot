using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TgApiService.Services;

/// <summary>
/// Адаптер между Telegram-клиентом и приложением.
/// Реализует IUpdateHandler: на каждый update создает DI-scope и передает управление UpdateProcessor.
/// </summary>
internal sealed class UpdateHandler : IUpdateHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateHandler(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task HandleUpdateAsync
    (
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken
    )
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        UpdateProcessor processor = scope.ServiceProvider.GetRequiredService<UpdateProcessor>();
        await processor.HandleUpdateAsync(botClient, update, cancellationToken);
    }

    public async Task HandleErrorAsync
    (
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken
    )
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        UpdateProcessor processor = scope.ServiceProvider.GetRequiredService<UpdateProcessor>();
        await processor.HandleErrorAsync(botClient, exception, source, cancellationToken);
    }
}

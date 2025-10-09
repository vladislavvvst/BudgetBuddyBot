using System.Collections.Immutable;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TgApiService.Presentation.Telegram.Features.UI;

namespace TgApiService.Presentation.Telegram.Services;

/// <summary>
/// Hosted-сервис, который запускает polling Telegram-бота в фоновом потоке.
/// Регистрируется в DI как IHostedService, стартует при запуске приложения.
/// </summary>
internal class BotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotHostedService> _logger;
    private readonly IUpdateHandler _updateHandler;

    public BotHostedService
    (
        ITelegramBotClient botClient, IUpdateHandler updateHandler,
        ILogger<BotHostedService> logger
    )
    {
        _logger = logger;
        _updateHandler = updateHandler;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting polling service");

        await EnsureCommandsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ReceiverOptions receiverOptions = new() { DropPendingUpdates = true, AllowedUpdates = [] };

                await _botClient.ReceiveAsync
                (
                    updateHandler: _updateHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Polling failed with exception: {Exception}", ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    // Глобальные команды для бота (устанавливаются один раз при старте)

    private readonly record struct CommandInfo(string Command, string Description);

    private static readonly ImmutableArray<CommandInfo> All =
    [
        new(UiStrings.Commands.Start, UiStrings.Buttons.BotStart),
        new(UiStrings.Commands.Menu, UiStrings.Buttons.BotMenu),
        new(UiStrings.Commands.About, UiStrings.Buttons.BotAbout)
    ];

    private async Task EnsureCommandsAsync(CancellationToken cancellationToken) =>
        await _botClient.SetMyCommands(
            All.Select(c => new BotCommand { Command = c.Command, Description = c.Description }),
            cancellationToken: cancellationToken);
}

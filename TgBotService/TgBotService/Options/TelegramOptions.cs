namespace TgBotService.Options;

internal class TelegramOptions
{
    public const string Telegram = nameof(Telegram);
    public string Token { get; set; } = default!;
}

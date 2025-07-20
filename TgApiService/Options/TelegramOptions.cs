namespace TgApiService.Options;

internal class TelegramOptions
{
    public const string Telegram = nameof(Telegram);
    public string Token { get; set; } = default!;
    public long UserId { get; set; } = default!;
}

namespace TgApiService.Options;

internal class TelegramOptions
{
    public const string Telegram = nameof(Telegram);
    public string Token { get; set; } = default!;
    public long AdminId { get; set; } = default!;
    public long TestUserId { get; set; } = default!;
}

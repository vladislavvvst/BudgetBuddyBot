using System.Collections.Immutable;
using Telegram.Bot.Types;

namespace TgApiService.Entities;

internal readonly record struct CommandInfo(string Command, string Description);

internal static class BotCommands
{
    public static readonly ImmutableArray<CommandInfo> All =
    [
        new CommandInfo("start", "Запустить бота"),
        new CommandInfo("menu", "Открыть меню"),
        new CommandInfo("about", "О боте")
    ];

    public static IEnumerable<BotCommand> ToTelegram() =>
        All.Select(c => new BotCommand { Command = c.Command, Description = c.Description });
}

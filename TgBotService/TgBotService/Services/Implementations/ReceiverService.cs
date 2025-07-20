using Telegram.Bot;
using TgBotService.Services.Abstract;

namespace TgBotService.Services.Implementations;

internal class ReceiverService : ReceiverServiceBase<UpdateHandlerService>
{
    public ReceiverService(ILogger<ReceiverService> logger, ITelegramBotClient botClient, UpdateHandlerService updateHandler)
        : base(logger, botClient, updateHandler)
    {

    }
}

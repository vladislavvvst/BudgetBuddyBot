using Telegram.Bot;
using TgApiService.Services.Abstract;

namespace TgApiService.Services.Implementations;

internal class ReceiverService : ReceiverServiceBase<UpdateHandlerService>
{
    public ReceiverService(ILogger<ReceiverService> logger, ITelegramBotClient botClient, UpdateHandlerService updateHandler)
        : base(logger, botClient, updateHandler)
    {

    }
}

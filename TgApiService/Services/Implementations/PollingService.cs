using TgApiService.Services.Abstract;

namespace TgApiService.Services.Implementations;

internal class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(ILogger<PollingService> logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {

    }
}

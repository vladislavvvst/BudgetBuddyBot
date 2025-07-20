namespace TgApiService.Services.Abstract;

interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}

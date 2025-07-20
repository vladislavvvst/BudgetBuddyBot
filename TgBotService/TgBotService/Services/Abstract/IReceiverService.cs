namespace TgBotService.Services.Abstract;

interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}

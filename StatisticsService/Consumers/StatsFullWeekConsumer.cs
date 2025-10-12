using MassTransit;
using SharedTypes;

namespace StatisticsService.Consumers;

internal sealed class StatsFullWeekConsumer : IConsumer<GetStatsFullWeekRequest>
{
    private readonly ILogger<StatsFullWeekConsumer> _logger;

    public StatsFullWeekConsumer(ILogger<StatsFullWeekConsumer> logger)
        => _logger = logger;

    public async Task Consume(ConsumeContext<GetStatsFullWeekRequest> context)
    {
        long userId = context.Message.UserId;

        _logger.LogInformation("Received GetStatsFullWeekRequest for user {UserId}", userId);

        Random rnd = new Random((int)(userId % int.MaxValue));

        decimal total = rnd.Next(2000, 10000);
        decimal avgPerDay = Math.Round(total / 7m, 2);
        decimal largestExpense = rnd.Next(300, 1200);

        IEnumerable<(string, decimal)> categories = new[]
        {
            ("Еда", rnd.Next(500, 2000)),
            ("Транспорт", rnd.Next(300, 800)),
            ("Развлечения", rnd.Next(400, 1000)),
            ("Подписки", rnd.Next(200, 600)),
            ("Прочее", rnd.Next(100, 400))
        }.Select(c => (c.Item1, (decimal)c.Item2));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IEnumerable<(DateTimeOffset Date, decimal Amount)> days = Enumerable
            .Range(0, 7)
            .Select(i => (now.AddDays(-i), (decimal)rnd.Next(100, 1500)))
            .OrderBy(x => x.Item1);

        GetStatsFullWeekResponse response = new
        (
            total,
            avgPerDay,
            largestExpense,
            categories,
            days
        );

        _logger.LogInformation(
            "Returning fake weekly stats for user {UserId}: Total={Total}, AvgPerDay={Avg}, Largest={Largest}",
            userId, total, avgPerDay, largestExpense
        );

        await context.RespondAsync(response);
    }
}

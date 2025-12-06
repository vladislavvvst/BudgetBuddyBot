using MassTransit;
using SharedTypes.Contracts;

namespace StatisticsService.Api.Consumers;

/// <summary>
/// Партиционирование обработки по UserId, чтобы события одного пользователя шли последовательно и не ловили гонки в БД
/// </summary>
internal sealed class ExpenseAddedConsumerDefinition : ConsumerDefinition<ExpenseAddedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ExpenseAddedConsumer> consumerConfigurator, IRegistrationContext context)
    {
        const int partitions = 32;

        consumerConfigurator.Message<ExpenseAddedNotification>(m =>
            m.UsePartitioner(partitions, ctx => ctx.Message.UserId));
    }
}

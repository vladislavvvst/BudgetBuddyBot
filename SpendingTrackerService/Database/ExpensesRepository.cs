namespace SpendingTrackerService.Database;

/// <summary>
/// Предоставляет методы доступа к данным для управления расходами в базе данных
/// </summary>
/// <remarks>
/// Этот репозиторий отвечает за взаимодействие с контекстом базы данных для выполнения CRUD-операций с данными о расходах
/// Он предназначен для инкапсуляции логики доступа к данным и предоставляет чистый API для работы с расходами
/// </remarks>
internal class ExpensesRepository
{
    private readonly ExpenseDbContext _dbContext;

    public ExpensesRepository(ExpenseDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}

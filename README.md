# BudgetBuddyBot - телеграм-бот для учета расходов

Telegram-бот для личных финансов: быстрый ввод трат, категории и наглядная статистика

---

## Возможности

- Добавление трат одним сообщением (сумма + комментарий)
- Управление категориями (добавление, удаление)
- Статистика: сегодня, 7 дней, месяц; топ категорий; динамика по дням
- Последние 10 трат

> Кастомный период пока не реализован

---

## Роадмап

- Повторяющиеся платежи и напоминания
- Экспорт/импорт (CSV/Excel)
- Сканы чеков, парсинг и автоматическая категоризация

---

## Команды

- `/start` - запуск бота
- `/menu` - главное меню
- `/about` - информация о боте
- `/cancel` - отмена действия и возврат в меню

---

## Архитектура

- **TgApiService** - Telegram UI, состояния пользователя, RPC-запросы к сервисам
- **SpendingTrackerService** - доменная логика трат и категорий, публикация событий
- **StatisticsService** - агрегаты и отчеты по тратам
- **Shared/SharedTypes** - общие контракты и DTO

---

## Поток данных (кратко)

1. TgApiService отправляет `AddExpenseRequest` в SpendingTrackerService
2. SpendingTrackerService сохраняет трату и публикует `ExpenseAddedNotification`
3. StatisticsService обновляет агрегаты
4. TgApiService запрашивает статистику по RPC

---

## Запуск через Docker Compose (dev)

1. Создайте файл `.env` в корне проекта и укажите чувствительные значения:

```
TELEGRAM_TOKEN=ваш_токен
TELEGRAM_ADMIN_ID=12345
TELEGRAM_TEST_USER_ID=12345

RABBITMQ_USER=rabbitmq
RABBITMQ_PASSWORD=secret

SPENDING_DB_NAME=spending_db
SPENDING_DB_USER=spending_user
SPENDING_DB_PASSWORD=secret

STATS_DB_NAME=stats_db
STATS_DB_USER=stats_user
STATS_DB_PASSWORD=secret
```

2. Соберите и запустите:

```
docker compose -f docker-compose.dev.yml up --build
```

При первом запуске миграции применяются автоматически при старте сервисов

P.S. Пересоздать контейнер с новыми данными

```
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up --build
```

---

## Технологии

- C# 12, .NET 8
- MassTransit + RabbitMQ
- EF Core + PostgreSQL
- Serilog
- Telegram.Bot SDK
- Docker

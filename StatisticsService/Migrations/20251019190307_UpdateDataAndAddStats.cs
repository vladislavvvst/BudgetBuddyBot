using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StatisticsService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataAndAddStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_stats_daily",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountTotal = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    ExpensesCount = table.Column<int>(type: "integer", nullable: false),
                    MaxExpenseAmount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    MaxExpenseNote = table.Column<string>(type: "text", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stats_daily", x => new { x.UserId, x.Day });
                });

            migrationBuilder.CreateTable(
                name: "user_stats_daily_by_category",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    AmountTotal = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    MaxExpenseAmount = table.Column<decimal>(type: "numeric(19,2)", nullable: false),
                    MaxExpenseNote = table.Column<string>(type: "text", nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpensesCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stats_daily_by_category", x => new { x.UserId, x.Day, x.CategoryId });
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_stats_daily_day",
                table: "user_stats_daily",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "ix_user_stats_daily_user_day_desc",
                table: "user_stats_daily",
                columns: new[] { "UserId", "Day" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_user_stats_dbc_day",
                table: "user_stats_daily_by_category",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "ix_user_stats_dbc_user_day_amount_desc",
                table: "user_stats_daily_by_category",
                columns: new[] { "UserId", "Day", "AmountTotal" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_user_stats_dbc_user_day_desc",
                table: "user_stats_daily_by_category",
                columns: new[] { "UserId", "Day" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_stats_daily");

            migrationBuilder.DropTable(
                name: "user_stats_daily_by_category");
        }
    }
}

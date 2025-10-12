using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendingTrackerService.Migrations
{
    /// <inheritdoc />
    public partial class AlignUtcDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AddedAtUtc",
                table: "expenses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AddedAtUtc",
                table: "categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "timezone('utc', now())",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AddedAtUtc",
                table: "expenses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "timezone('utc', now())");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AddedAtUtc",
                table: "categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "timezone('utc', now())");
        }
    }
}

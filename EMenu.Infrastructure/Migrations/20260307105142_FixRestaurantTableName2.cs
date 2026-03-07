using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMenu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRestaurantTableName2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSessions_RestaurantTables_RestaurantTableTableID",
                table: "OrderSessions");

            migrationBuilder.DropIndex(
                name: "IX_OrderSessions_RestaurantTableTableID",
                table: "OrderSessions");

            migrationBuilder.DropColumn(
                name: "RestaurantTableTableID",
                table: "OrderSessions");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSessions_TableID",
                table: "OrderSessions",
                column: "TableID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSessions_RestaurantTables_TableID",
                table: "OrderSessions",
                column: "TableID",
                principalTable: "RestaurantTables",
                principalColumn: "TableID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSessions_RestaurantTables_TableID",
                table: "OrderSessions");

            migrationBuilder.DropIndex(
                name: "IX_OrderSessions_TableID",
                table: "OrderSessions");

            migrationBuilder.AddColumn<int>(
                name: "RestaurantTableTableID",
                table: "OrderSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderSessions_RestaurantTableTableID",
                table: "OrderSessions",
                column: "RestaurantTableTableID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSessions_RestaurantTables_RestaurantTableTableID",
                table: "OrderSessions",
                column: "RestaurantTableTableID",
                principalTable: "RestaurantTables",
                principalColumn: "TableID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

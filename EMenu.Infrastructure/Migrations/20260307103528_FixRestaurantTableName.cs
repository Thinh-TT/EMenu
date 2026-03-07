using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMenu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRestaurantTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSessions_Table_RestaurantTableTableID",
                table: "OrderSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Table",
                table: "Table");

            migrationBuilder.RenameTable(
                name: "Table",
                newName: "RestaurantTables");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RestaurantTables",
                table: "RestaurantTables",
                column: "TableID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSessions_RestaurantTables_RestaurantTableTableID",
                table: "OrderSessions",
                column: "RestaurantTableTableID",
                principalTable: "RestaurantTables",
                principalColumn: "TableID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSessions_RestaurantTables_RestaurantTableTableID",
                table: "OrderSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RestaurantTables",
                table: "RestaurantTables");

            migrationBuilder.RenameTable(
                name: "RestaurantTables",
                newName: "Table");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Table",
                table: "Table",
                column: "TableID");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSessions_Table_RestaurantTableTableID",
                table: "OrderSessions",
                column: "RestaurantTableTableID",
                principalTable: "Table",
                principalColumn: "TableID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

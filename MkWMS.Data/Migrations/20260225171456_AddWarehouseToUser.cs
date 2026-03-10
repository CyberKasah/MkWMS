using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MkWMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
    name: "WarehouseId",
    table: "Пользователи",
    type: "int",
    nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Пользователи_WarehouseId",
                table: "Пользователи",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Пользователи_Склады_WarehouseId",
                table: "Пользователи",
                column: "WarehouseId",
                principalTable: "Склады",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

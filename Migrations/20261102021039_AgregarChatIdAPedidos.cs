using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PupuseriaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarChatIdAPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChatId",
                table: "Pedidos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "Pedidos");
        }
    }
}

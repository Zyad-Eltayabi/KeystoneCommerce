using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeystoneCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFulfilledToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFulfilled",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFulfilled",
                table: "Payments");
        }
    }
}

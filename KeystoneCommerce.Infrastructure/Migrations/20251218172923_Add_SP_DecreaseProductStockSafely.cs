using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeystoneCommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_SP_DecreaseProductStockSafely : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                        CREATE OR ALTER PROCEDURE SP_DecreaseProductQty
                        (
                            @ProductId  INT,
                            @ProductQty INT
                        )
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SET XACT_ABORT ON;
                            -- Guard clauses
                            IF @ProductId <= 0 OR @ProductQty <= 0
                                THROW 50010, 'Invalid product id or quantity', 1;
                            BEGIN TRY
                                BEGIN TRANSACTION;
                                UPDATE p
                                SET p.QTY = p.QTY - @ProductQty
                                FROM Products AS p WITH (UPDLOCK, ROWLOCK)
                                WHERE p.Id = @ProductId
                                  AND p.QTY >= @ProductQty;
                                IF @@ROWCOUNT = 0
                                    THROW 50011, 'Insufficient stock or product not found', 1;
                                COMMIT TRANSACTION;
                            END TRY
                            BEGIN CATCH
                                IF @@TRANCOUNT > 0
                                    ROLLBACK TRANSACTION;
                                THROW;
                            END CATCH
                        END
                        GO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP PROCEDURE IF EXISTS SP_DecreaseProductQty;GO");
        }
    }
}

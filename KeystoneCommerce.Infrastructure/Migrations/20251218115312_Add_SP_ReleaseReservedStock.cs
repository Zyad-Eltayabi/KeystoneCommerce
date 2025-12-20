using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeystoneCommerce.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Add_SP_ReleaseReservedStock : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"CREATE OR ALTER PROCEDURE SP_ReleaseReservedStock
                        (
                            @OrderId INT
                        )
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SET XACT_ABORT ON;

                            BEGIN TRY
                                BEGIN TRANSACTION;

                                UPDATE p
                                SET p.QTY = p.QTY + oi.Quantity
                                FROM Products p
                                INNER JOIN OrderItems oi 
                                    ON oi.ProductId = p.Id
                                WHERE oi.OrderId = @OrderId;

                                IF @@ROWCOUNT = 0
                                BEGIN
                                    THROW 50001, 'Stock already released or order not found', 1;
                                END

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
            "DROP PROCEDURE IF EXISTS SP_ReleaseReservedStock");
    }
}

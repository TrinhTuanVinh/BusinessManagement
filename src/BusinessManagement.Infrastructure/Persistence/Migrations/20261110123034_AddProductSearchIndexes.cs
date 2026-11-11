using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("""
                                CREATE INDEX "IX_Products_Name_trgm" ON "Products" USING gin ("Name" gin_trgm_ops);
                                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Products_Name_trgm";""");
            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

        }
    }
}

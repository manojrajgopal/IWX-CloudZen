using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class KeyPair001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyPairRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyPairId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    KeyFingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    KeyType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrivateKeyMaterial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicKeyMaterial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsImported = table.Column<bool>(type: "bit", nullable: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AwsCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyPairRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyPairRecords");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class ECR001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EcrImageRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepositoryRecordId = table.Column<int>(type: "int", nullable: false),
                    RepositoryName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ImageTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageDigest = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    ScanStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FindingsCritical = table.Column<int>(type: "int", nullable: true),
                    FindingsHigh = table.Column<int>(type: "int", nullable: true),
                    FindingsMedium = table.Column<int>(type: "int", nullable: true),
                    FindingsLow = table.Column<int>(type: "int", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PushedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcrImageRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EcrRepositoryRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepositoryName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RepositoryArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RepositoryUri = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ImageTagMutability = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScanOnPush = table.Column<bool>(type: "bit", nullable: false),
                    EncryptionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcrRepositoryRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EcrImageRecords");

            migrationBuilder.DropTable(
                name: "EcrRepositoryRecords");
        }
    }
}

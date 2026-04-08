using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class CloudWatchLogs001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogGroupRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogGroupName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Arn = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RetentionInDays = table.Column<int>(type: "int", nullable: true),
                    StoredBytes = table.Column<long>(type: "bigint", nullable: false),
                    MetricFilterCount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KmsKeyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataProtectionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LogGroupClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogGroupRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogStreamRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogStreamName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Arn = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    LogGroupName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    LogGroupRecordId = table.Column<int>(type: "int", nullable: false),
                    FirstEventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastIngestionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StoredBytes = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogStreamRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogGroupRecords");

            migrationBuilder.DropTable(
                name: "LogStreamRecords");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class ECS001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EcsServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ServiceArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClusterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClusterArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaskDefinition = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DesiredCount = table.Column<int>(type: "int", nullable: false),
                    RunningCount = table.Column<int>(type: "int", nullable: false),
                    PendingCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LaunchType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SchedulingStrategy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NetworkConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ServiceCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcsServiceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EcsTaskDefinitionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Family = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaskDefinitionArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cpu = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Memory = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NetworkMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExecutionRoleArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaskRoleArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresCompatibilities = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OsFamily = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ContainerDefinitionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContainerCount = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcsTaskDefinitionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EcsTaskRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClusterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClusterArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaskDefinitionArn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Group = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DesiredStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cpu = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Memory = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LaunchType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Connectivity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StopCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StoppedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StoppedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PullStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PullStoppedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcsTaskRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EcsServiceRecords");

            migrationBuilder.DropTable(
                name: "EcsTaskDefinitionRecords");

            migrationBuilder.DropTable(
                name: "EcsTaskRecords");
        }
    }
}

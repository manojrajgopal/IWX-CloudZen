using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class SecurityGroups001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityGroupRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SecurityGroupId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VpcId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InboundRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutboundRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityGroupRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityGroupRecords");
        }
    }
}

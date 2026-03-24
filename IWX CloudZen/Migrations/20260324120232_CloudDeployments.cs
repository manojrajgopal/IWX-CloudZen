using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class CloudDeployments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CloudDeployments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    DeploymentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PackagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudDeployments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudDeployments");
        }
    }
}

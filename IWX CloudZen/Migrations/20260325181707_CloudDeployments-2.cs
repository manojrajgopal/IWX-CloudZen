using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class CloudDeployments2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClusterName",
                table: "CloudDeployments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthUrl",
                table: "CloudDeployments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CloudDeployments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogsGroup",
                table: "CloudDeployments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "CloudDeployments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterName",
                table: "CloudDeployments");

            migrationBuilder.DropColumn(
                name: "HealthUrl",
                table: "CloudDeployments");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CloudDeployments");

            migrationBuilder.DropColumn(
                name: "LogsGroup",
                table: "CloudDeployments");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "CloudDeployments");
        }
    }
}

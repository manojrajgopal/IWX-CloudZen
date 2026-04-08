using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class Subnet001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubnetRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubnetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    VpcId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CidrBlock = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ipv6CidrBlock = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    AvailabilityZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvailabilityZoneId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    State = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AvailableIpAddressCount = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    MapPublicIpOnLaunch = table.Column<bool>(type: "bit", nullable: false),
                    AssignIpv6AddressOnCreation = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubnetRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubnetRecords");
        }
    }
}

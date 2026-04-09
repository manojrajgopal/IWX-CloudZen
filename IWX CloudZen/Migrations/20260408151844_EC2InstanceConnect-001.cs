using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class EC2InstanceConnect001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ec2InstanceConnectEndpointRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EndpointId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubnetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VpcId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DnsName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NetworkInterfaceId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AvailabilityZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FipsDnsName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PreserveClientIp = table.Column<bool>(type: "bit", nullable: false),
                    SecurityGroupIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ec2InstanceConnectEndpointRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ec2InstanceConnectSessionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstanceOsUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AvailabilityZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SessionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ec2InstanceConnectSessionRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ec2InstanceConnectEndpointRecords");

            migrationBuilder.DropTable(
                name: "Ec2InstanceConnectSessionRecords");
        }
    }
}

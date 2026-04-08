using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class EC2001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ec2InstanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstanceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PublicIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrivateIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VpcId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubnetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Architecture = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Monitoring = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EbsOptimized = table.Column<bool>(type: "bit", nullable: false),
                    SecurityGroupsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaunchTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CloudAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ec2InstanceRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ec2InstanceRecords");
        }
    }
}

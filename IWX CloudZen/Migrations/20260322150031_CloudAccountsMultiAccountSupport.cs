using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IWX_CloudZen.Migrations
{
    /// <inheritdoc />
    public partial class CloudAccountsMultiAccountSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessKey",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "SecretKey",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CloudAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "CloudAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "CloudAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "CloudAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AccountName",
                table: "CloudAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AccessKeyEncrypted",
                table: "CloudAccounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientIdEncrypted",
                table: "CloudAccounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSecretEncrypted",
                table: "CloudAccounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "CloudAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastValidatedAt",
                table: "CloudAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretKeyEncrypted",
                table: "CloudAccounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantIdEncrypted",
                table: "CloudAccounts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CloudAccounts_UserEmail_Provider_AccountName",
                table: "CloudAccounts",
                columns: new[] { "UserEmail", "Provider", "AccountName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CloudAccounts_UserEmail_Provider_AccountName",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "AccessKeyEncrypted",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "ClientIdEncrypted",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "ClientSecretEncrypted",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "LastValidatedAt",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "SecretKeyEncrypted",
                table: "CloudAccounts");

            migrationBuilder.DropColumn(
                name: "TenantIdEncrypted",
                table: "CloudAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "AccountName",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AccessKey",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecretKey",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "CloudAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

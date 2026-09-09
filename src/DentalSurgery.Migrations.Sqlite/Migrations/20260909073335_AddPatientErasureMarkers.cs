using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalSurgery.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientErasureMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ErasedAtUtc",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErasureReason",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsErased",
                table: "Patients",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErasedAtUtc",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ErasureReason",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IsErased",
                table: "Patients");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PIPDC.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentReadAtAndEnquiryStatusUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AgentReadAt",
                table: "Enquiries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Enquiries\" SET \"Status\" = 'InProgress' WHERE \"Status\" = 'Responded';");

            migrationBuilder.Sql(
                "UPDATE \"Enquiries\" SET \"Status\" = 'Resolved' WHERE \"Status\" = 'Closed';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Enquiries\" SET \"Status\" = 'Closed' WHERE \"Status\" = 'Resolved';");

            migrationBuilder.Sql(
                "UPDATE \"Enquiries\" SET \"Status\" = 'Responded' WHERE \"Status\" = 'InProgress';");

            migrationBuilder.DropColumn(
                name: "AgentReadAt",
                table: "Enquiries");
        }
    }
}

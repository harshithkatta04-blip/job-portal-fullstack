using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeJobType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
        """
        UPDATE "Jobs"
        SET "JobType" = CASE
            WHEN LOWER(REPLACE(REPLACE("JobType", '-', ''), ' ', ''))
                = 'fulltime' THEN 'FullTime'
            WHEN LOWER(REPLACE(REPLACE("JobType", '-', ''), ' ', ''))
                = 'parttime' THEN 'PartTime'
            WHEN LOWER(REPLACE(REPLACE("JobType", '-', ''), ' ', ''))
                = 'contract' THEN 'Contract'
            WHEN LOWER(REPLACE(REPLACE("JobType", '-', ''), ' ', ''))
                = 'internship' THEN 'Internship'
            ELSE "JobType"
        END;
        """);
            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "Jobs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "Jobs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}

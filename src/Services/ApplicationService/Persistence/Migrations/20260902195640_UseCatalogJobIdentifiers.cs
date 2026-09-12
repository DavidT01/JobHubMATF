using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApplicationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseCatalogJobIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GUID values cannot be inferred from Catalog ObjectIds. Require an explicit data migration.
            migrationBuilder.Sql("""
                LOCK TABLE job_applications IN ACCESS EXCLUSIVE MODE;
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM job_applications) THEN
                        RAISE EXCEPTION 'Existing applications require an explicit GUID-to-Catalog-ID mapping before this migration. No application data was changed.';
                    END IF;
                END $$;
                ALTER TABLE job_applications
                    ALTER COLUMN job_id TYPE character varying(24) USING job_id::text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                LOCK TABLE job_applications IN ACCESS EXCLUSIVE MODE;
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM job_applications) THEN
                        RAISE EXCEPTION 'Cannot revert Catalog identifiers while applications exist. An explicit data migration is required.';
                    END IF;
                END $$;
                ALTER TABLE job_applications
                    ALTER COLUMN job_id TYPE uuid USING job_id::uuid;
                """);
        }
    }
}

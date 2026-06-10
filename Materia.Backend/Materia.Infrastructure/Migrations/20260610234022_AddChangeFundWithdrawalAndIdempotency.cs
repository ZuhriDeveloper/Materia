using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeFundWithdrawalAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Written defensively with IF [NOT] EXISTS so the migration is re-runnable: a prior
            // run may have partially applied (added the columns) without being recorded in the
            // migrations history, which would make a plain AddColumn fail with "already exists".

            migrationBuilder.Sql(
                @"ALTER TABLE ""PettyCashExpenseReadModels""
                  ADD COLUMN IF NOT EXISTS ""IdempotencyKey"" uuid NOT NULL
                  DEFAULT '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.Sql(
                @"ALTER TABLE ""ChangeFundDepositReadModels""
                  ADD COLUMN IF NOT EXISTS ""IdempotencyKey"" uuid NOT NULL
                  DEFAULT '00000000-0000-0000-0000-000000000000';");

            // Backfill rows that still carry the empty key with distinct values so the new unique
            // (StoreId, IdempotencyKey) index can be created — every such row would otherwise
            // collide on Guid.Empty. These rows predate idempotency, so any unique value is fine.
            // Guarded by WHERE so a re-run never disturbs rows already given a real key.
            migrationBuilder.Sql(
                @"UPDATE ""PettyCashExpenseReadModels"" SET ""IdempotencyKey"" = gen_random_uuid()
                  WHERE ""IdempotencyKey"" = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql(
                @"UPDATE ""ChangeFundDepositReadModels"" SET ""IdempotencyKey"" = gen_random_uuid()
                  WHERE ""IdempotencyKey"" = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.Sql(
                @"CREATE TABLE IF NOT EXISTS ""ChangeFundWithdrawalReadModels"" (
                      ""Id"" uuid NOT NULL,
                      ""StoreId"" uuid NOT NULL,
                      ""Amount"" numeric(18,2) NOT NULL,
                      ""Reason"" character varying(300) NOT NULL,
                      ""RecordedBy"" character varying(100) NOT NULL,
                      ""RecordedAt"" timestamp with time zone NOT NULL,
                      ""IdempotencyKey"" uuid NOT NULL,
                      CONSTRAINT ""PK_ChangeFundWithdrawalReadModels"" PRIMARY KEY (""Id"")
                  );");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PettyCashExpenseReadModels_StoreId_IdempotencyKey""
                  ON ""PettyCashExpenseReadModels"" (""StoreId"", ""IdempotencyKey"");");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ChangeFundDepositReadModels_StoreId_IdempotencyKey""
                  ON ""ChangeFundDepositReadModels"" (""StoreId"", ""IdempotencyKey"");");

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_ChangeFundWithdrawalReadModels_RecordedAt""
                  ON ""ChangeFundWithdrawalReadModels"" (""RecordedAt"");");

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_ChangeFundWithdrawalReadModels_StoreId""
                  ON ""ChangeFundWithdrawalReadModels"" (""StoreId"");");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ChangeFundWithdrawalReadModels_StoreId_IdempotencyKey""
                  ON ""ChangeFundWithdrawalReadModels"" (""StoreId"", ""IdempotencyKey"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ChangeFundWithdrawalReadModels"";");

            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_PettyCashExpenseReadModels_StoreId_IdempotencyKey"";");
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_ChangeFundDepositReadModels_StoreId_IdempotencyKey"";");

            migrationBuilder.Sql(
                @"ALTER TABLE ""PettyCashExpenseReadModels"" DROP COLUMN IF EXISTS ""IdempotencyKey"";");
            migrationBuilder.Sql(
                @"ALTER TABLE ""ChangeFundDepositReadModels"" DROP COLUMN IF EXISTS ""IdempotencyKey"";");
        }
    }
}

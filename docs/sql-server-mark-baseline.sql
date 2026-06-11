/*
Human-run production script.

This script DOES NOT apply the baseline migration. It records that the existing
schema has been independently verified to match:
  20260611161345_InitialSqlServer (EF Core 10.0.3)

Run docs/sql-server-drift-check.sql first. Do not proceed unless it reports
READY TO BASELINE with zero errors.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MigrationId nvarchar(150) = N'20260611161345_InitialSqlServer';
DECLARE @ProductVersion nvarchar(32) = N'10.0.3';

-- Required operator confirmations. Change all three values before execution.
DECLARE @ConfirmedDatabaseName sysname = N'REPLACE_WITH_PRODUCTION_DATABASE_NAME';
DECLARE @ConfirmedCleanDriftCheckForMigrationId nvarchar(150) = N'NOT_CONFIRMED';
DECLARE @ConfirmedBackupTaken bit = 0;

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 51000, 'Refusing to run against a SQL Server system database.', 1;

IF DB_NAME() <> @ConfirmedDatabaseName
    THROW 51001, 'DB_NAME() does not match @ConfirmedDatabaseName.', 1;

IF @ConfirmedCleanDriftCheckForMigrationId <> @MigrationId
    THROW 51002, 'The clean drift check has not been confirmed for this migration ID.', 1;

IF @ConfirmedBackupTaken <> 1
    THROW 51003, 'A verified backup must be confirmed before baselining.', 1;

DECLARE @ExpectedTables TABLE (TableName sysname PRIMARY KEY);
INSERT INTO @ExpectedTables (TableName) VALUES
    (N'AiProviderSettings'), (N'ApplicationSettings'), (N'AspNetRoleClaims'),
    (N'AspNetRoles'), (N'AspNetUserClaims'), (N'AspNetUserLogins'),
    (N'AspNetUserRoles'), (N'AspNetUsers'), (N'AspNetUserTokens'),
    (N'AiAuditLogs'), (N'AiChatSessionMessages'), (N'AiChatSessions'),
    (N'ClubMemberships'), (N'GolfClubs'), (N'GolfCourses'),
    (N'GolfSocieties'), (N'Holes'), (N'HoleTees'), (N'Notifications'),
    (N'PlayerConnections'), (N'PlayerMergeRequests'), (N'Players'),
    (N'RoundPlayers'), (N'Rounds'), (N'Scores'), (N'SocietyMemberships'),
    (N'TeeSets');

IF EXISTS (
    SELECT 1
    FROM @ExpectedTables expected
    WHERE OBJECT_ID(N'dbo.' + QUOTENAME(expected.TableName), N'U') IS NULL
)
    THROW 51004, 'One or more expected dbo tables are missing. Rerun the drift check.', 1;

DECLARE @HistoryStatus int = 0;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    IF (SELECT COUNT(*) FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.__EFMigrationsHistory')) <> 2
        OR NOT EXISTS (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.__EFMigrationsHistory')
              AND name = N'MigrationId' AND TYPE_NAME(user_type_id) = N'nvarchar'
              AND max_length = 300 AND is_nullable = 0
        )
        OR NOT EXISTS (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.__EFMigrationsHistory')
              AND name = N'ProductVersion' AND TYPE_NAME(user_type_id) = N'nvarchar'
              AND max_length = 64 AND is_nullable = 0
        )
        OR NOT EXISTS (
            SELECT 1
            FROM sys.key_constraints keyObject
            JOIN sys.index_columns indexColumn
                ON indexColumn.object_id = keyObject.parent_object_id
               AND indexColumn.index_id = keyObject.unique_index_id
            JOIN sys.columns columnObject
                ON columnObject.object_id = indexColumn.object_id
               AND columnObject.column_id = indexColumn.column_id
            WHERE keyObject.parent_object_id = OBJECT_ID(N'dbo.__EFMigrationsHistory')
              AND keyObject.type = N'PK'
              AND columnObject.name = N'MigrationId'
              AND indexColumn.key_ordinal = 1
        )
        OR (SELECT COUNT(*)
            FROM sys.key_constraints keyObject
            JOIN sys.index_columns indexColumn
                ON indexColumn.object_id = keyObject.parent_object_id
               AND indexColumn.index_id = keyObject.unique_index_id
            WHERE keyObject.parent_object_id = OBJECT_ID(N'dbo.__EFMigrationsHistory')
              AND keyObject.type = N'PK') <> 1
        SET @HistoryStatus = 5;
    ELSE
        EXEC sys.sp_executesql N'
            SELECT @Status =
                CASE
                    WHEN EXISTS (
                        SELECT 1 FROM dbo.__EFMigrationsHistory
                        WHERE MigrationId <> @MigrationId
                    ) THEN 4
                    WHEN EXISTS (
                        SELECT 1 FROM dbo.__EFMigrationsHistory
                        WHERE MigrationId = @MigrationId
                          AND ProductVersion <> @ProductVersion
                    ) THEN 3
                    WHEN EXISTS (
                        SELECT 1 FROM dbo.__EFMigrationsHistory
                        WHERE MigrationId = @MigrationId
                          AND ProductVersion = @ProductVersion
                    ) THEN 2
                    ELSE 1
                END;',
            N'@MigrationId nvarchar(150), @ProductVersion nvarchar(32), @Status int OUTPUT',
            @MigrationId, @ProductVersion, @HistoryStatus OUTPUT;
END;

IF @HistoryStatus = 5
    THROW 51005, 'Existing __EFMigrationsHistory table has an unexpected shape.', 1;

IF @HistoryStatus = 3
    THROW 51006, 'The baseline migration row exists with a different product version.', 1;

IF @HistoryStatus = 4
    THROW 51007, 'Unexpected migration history rows exist. Stop and investigate.', 1;

IF @HistoryStatus = 2
BEGIN
    SELECT N'ALREADY BASELINED - no changes made' AS Result, DB_NAME() AS DatabaseName,
        @MigrationId AS MigrationId, @ProductVersion AS ProductVersion;
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
        EXEC(N'
            CREATE TABLE dbo.__EFMigrationsHistory (
                MigrationId nvarchar(150) NOT NULL,
                ProductVersion nvarchar(32) NOT NULL,
                CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
            );');

    EXEC sys.sp_executesql N'
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES (@MigrationId, @ProductVersion);',
        N'@MigrationId nvarchar(150), @ProductVersion nvarchar(32)',
        @MigrationId, @ProductVersion;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'BASELINE RECORDED' AS Result, DB_NAME() AS DatabaseName,
    @MigrationId AS MigrationId, @ProductVersion AS ProductVersion;

/*
Human-run production reconciliation for:
  20260611161345_InitialSqlServer (EF Core 10.0.3)

This script is tailored to the reviewed 64-row production drift report from
2026-06-11. It changes schema only; it does not update or delete application
rows.

Run docs/sql-server-drift-check.sql immediately before and after this script.
Do not run this script unless the pre-run drift report matches the reviewed
report and both orphan checks return zero.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Required operator confirmations. Change all four values before execution.
DECLARE @ConfirmedDatabaseName sysname = N'REPLACE_WITH_PRODUCTION_DATABASE_NAME';
DECLARE @ConfirmedBackupTaken bit = 0;
DECLARE @ConfirmedReviewedDriftErrorCount int = -1;
DECLARE @ConfirmedOrphanChecksReturnedZero bit = 0;

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 51000, 'Refusing to run against a SQL Server system database.', 1;

IF DB_NAME() <> @ConfirmedDatabaseName
    THROW 51001, 'DB_NAME() does not match @ConfirmedDatabaseName.', 1;

IF @ConfirmedBackupTaken <> 1
    THROW 51002, 'A verified backup must be confirmed before reconciliation.', 1;

IF @ConfirmedReviewedDriftErrorCount <> 64
    THROW 51003, 'The reviewed 64-row drift report has not been confirmed.', 1;

IF @ConfirmedOrphanChecksReturnedZero <> 1
    THROW 51004, 'Both required orphan checks must return zero.', 1;

DECLARE @HistoryRows int = 0;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql
        N'SELECT @Count = COUNT(*) FROM dbo.__EFMigrationsHistory;',
        N'@Count int OUTPUT',
        @HistoryRows OUTPUT;
END;

IF @HistoryRows <> 0
    THROW 51005, 'Migration history is not empty. Stop and investigate.', 1;

-- Confirm the two columns still have the reviewed pre-reconciliation shapes.
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns columnObject
    JOIN sys.types typeObject ON typeObject.user_type_id = columnObject.user_type_id
    WHERE columnObject.object_id = OBJECT_ID(N'dbo.TeeSets')
      AND columnObject.name = N'Colour'
      AND typeObject.name = N'nvarchar'
      AND columnObject.max_length = 14
      AND columnObject.is_nullable = 0
)
    THROW 51006, 'dbo.TeeSets.Colour no longer matches reviewed nvarchar(7) shape.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns columnObject
    JOIN sys.types typeObject ON typeObject.user_type_id = columnObject.user_type_id
    WHERE columnObject.object_id = OBJECT_ID(N'dbo.TeeSets')
      AND columnObject.name = N'CourseRating'
      AND typeObject.name = N'decimal'
      AND columnObject.precision = 4
      AND columnObject.scale = 1
      AND columnObject.is_nullable = 1
)
    THROW 51007, 'dbo.TeeSets.CourseRating no longer matches reviewed decimal(4,1) shape.', 1;

-- Recheck the two relationships that were confirmed orphan-free by the human.
IF EXISTS (
    SELECT 1
    FROM dbo.AspNetUsers applicationUser
    LEFT JOIN dbo.Players player ON player.PlayerId = applicationUser.LinkedPlayerId
    WHERE applicationUser.LinkedPlayerId IS NOT NULL
      AND player.PlayerId IS NULL
)
    THROW 51008, 'Orphans now exist for dbo.AspNetUsers.LinkedPlayerId.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.Scores score
    LEFT JOIN dbo.TeeSets teeSet ON teeSet.TeeSetId = score.TeeSetId
    WHERE score.TeeSetId IS NOT NULL
      AND teeSet.TeeSetId IS NULL
)
    THROW 51009, 'Orphans now exist for dbo.Scores.TeeSetId.', 1;

DECLARE @Defaults TABLE (
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    PRIMARY KEY (TableName, ColumnName)
);

INSERT INTO @Defaults (TableName, ColumnName) VALUES
    (N'AiAuditLogs', N'CompletionTokens'),
    (N'AiAuditLogs', N'PromptTokens'),
    (N'AiAuditLogs', N'RequestedAt'),
    (N'AiAuditLogs', N'ResponseTimeMs'),
    (N'AiAuditLogs', N'Success'),
    (N'AiAuditLogs', N'TotalTokens'),
    (N'AiChatSessionMessages', N'Timestamp'),
    (N'AiChatSessions', N'CreatedAt'),
    (N'AiChatSessions', N'IsArchived'),
    (N'AiChatSessions', N'LastMessageAt'),
    (N'AiProviderSettings', N'Enabled'),
    (N'AiProviderSettings', N'Priority'),
    (N'AiProviderSettings', N'UpdatedAt'),
    (N'ApplicationSettings', N'Category'),
    (N'ApplicationSettings', N'UpdatedAt'),
    (N'ApplicationSettings', N'ValueType'),
    (N'AspNetUsers', N'AiInsightsOptOut'),
    (N'ClubMemberships', N'JoinedAt'),
    (N'ClubMemberships', N'Role'),
    (N'GolfSocieties', N'CreatedAt'),
    (N'GolfSocieties', N'IsActive'),
    (N'SocietyMemberships', N'JoinedAt'),
    (N'SocietyMemberships', N'Role'),
    (N'TeeSets', N'Colour'),
    (N'TeeSets', N'Gender'),
    (N'TeeSets', N'SortOrder');

IF (
    SELECT COUNT(*)
    FROM sys.default_constraints defaultObject
    JOIN sys.tables tableObject ON tableObject.object_id = defaultObject.parent_object_id
    JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
    JOIN sys.columns columnObject
        ON columnObject.object_id = tableObject.object_id
       AND columnObject.column_id = defaultObject.parent_column_id
    JOIN @Defaults expected
        ON expected.TableName = tableObject.name
       AND expected.ColumnName = columnObject.name
    WHERE schemaObject.name = N'dbo'
) <> 26
    THROW 51010, 'The reviewed set of 26 legacy defaults has changed.', 1;

DECLARE @ForeignKeyRenames TABLE (
    TableName sysname NOT NULL,
    OldName sysname NOT NULL PRIMARY KEY,
    NewName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    ReferencedTable sysname NOT NULL,
    ReferencedColumn sysname NOT NULL,
    DeleteAction nvarchar(60) NOT NULL
);

INSERT INTO @ForeignKeyRenames
    (TableName, OldName, NewName, ColumnName, ReferencedTable, ReferencedColumn, DeleteAction)
VALUES
    (N'AiAuditLogs', N'FK_AiAuditLogs_AiChatSession', N'FK_AiAuditLogs_AiChatSessions_AiChatSessionId', N'AiChatSessionId', N'AiChatSessions', N'AiChatSessionId', N'NO ACTION'),
    (N'AiAuditLogs', N'FK_AiAuditLogs_AspNetUsers', N'FK_AiAuditLogs_AspNetUsers_ApplicationUserId', N'ApplicationUserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'AiChatSessionMessages', N'FK_AiChatSessionMessages_Session', N'FK_AiChatSessionMessages_AiChatSessions_AiChatSessionId', N'AiChatSessionId', N'AiChatSessions', N'AiChatSessionId', N'CASCADE'),
    (N'AiChatSessions', N'FK_AiChatSessions_AspNetUsers', N'FK_AiChatSessions_AspNetUsers_ApplicationUserId', N'ApplicationUserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'ClubMemberships', N'FK_ClubMemberships_AspNetUsers', N'FK_ClubMemberships_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'ClubMemberships', N'FK_ClubMemberships_GolfClubs', N'FK_ClubMemberships_GolfClubs_GolfClubId', N'GolfClubId', N'GolfClubs', N'GolfClubId', N'CASCADE'),
    (N'GolfSocieties', N'FK_GolfSocieties_AspNetUsers', N'FK_GolfSocieties_AspNetUsers_CreatedByUserId', N'CreatedByUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'HoleTees', N'FK_HoleTees_Holes', N'FK_HoleTees_Holes_HoleId', N'HoleId', N'Holes', N'HoleId', N'CASCADE'),
    (N'HoleTees', N'FK_HoleTees_TeeSets', N'FK_HoleTees_TeeSets_TeeSetId', N'TeeSetId', N'TeeSets', N'TeeSetId', N'NO ACTION'),
    (N'RoundPlayers', N'FK_RoundPlayers_TeeSets', N'FK_RoundPlayers_TeeSets_TeeSetId', N'TeeSetId', N'TeeSets', N'TeeSetId', N'NO ACTION'),
    (N'SocietyMemberships', N'FK_SocietyMemberships_AspNetUsers', N'FK_SocietyMemberships_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'SocietyMemberships', N'FK_SocietyMemberships_GolfSocieties', N'FK_SocietyMemberships_GolfSocieties_GolfSocietyId', N'GolfSocietyId', N'GolfSocieties', N'GolfSocietyId', N'CASCADE'),
    (N'TeeSets', N'FK_TeeSets_GolfCourses', N'FK_TeeSets_GolfCourses_GolfCourseId', N'GolfCourseId', N'GolfCourses', N'GolfCourseId', N'CASCADE');

IF EXISTS (
    SELECT 1
    FROM @ForeignKeyRenames rename
    LEFT JOIN sys.foreign_keys foreignKey ON foreignKey.name = rename.OldName
    LEFT JOIN sys.tables parentTable ON parentTable.object_id = foreignKey.parent_object_id
    LEFT JOIN sys.schemas parentSchema ON parentSchema.schema_id = parentTable.schema_id
    LEFT JOIN sys.tables referencedTable ON referencedTable.object_id = foreignKey.referenced_object_id
    LEFT JOIN sys.foreign_key_columns foreignKeyColumn
        ON foreignKeyColumn.constraint_object_id = foreignKey.object_id
       AND foreignKeyColumn.constraint_column_id = 1
    LEFT JOIN sys.columns parentColumn
        ON parentColumn.object_id = parentTable.object_id
       AND parentColumn.column_id = foreignKeyColumn.parent_column_id
    LEFT JOIN sys.columns referencedColumn
        ON referencedColumn.object_id = referencedTable.object_id
       AND referencedColumn.column_id = foreignKeyColumn.referenced_column_id
    WHERE foreignKey.object_id IS NULL
       OR parentSchema.name <> N'dbo'
       OR parentTable.name <> rename.TableName
       OR parentColumn.name <> rename.ColumnName
       OR referencedTable.name <> rename.ReferencedTable
       OR referencedColumn.name <> rename.ReferencedColumn
       OR REPLACE(foreignKey.delete_referential_action_desc, N'_', N' ') <> rename.DeleteAction
       OR OBJECT_ID(N'dbo.' + rename.NewName, N'F') IS NOT NULL
)
    THROW 51011, 'The reviewed foreign-key names, shapes, or delete actions have changed.', 1;

DECLARE @IndexRenames TABLE (
    TableName sysname NOT NULL,
    OldName sysname NOT NULL,
    NewName sysname NOT NULL,
    IsUnique bit NOT NULL,
    Columns nvarchar(400) NOT NULL,
    PRIMARY KEY (TableName, OldName)
);

INSERT INTO @IndexRenames (TableName, OldName, NewName, IsUnique, Columns) VALUES
    (N'AiAuditLogs', N'IX_AiAuditLogs_UserId_RequestedAt', N'IX_AiAuditLogs_ApplicationUserId_RequestedAt', 0, N'ApplicationUserId,RequestedAt'),
    (N'AiChatSessionMessages', N'IX_AiChatSessionMessages_SessionId_Timestamp', N'IX_AiChatSessionMessages_AiChatSessionId_Timestamp', 0, N'AiChatSessionId,Timestamp'),
    (N'AiChatSessions', N'IX_AiChatSessions_UserId_LastMessage', N'IX_AiChatSessions_ApplicationUserId_LastMessageAt', 0, N'ApplicationUserId,LastMessageAt');

IF EXISTS (
    SELECT 1
    FROM @IndexRenames rename
    OUTER APPLY (
        SELECT
            indexObject.is_unique AS IsUnique,
            STRING_AGG(CONVERT(nvarchar(max), columnObject.name), N',')
                WITHIN GROUP (ORDER BY indexColumn.key_ordinal) AS Columns
        FROM sys.indexes indexObject
        JOIN sys.tables tableObject ON tableObject.object_id = indexObject.object_id
        JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
        JOIN sys.index_columns indexColumn
            ON indexColumn.object_id = indexObject.object_id
           AND indexColumn.index_id = indexObject.index_id
        JOIN sys.columns columnObject
            ON columnObject.object_id = indexColumn.object_id
           AND columnObject.column_id = indexColumn.column_id
        WHERE schemaObject.name = N'dbo'
          AND tableObject.name = rename.TableName
          AND indexObject.name = rename.OldName
          AND indexColumn.is_included_column = 0
          AND indexColumn.key_ordinal > 0
        GROUP BY indexObject.is_unique
    ) actual
    WHERE actual.Columns IS NULL
       OR actual.IsUnique <> rename.IsUnique
       OR actual.Columns <> rename.Columns
       OR INDEXPROPERTY(OBJECT_ID(N'dbo.' + rename.TableName), rename.NewName, N'IndexID') IS NOT NULL
)
    THROW 51012, 'The reviewed index names, shapes, or uniqueness have changed.', 1;

IF OBJECT_ID(N'dbo.FK_AspNetUsers_Players_LinkedPlayerId', N'F') IS NOT NULL
   OR OBJECT_ID(N'dbo.FK_Scores_TeeSets_TeeSetId', N'F') IS NOT NULL
   OR INDEXPROPERTY(OBJECT_ID(N'dbo.AiAuditLogs'), N'IX_AiAuditLogs_AiChatSessionId', N'IndexID') IS NOT NULL
   OR INDEXPROPERTY(OBJECT_ID(N'dbo.GolfSocieties'), N'IX_GolfSocieties_CreatedByUserId', N'IndexID') IS NOT NULL
    THROW 51013, 'One or more reviewed missing objects now exist. Rerun the drift check.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @DropDefaultsSql nvarchar(max) = N'';
    SELECT @DropDefaultsSql = @DropDefaultsSql +
        N'ALTER TABLE dbo.' + QUOTENAME(tableObject.name) +
        N' DROP CONSTRAINT ' + QUOTENAME(defaultObject.name) + N';' + CHAR(10)
    FROM sys.default_constraints defaultObject
    JOIN sys.tables tableObject ON tableObject.object_id = defaultObject.parent_object_id
    JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
    JOIN sys.columns columnObject
        ON columnObject.object_id = tableObject.object_id
       AND columnObject.column_id = defaultObject.parent_column_id
    JOIN @Defaults expected
        ON expected.TableName = tableObject.name
       AND expected.ColumnName = columnObject.name
    WHERE schemaObject.name = N'dbo';

    EXEC sys.sp_executesql @DropDefaultsSql;

    ALTER TABLE dbo.TeeSets ALTER COLUMN Colour nvarchar(20) NOT NULL;
    ALTER TABLE dbo.TeeSets ALTER COLUMN CourseRating decimal(18,2) NULL;

    DECLARE @OldName sysname;
    DECLARE @NewName sysname;
    DECLARE @QualifiedName nvarchar(776);

    DECLARE foreignKeyCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT OldName, NewName FROM @ForeignKeyRenames ORDER BY OldName;

    OPEN foreignKeyCursor;
    FETCH NEXT FROM foreignKeyCursor INTO @OldName, @NewName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @QualifiedName = N'dbo.' + @OldName;
        EXEC sys.sp_rename
            @objname = @QualifiedName,
            @newname = @NewName,
            @objtype = N'OBJECT';
        FETCH NEXT FROM foreignKeyCursor INTO @OldName, @NewName;
    END;
    CLOSE foreignKeyCursor;
    DEALLOCATE foreignKeyCursor;

    DECLARE @TableName sysname;
    DECLARE indexCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, OldName, NewName FROM @IndexRenames ORDER BY TableName, OldName;

    OPEN indexCursor;
    FETCH NEXT FROM indexCursor INTO @TableName, @OldName, @NewName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @QualifiedName = N'dbo.' + @TableName + N'.' + @OldName;
        EXEC sys.sp_rename
            @objname = @QualifiedName,
            @newname = @NewName,
            @objtype = N'INDEX';
        FETCH NEXT FROM indexCursor INTO @TableName, @OldName, @NewName;
    END;
    CLOSE indexCursor;
    DEALLOCATE indexCursor;

    CREATE INDEX IX_AiAuditLogs_AiChatSessionId
        ON dbo.AiAuditLogs (AiChatSessionId);
    CREATE INDEX IX_GolfSocieties_CreatedByUserId
        ON dbo.GolfSocieties (CreatedByUserId);

    ALTER TABLE dbo.AspNetUsers WITH CHECK ADD CONSTRAINT FK_AspNetUsers_Players_LinkedPlayerId
        FOREIGN KEY (LinkedPlayerId) REFERENCES dbo.Players (PlayerId) ON DELETE NO ACTION;
    ALTER TABLE dbo.AspNetUsers CHECK CONSTRAINT FK_AspNetUsers_Players_LinkedPlayerId;

    ALTER TABLE dbo.Scores WITH CHECK ADD CONSTRAINT FK_Scores_TeeSets_TeeSetId
        FOREIGN KEY (TeeSetId) REFERENCES dbo.TeeSets (TeeSetId) ON DELETE NO ACTION;
    ALTER TABLE dbo.Scores CHECK CONSTRAINT FK_Scores_TeeSets_TeeSetId;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N'local', N'foreignKeyCursor') >= 0
        CLOSE foreignKeyCursor;
    IF CURSOR_STATUS(N'local', N'foreignKeyCursor') > -3
        DEALLOCATE foreignKeyCursor;
    IF CURSOR_STATUS(N'local', N'indexCursor') >= 0
        CLOSE indexCursor;
    IF CURSOR_STATUS(N'local', N'indexCursor') > -3
        DEALLOCATE indexCursor;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'RECONCILIATION COMPLETE - RERUN DRIFT CHECK BEFORE BASELINING' AS Result,
    DB_NAME() AS DatabaseName;

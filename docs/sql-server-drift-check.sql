/*
Human-run, read-only production drift check for:
  20260611161345_InitialSqlServer (EF Core 10.0.3)

The only objects created by this script are temporary tables in tempdb.
Any ERROR means the database must not be marked as baselined.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 51000, 'Refusing to inspect a SQL Server system database.', 1;

CREATE TABLE #ExpectedTables (
    TableName sysname NOT NULL PRIMARY KEY
);

CREATE TABLE #ExpectedColumns (
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    TypeName nvarchar(128) NOT NULL,
    IsNullable bit NOT NULL,
    IsIdentity bit NOT NULL,
    PRIMARY KEY (TableName, ColumnName)
);

CREATE TABLE #ExpectedPrimaryKeys (
    TableName sysname NOT NULL,
    ConstraintName sysname NOT NULL,
    Columns nvarchar(max) NOT NULL,
    PRIMARY KEY (TableName, ConstraintName)
);

CREATE TABLE #ExpectedIndexes (
    TableName sysname NOT NULL,
    IndexName sysname NOT NULL,
    IsUnique bit NOT NULL,
    Columns nvarchar(max) NOT NULL,
    PRIMARY KEY (TableName, IndexName)
);

CREATE TABLE #ExpectedForeignKeys (
    TableName sysname NOT NULL,
    ConstraintName sysname NOT NULL,
    Columns nvarchar(max) NOT NULL,
    ReferencedTable sysname NOT NULL,
    ReferencedColumns nvarchar(max) NOT NULL,
    DeleteAction nvarchar(60) NOT NULL,
    PRIMARY KEY (TableName, ConstraintName)
);

CREATE TABLE #Drift (
    Category nvarchar(40) NOT NULL,
    ObjectName nvarchar(512) NOT NULL,
    Expected nvarchar(max) NULL,
    Actual nvarchar(max) NULL
);

INSERT INTO #ExpectedTables (TableName) VALUES
    (N'AiProviderSettings'), (N'ApplicationSettings'), (N'AspNetRoles'),
    (N'GolfClubs'), (N'AspNetRoleClaims'), (N'GolfCourses'), (N'Holes'),
    (N'TeeSets'), (N'HoleTees'), (N'AiAuditLogs'),
    (N'AiChatSessionMessages'), (N'AiChatSessions'), (N'AspNetUserClaims'),
    (N'AspNetUserLogins'), (N'AspNetUserRoles'), (N'AspNetUsers'),
    (N'AspNetUserTokens'), (N'ClubMemberships'), (N'GolfSocieties'),
    (N'Notifications'), (N'PlayerConnections'), (N'Players'), (N'Rounds'),
    (N'SocietyMemberships'), (N'PlayerMergeRequests'), (N'RoundPlayers'),
    (N'Scores');

INSERT INTO #ExpectedColumns
    (TableName, ColumnName, TypeName, IsNullable, IsIdentity)
VALUES
    (N'AiProviderSettings', N'AiProviderSettingsId', N'int', 0, 1),
    (N'AiProviderSettings', N'ProviderName', N'nvarchar(50)', 0, 0),
    (N'AiProviderSettings', N'Enabled', N'bit', 0, 0),
    (N'AiProviderSettings', N'Priority', N'int', 0, 0),
    (N'AiProviderSettings', N'UpdatedAt', N'datetime2', 0, 0),
    (N'AiProviderSettings', N'UpdatedByUserId', N'nvarchar(450)', 1, 0),
    (N'ApplicationSettings', N'Id', N'int', 0, 1),
    (N'ApplicationSettings', N'Key', N'nvarchar(100)', 0, 0),
    (N'ApplicationSettings', N'Value', N'nvarchar(500)', 0, 0),
    (N'ApplicationSettings', N'Description', N'nvarchar(200)', 1, 0),
    (N'ApplicationSettings', N'Category', N'nvarchar(50)', 0, 0),
    (N'ApplicationSettings', N'ValueType', N'nvarchar(20)', 0, 0),
    (N'ApplicationSettings', N'UpdatedAt', N'datetime2', 0, 0),
    (N'ApplicationSettings', N'UpdatedByUserId', N'nvarchar(450)', 1, 0),
    (N'AspNetRoles', N'Id', N'nvarchar(450)', 0, 0),
    (N'AspNetRoles', N'Name', N'nvarchar(256)', 1, 0),
    (N'AspNetRoles', N'NormalizedName', N'nvarchar(256)', 1, 0),
    (N'AspNetRoles', N'ConcurrencyStamp', N'nvarchar(max)', 1, 0),
    (N'GolfClubs', N'GolfClubId', N'int', 0, 1),
    (N'GolfClubs', N'Name', N'nvarchar(100)', 0, 0),
    (N'GolfClubs', N'AddressLine1', N'nvarchar(200)', 1, 0),
    (N'GolfClubs', N'AddressLine2', N'nvarchar(100)', 1, 0),
    (N'GolfClubs', N'City', N'nvarchar(100)', 1, 0),
    (N'GolfClubs', N'CountyOrRegion', N'nvarchar(50)', 1, 0),
    (N'GolfClubs', N'Postcode', N'nvarchar(20)', 1, 0),
    (N'GolfClubs', N'Country', N'nvarchar(50)', 1, 0),
    (N'GolfClubs', N'Website', N'nvarchar(200)', 1, 0),
    (N'AspNetRoleClaims', N'Id', N'int', 0, 1),
    (N'AspNetRoleClaims', N'RoleId', N'nvarchar(450)', 0, 0),
    (N'AspNetRoleClaims', N'ClaimType', N'nvarchar(max)', 1, 0),
    (N'AspNetRoleClaims', N'ClaimValue', N'nvarchar(max)', 1, 0),
    (N'GolfCourses', N'GolfCourseId', N'int', 0, 1),
    (N'GolfCourses', N'GolfClubId', N'int', 0, 0),
    (N'GolfCourses', N'Name', N'nvarchar(100)', 0, 0),
    (N'GolfCourses', N'DefaultPar', N'int', 0, 0),
    (N'GolfCourses', N'NumberOfHoles', N'int', 0, 0),
    (N'Holes', N'HoleId', N'int', 0, 1),
    (N'Holes', N'GolfCourseId', N'int', 0, 0),
    (N'Holes', N'HoleNumber', N'int', 0, 0),
    (N'Holes', N'Par', N'int', 0, 0),
    (N'Holes', N'StrokeIndex', N'int', 1, 0),
    (N'Holes', N'LengthYards', N'int', 1, 0),
    (N'TeeSets', N'TeeSetId', N'int', 0, 1),
    (N'TeeSets', N'GolfCourseId', N'int', 0, 0),
    (N'TeeSets', N'Name', N'nvarchar(50)', 0, 0),
    (N'TeeSets', N'Colour', N'nvarchar(20)', 0, 0),
    (N'TeeSets', N'CourseRating', N'decimal(18,2)', 1, 0),
    (N'TeeSets', N'SlopeRating', N'int', 1, 0),
    (N'TeeSets', N'Gender', N'int', 0, 0),
    (N'TeeSets', N'SortOrder', N'int', 0, 0),
    (N'HoleTees', N'HoleTeeId', N'int', 0, 1),
    (N'HoleTees', N'HoleId', N'int', 0, 0),
    (N'HoleTees', N'TeeSetId', N'int', 0, 0),
    (N'HoleTees', N'Par', N'int', 0, 0),
    (N'HoleTees', N'StrokeIndex', N'int', 1, 0),
    (N'HoleTees', N'LengthYards', N'int', 1, 0),
    (N'AiAuditLogs', N'AiAuditLogId', N'int', 0, 1),
    (N'AiAuditLogs', N'ApplicationUserId', N'nvarchar(450)', 0, 0),
    (N'AiAuditLogs', N'RequestedAt', N'datetime2', 0, 0),
    (N'AiAuditLogs', N'ResponseTimeMs', N'int', 0, 0),
    (N'AiAuditLogs', N'InsightType', N'nvarchar(50)', 0, 0),
    (N'AiAuditLogs', N'ProviderName', N'nvarchar(50)', 1, 0),
    (N'AiAuditLogs', N'ModelUsed', N'nvarchar(50)', 1, 0),
    (N'AiAuditLogs', N'PromptTokens', N'int', 0, 0),
    (N'AiAuditLogs', N'CompletionTokens', N'int', 0, 0),
    (N'AiAuditLogs', N'TotalTokens', N'int', 0, 0),
    (N'AiAuditLogs', N'Success', N'bit', 0, 0),
    (N'AiAuditLogs', N'ErrorMessage', N'nvarchar(500)', 1, 0),
    (N'AiAuditLogs', N'PromptSent', N'nvarchar(max)', 1, 0),
    (N'AiAuditLogs', N'ResponseReceived', N'nvarchar(max)', 1, 0),
    (N'AiAuditLogs', N'AiChatSessionId', N'int', 1, 0),
    (N'AiChatSessionMessages', N'AiChatSessionMessageId', N'int', 0, 1),
    (N'AiChatSessionMessages', N'AiChatSessionId', N'int', 0, 0),
    (N'AiChatSessionMessages', N'Role', N'nvarchar(20)', 0, 0),
    (N'AiChatSessionMessages', N'Content', N'nvarchar(max)', 0, 0),
    (N'AiChatSessionMessages', N'Timestamp', N'datetime2', 0, 0),
    (N'AiChatSessions', N'AiChatSessionId', N'int', 0, 1),
    (N'AiChatSessions', N'ApplicationUserId', N'nvarchar(450)', 0, 0),
    (N'AiChatSessions', N'Title', N'nvarchar(100)', 1, 0),
    (N'AiChatSessions', N'CreatedAt', N'datetime2', 0, 0),
    (N'AiChatSessions', N'LastMessageAt', N'datetime2', 0, 0),
    (N'AiChatSessions', N'IsArchived', N'bit', 0, 0),
    (N'AspNetUserClaims', N'Id', N'int', 0, 1),
    (N'AspNetUserClaims', N'UserId', N'nvarchar(450)', 0, 0),
    (N'AspNetUserClaims', N'ClaimType', N'nvarchar(max)', 1, 0),
    (N'AspNetUserClaims', N'ClaimValue', N'nvarchar(max)', 1, 0),
    (N'AspNetUserLogins', N'LoginProvider', N'nvarchar(450)', 0, 0),
    (N'AspNetUserLogins', N'ProviderKey', N'nvarchar(450)', 0, 0),
    (N'AspNetUserLogins', N'ProviderDisplayName', N'nvarchar(max)', 1, 0),
    (N'AspNetUserLogins', N'UserId', N'nvarchar(450)', 0, 0),
    (N'AspNetUserRoles', N'UserId', N'nvarchar(450)', 0, 0),
    (N'AspNetUserRoles', N'RoleId', N'nvarchar(450)', 0, 0),
    (N'AspNetUsers', N'Id', N'nvarchar(450)', 0, 0),
    (N'AspNetUsers', N'LinkedPlayerId', N'int', 1, 0),
    (N'AspNetUsers', N'AiInsightsOptOut', N'bit', 0, 0),
    (N'AspNetUsers', N'UserName', N'nvarchar(256)', 1, 0),
    (N'AspNetUsers', N'NormalizedUserName', N'nvarchar(256)', 1, 0),
    (N'AspNetUsers', N'Email', N'nvarchar(256)', 1, 0),
    (N'AspNetUsers', N'NormalizedEmail', N'nvarchar(256)', 1, 0),
    (N'AspNetUsers', N'EmailConfirmed', N'bit', 0, 0),
    (N'AspNetUsers', N'PasswordHash', N'nvarchar(max)', 1, 0),
    (N'AspNetUsers', N'SecurityStamp', N'nvarchar(max)', 1, 0),
    (N'AspNetUsers', N'ConcurrencyStamp', N'nvarchar(max)', 1, 0),
    (N'AspNetUsers', N'PhoneNumber', N'nvarchar(max)', 1, 0),
    (N'AspNetUsers', N'PhoneNumberConfirmed', N'bit', 0, 0),
    (N'AspNetUsers', N'TwoFactorEnabled', N'bit', 0, 0),
    (N'AspNetUsers', N'LockoutEnd', N'datetimeoffset', 1, 0),
    (N'AspNetUsers', N'LockoutEnabled', N'bit', 0, 0),
    (N'AspNetUsers', N'AccessFailedCount', N'int', 0, 0),
    (N'AspNetUserTokens', N'UserId', N'nvarchar(450)', 0, 0),
    (N'AspNetUserTokens', N'LoginProvider', N'nvarchar(450)', 0, 0),
    (N'AspNetUserTokens', N'Name', N'nvarchar(450)', 0, 0),
    (N'AspNetUserTokens', N'Value', N'nvarchar(max)', 1, 0),
    (N'ClubMemberships', N'ClubMembershipId', N'int', 0, 1),
    (N'ClubMemberships', N'GolfClubId', N'int', 0, 0),
    (N'ClubMemberships', N'UserId', N'nvarchar(450)', 0, 0),
    (N'ClubMemberships', N'Role', N'int', 0, 0),
    (N'ClubMemberships', N'MembershipNumber', N'nvarchar(50)', 1, 0),
    (N'ClubMemberships', N'JoinedAt', N'datetime2', 0, 0),
    (N'GolfSocieties', N'GolfSocietyId', N'int', 0, 1),
    (N'GolfSocieties', N'Name', N'nvarchar(100)', 0, 0),
    (N'GolfSocieties', N'Description', N'nvarchar(500)', 1, 0),
    (N'GolfSocieties', N'CreatedByUserId', N'nvarchar(450)', 0, 0),
    (N'GolfSocieties', N'CreatedAt', N'datetime2', 0, 0),
    (N'GolfSocieties', N'IsActive', N'bit', 0, 0),
    (N'Notifications', N'Id', N'int', 0, 1),
    (N'Notifications', N'UserId', N'nvarchar(450)', 0, 0),
    (N'Notifications', N'Type', N'int', 0, 0),
    (N'Notifications', N'Title', N'nvarchar(100)', 0, 0),
    (N'Notifications', N'Message', N'nvarchar(500)', 0, 0),
    (N'Notifications', N'ActionUrl', N'nvarchar(200)', 1, 0),
    (N'Notifications', N'RelatedEntityId', N'int', 1, 0),
    (N'Notifications', N'IsRead', N'bit', 0, 0),
    (N'Notifications', N'CreatedAt', N'datetime2', 0, 0),
    (N'PlayerConnections', N'Id', N'int', 0, 1),
    (N'PlayerConnections', N'RequestingUserId', N'nvarchar(450)', 0, 0),
    (N'PlayerConnections', N'TargetUserId', N'nvarchar(450)', 0, 0),
    (N'PlayerConnections', N'Status', N'int', 0, 0),
    (N'PlayerConnections', N'RequestedAt', N'datetime2', 0, 0),
    (N'PlayerConnections', N'RespondedAt', N'datetime2', 1, 0),
    (N'Players', N'PlayerId', N'int', 0, 1),
    (N'Players', N'FirstName', N'nvarchar(50)', 0, 0),
    (N'Players', N'LastName', N'nvarchar(50)', 0, 0),
    (N'Players', N'Handicap', N'float', 1, 0),
    (N'Players', N'ApplicationUserId', N'nvarchar(450)', 1, 0),
    (N'Players', N'CreatedByApplicationUserId', N'nvarchar(450)', 0, 0),
    (N'Rounds', N'RoundId', N'int', 0, 1),
    (N'Rounds', N'GolfCourseId', N'int', 0, 0),
    (N'Rounds', N'DatePlayed', N'datetime2', 0, 0),
    (N'Rounds', N'StartingHole', N'int', 0, 0),
    (N'Rounds', N'HolesPlayed', N'int', 0, 0),
    (N'Rounds', N'RoundType', N'int', 0, 0),
    (N'Rounds', N'Notes', N'nvarchar(max)', 1, 0),
    (N'Rounds', N'CreatedByApplicationUserId', N'nvarchar(450)', 0, 0),
    (N'Rounds', N'Status', N'int', 0, 0),
    (N'SocietyMemberships', N'SocietyMembershipId', N'int', 0, 1),
    (N'SocietyMemberships', N'GolfSocietyId', N'int', 0, 0),
    (N'SocietyMemberships', N'UserId', N'nvarchar(450)', 0, 0),
    (N'SocietyMemberships', N'Role', N'int', 0, 0),
    (N'SocietyMemberships', N'JoinedAt', N'datetime2', 0, 0),
    (N'PlayerMergeRequests', N'Id', N'int', 0, 1),
    (N'PlayerMergeRequests', N'RequestingUserId', N'nvarchar(450)', 0, 0),
    (N'PlayerMergeRequests', N'TargetUserId', N'nvarchar(450)', 0, 0),
    (N'PlayerMergeRequests', N'SourcePlayerId', N'int', 0, 0),
    (N'PlayerMergeRequests', N'TargetPlayerId', N'int', 0, 0),
    (N'PlayerMergeRequests', N'Status', N'int', 0, 0),
    (N'PlayerMergeRequests', N'RequestedAt', N'datetime2', 0, 0),
    (N'PlayerMergeRequests', N'CompletedAt', N'datetime2', 1, 0),
    (N'PlayerMergeRequests', N'Message', N'nvarchar(500)', 1, 0),
    (N'PlayerMergeRequests', N'RoundsMerged', N'int', 0, 0),
    (N'PlayerMergeRequests', N'RoundsSkipped', N'int', 0, 0),
    (N'RoundPlayers', N'RoundId', N'int', 0, 0),
    (N'RoundPlayers', N'PlayerId', N'int', 0, 0),
    (N'RoundPlayers', N'TeeSetId', N'int', 1, 0),
    (N'Scores', N'ScoreId', N'int', 0, 1),
    (N'Scores', N'RoundId', N'int', 0, 0),
    (N'Scores', N'PlayerId', N'int', 0, 0),
    (N'Scores', N'HoleId', N'int', 0, 0),
    (N'Scores', N'Strokes', N'int', 0, 0),
    (N'Scores', N'Putts', N'int', 1, 0),
    (N'Scores', N'FairwayHit', N'bit', 1, 0),
    (N'Scores', N'TeeSetId', N'int', 1, 0);

INSERT INTO #ExpectedPrimaryKeys (TableName, ConstraintName, Columns) VALUES
    (N'AiProviderSettings', N'PK_AiProviderSettings', N'AiProviderSettingsId'),
    (N'ApplicationSettings', N'PK_ApplicationSettings', N'Id'),
    (N'AspNetRoles', N'PK_AspNetRoles', N'Id'),
    (N'GolfClubs', N'PK_GolfClubs', N'GolfClubId'),
    (N'AspNetRoleClaims', N'PK_AspNetRoleClaims', N'Id'),
    (N'GolfCourses', N'PK_GolfCourses', N'GolfCourseId'),
    (N'Holes', N'PK_Holes', N'HoleId'),
    (N'TeeSets', N'PK_TeeSets', N'TeeSetId'),
    (N'HoleTees', N'PK_HoleTees', N'HoleTeeId'),
    (N'AiAuditLogs', N'PK_AiAuditLogs', N'AiAuditLogId'),
    (N'AiChatSessionMessages', N'PK_AiChatSessionMessages', N'AiChatSessionMessageId'),
    (N'AiChatSessions', N'PK_AiChatSessions', N'AiChatSessionId'),
    (N'AspNetUserClaims', N'PK_AspNetUserClaims', N'Id'),
    (N'AspNetUserLogins', N'PK_AspNetUserLogins', N'LoginProvider,ProviderKey'),
    (N'AspNetUserRoles', N'PK_AspNetUserRoles', N'UserId,RoleId'),
    (N'AspNetUsers', N'PK_AspNetUsers', N'Id'),
    (N'AspNetUserTokens', N'PK_AspNetUserTokens', N'UserId,LoginProvider,Name'),
    (N'ClubMemberships', N'PK_ClubMemberships', N'ClubMembershipId'),
    (N'GolfSocieties', N'PK_GolfSocieties', N'GolfSocietyId'),
    (N'Notifications', N'PK_Notifications', N'Id'),
    (N'PlayerConnections', N'PK_PlayerConnections', N'Id'),
    (N'Players', N'PK_Players', N'PlayerId'),
    (N'Rounds', N'PK_Rounds', N'RoundId'),
    (N'SocietyMemberships', N'PK_SocietyMemberships', N'SocietyMembershipId'),
    (N'PlayerMergeRequests', N'PK_PlayerMergeRequests', N'Id'),
    (N'RoundPlayers', N'PK_RoundPlayers', N'RoundId,PlayerId'),
    (N'Scores', N'PK_Scores', N'ScoreId');

INSERT INTO #ExpectedIndexes (TableName, IndexName, IsUnique, Columns) VALUES
    (N'AiAuditLogs', N'IX_AiAuditLogs_AiChatSessionId', 0, N'AiChatSessionId'),
    (N'AiAuditLogs', N'IX_AiAuditLogs_ApplicationUserId_RequestedAt', 0, N'ApplicationUserId,RequestedAt'),
    (N'AiAuditLogs', N'IX_AiAuditLogs_RequestedAt', 0, N'RequestedAt'),
    (N'AiChatSessionMessages', N'IX_AiChatSessionMessages_AiChatSessionId_Timestamp', 0, N'AiChatSessionId,Timestamp'),
    (N'AiChatSessions', N'IX_AiChatSessions_ApplicationUserId_LastMessageAt', 0, N'ApplicationUserId,LastMessageAt'),
    (N'AiProviderSettings', N'IX_AiProviderSettings_ProviderName', 1, N'ProviderName'),
    (N'ApplicationSettings', N'IX_ApplicationSettings_Key', 1, N'Key'),
    (N'AspNetRoleClaims', N'IX_AspNetRoleClaims_RoleId', 0, N'RoleId'),
    (N'AspNetRoles', N'RoleNameIndex', 1, N'NormalizedName'),
    (N'AspNetUserClaims', N'IX_AspNetUserClaims_UserId', 0, N'UserId'),
    (N'AspNetUserLogins', N'IX_AspNetUserLogins_UserId', 0, N'UserId'),
    (N'AspNetUserRoles', N'IX_AspNetUserRoles_RoleId', 0, N'RoleId'),
    (N'AspNetUsers', N'EmailIndex', 0, N'NormalizedEmail'),
    (N'AspNetUsers', N'IX_AspNetUsers_LinkedPlayerId', 0, N'LinkedPlayerId'),
    (N'AspNetUsers', N'UserNameIndex', 1, N'NormalizedUserName'),
    (N'ClubMemberships', N'IX_ClubMemberships_GolfClubId_UserId', 1, N'GolfClubId,UserId'),
    (N'ClubMemberships', N'IX_ClubMemberships_UserId', 0, N'UserId'),
    (N'GolfCourses', N'IX_GolfCourses_GolfClubId', 0, N'GolfClubId'),
    (N'GolfSocieties', N'IX_GolfSocieties_CreatedByUserId', 0, N'CreatedByUserId'),
    (N'Holes', N'IX_Holes_GolfCourseId', 0, N'GolfCourseId'),
    (N'HoleTees', N'IX_HoleTees_HoleId_TeeSetId', 1, N'HoleId,TeeSetId'),
    (N'HoleTees', N'IX_HoleTees_TeeSetId', 0, N'TeeSetId'),
    (N'Notifications', N'IX_Notifications_UserId_IsRead_CreatedAt', 0, N'UserId,IsRead,CreatedAt'),
    (N'PlayerConnections', N'IX_PlayerConnections_RequestingUserId_TargetUserId', 1, N'RequestingUserId,TargetUserId'),
    (N'PlayerConnections', N'IX_PlayerConnections_TargetUserId', 0, N'TargetUserId'),
    (N'PlayerMergeRequests', N'IX_PlayerMergeRequests_RequestingUserId', 0, N'RequestingUserId'),
    (N'PlayerMergeRequests', N'IX_PlayerMergeRequests_SourcePlayerId', 0, N'SourcePlayerId'),
    (N'PlayerMergeRequests', N'IX_PlayerMergeRequests_TargetPlayerId', 0, N'TargetPlayerId'),
    (N'PlayerMergeRequests', N'IX_PlayerMergeRequests_TargetUserId', 0, N'TargetUserId'),
    (N'Players', N'IX_Players_ApplicationUserId', 0, N'ApplicationUserId'),
    (N'Players', N'IX_Players_CreatedByApplicationUserId', 0, N'CreatedByApplicationUserId'),
    (N'RoundPlayers', N'IX_RoundPlayers_PlayerId', 0, N'PlayerId'),
    (N'RoundPlayers', N'IX_RoundPlayers_TeeSetId', 0, N'TeeSetId'),
    (N'Rounds', N'IX_Rounds_CreatedByApplicationUserId', 0, N'CreatedByApplicationUserId'),
    (N'Rounds', N'IX_Rounds_GolfCourseId', 0, N'GolfCourseId'),
    (N'Scores', N'IX_Scores_HoleId', 0, N'HoleId'),
    (N'Scores', N'IX_Scores_PlayerId', 0, N'PlayerId'),
    (N'Scores', N'IX_Scores_RoundId', 0, N'RoundId'),
    (N'Scores', N'IX_Scores_TeeSetId', 0, N'TeeSetId'),
    (N'SocietyMemberships', N'IX_SocietyMemberships_GolfSocietyId_UserId', 1, N'GolfSocietyId,UserId'),
    (N'SocietyMemberships', N'IX_SocietyMemberships_UserId', 0, N'UserId'),
    (N'TeeSets', N'IX_TeeSets_GolfCourseId_Name', 1, N'GolfCourseId,Name');

INSERT INTO #ExpectedForeignKeys
    (TableName, ConstraintName, Columns, ReferencedTable, ReferencedColumns, DeleteAction)
VALUES
    (N'AspNetRoleClaims', N'FK_AspNetRoleClaims_AspNetRoles_RoleId', N'RoleId', N'AspNetRoles', N'Id', N'CASCADE'),
    (N'GolfCourses', N'FK_GolfCourses_GolfClubs_GolfClubId', N'GolfClubId', N'GolfClubs', N'GolfClubId', N'CASCADE'),
    (N'Holes', N'FK_Holes_GolfCourses_GolfCourseId', N'GolfCourseId', N'GolfCourses', N'GolfCourseId', N'CASCADE'),
    (N'TeeSets', N'FK_TeeSets_GolfCourses_GolfCourseId', N'GolfCourseId', N'GolfCourses', N'GolfCourseId', N'CASCADE'),
    (N'HoleTees', N'FK_HoleTees_Holes_HoleId', N'HoleId', N'Holes', N'HoleId', N'CASCADE'),
    (N'HoleTees', N'FK_HoleTees_TeeSets_TeeSetId', N'TeeSetId', N'TeeSets', N'TeeSetId', N'NO ACTION'),
    (N'AspNetUserRoles', N'FK_AspNetUserRoles_AspNetRoles_RoleId', N'RoleId', N'AspNetRoles', N'Id', N'CASCADE'),
    (N'AspNetUserTokens', N'FK_AspNetUserTokens_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'ClubMemberships', N'FK_ClubMemberships_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'ClubMemberships', N'FK_ClubMemberships_GolfClubs_GolfClubId', N'GolfClubId', N'GolfClubs', N'GolfClubId', N'CASCADE'),
    (N'GolfSocieties', N'FK_GolfSocieties_AspNetUsers_CreatedByUserId', N'CreatedByUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'Notifications', N'FK_Notifications_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'PlayerConnections', N'FK_PlayerConnections_AspNetUsers_RequestingUserId', N'RequestingUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'PlayerConnections', N'FK_PlayerConnections_AspNetUsers_TargetUserId', N'TargetUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'Players', N'FK_Players_AspNetUsers_ApplicationUserId', N'ApplicationUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'Players', N'FK_Players_AspNetUsers_CreatedByApplicationUserId', N'CreatedByApplicationUserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'Rounds', N'FK_Rounds_AspNetUsers_CreatedByApplicationUserId', N'CreatedByApplicationUserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'Rounds', N'FK_Rounds_GolfCourses_GolfCourseId', N'GolfCourseId', N'GolfCourses', N'GolfCourseId', N'CASCADE'),
    (N'SocietyMemberships', N'FK_SocietyMemberships_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'SocietyMemberships', N'FK_SocietyMemberships_GolfSocieties_GolfSocietyId', N'GolfSocietyId', N'GolfSocieties', N'GolfSocietyId', N'CASCADE'),
    (N'PlayerMergeRequests', N'FK_PlayerMergeRequests_AspNetUsers_RequestingUserId', N'RequestingUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'PlayerMergeRequests', N'FK_PlayerMergeRequests_AspNetUsers_TargetUserId', N'TargetUserId', N'AspNetUsers', N'Id', N'NO ACTION'),
    (N'PlayerMergeRequests', N'FK_PlayerMergeRequests_Players_SourcePlayerId', N'SourcePlayerId', N'Players', N'PlayerId', N'NO ACTION'),
    (N'PlayerMergeRequests', N'FK_PlayerMergeRequests_Players_TargetPlayerId', N'TargetPlayerId', N'Players', N'PlayerId', N'NO ACTION'),
    (N'RoundPlayers', N'FK_RoundPlayers_Players_PlayerId', N'PlayerId', N'Players', N'PlayerId', N'NO ACTION'),
    (N'RoundPlayers', N'FK_RoundPlayers_Rounds_RoundId', N'RoundId', N'Rounds', N'RoundId', N'NO ACTION'),
    (N'RoundPlayers', N'FK_RoundPlayers_TeeSets_TeeSetId', N'TeeSetId', N'TeeSets', N'TeeSetId', N'NO ACTION'),
    (N'Scores', N'FK_Scores_Holes_HoleId', N'HoleId', N'Holes', N'HoleId', N'NO ACTION'),
    (N'Scores', N'FK_Scores_Players_PlayerId', N'PlayerId', N'Players', N'PlayerId', N'NO ACTION'),
    (N'Scores', N'FK_Scores_Rounds_RoundId', N'RoundId', N'Rounds', N'RoundId', N'CASCADE'),
    (N'Scores', N'FK_Scores_TeeSets_TeeSetId', N'TeeSetId', N'TeeSets', N'TeeSetId', N'NO ACTION'),
    (N'AiAuditLogs', N'FK_AiAuditLogs_AiChatSessions_AiChatSessionId', N'AiChatSessionId', N'AiChatSessions', N'AiChatSessionId', N'SET NULL'),
    (N'AiAuditLogs', N'FK_AiAuditLogs_AspNetUsers_ApplicationUserId', N'ApplicationUserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'AiChatSessionMessages', N'FK_AiChatSessionMessages_AiChatSessions_AiChatSessionId', N'AiChatSessionId', N'AiChatSessions', N'AiChatSessionId', N'CASCADE'),
    (N'AiChatSessions', N'FK_AiChatSessions_AspNetUsers_ApplicationUserId', N'ApplicationUserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'AspNetUserClaims', N'FK_AspNetUserClaims_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'AspNetUserLogins', N'FK_AspNetUserLogins_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'AspNetUserRoles', N'FK_AspNetUserRoles_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'CASCADE'),
    (N'AspNetUsers', N'FK_AspNetUsers_Players_LinkedPlayerId', N'LinkedPlayerId', N'Players', N'PlayerId', N'SET NULL');

ALTER TABLE #ExpectedIndexes ADD HasFilter bit NOT NULL DEFAULT 0;
UPDATE #ExpectedIndexes
SET HasFilter = 1
WHERE IndexName IN (N'RoleNameIndex', N'UserNameIndex');

CREATE TABLE #ActualColumns (
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    TypeName nvarchar(128) NOT NULL,
    IsNullable bit NOT NULL,
    IsIdentity bit NOT NULL,
    PRIMARY KEY (TableName, ColumnName)
);

INSERT INTO #ActualColumns (TableName, ColumnName, TypeName, IsNullable, IsIdentity)
SELECT
    tableObject.name,
    columnObject.name,
    LOWER(typeObject.name) +
        CASE
            WHEN typeObject.name IN (N'nvarchar', N'nchar')
                THEN N'(' + CASE WHEN columnObject.max_length = -1 THEN N'max'
                    ELSE CONVERT(nvarchar(10), columnObject.max_length / 2) END + N')'
            WHEN typeObject.name IN (N'varchar', N'char', N'varbinary', N'binary')
                THEN N'(' + CASE WHEN columnObject.max_length = -1 THEN N'max'
                    ELSE CONVERT(nvarchar(10), columnObject.max_length) END + N')'
            WHEN typeObject.name IN (N'decimal', N'numeric')
                THEN N'(' + CONVERT(nvarchar(10), columnObject.precision) + N',' +
                    CONVERT(nvarchar(10), columnObject.scale) + N')'
            ELSE N''
        END,
    columnObject.is_nullable,
    columnObject.is_identity
FROM sys.tables tableObject
JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
JOIN sys.columns columnObject ON columnObject.object_id = tableObject.object_id
JOIN sys.types typeObject ON typeObject.user_type_id = columnObject.user_type_id
WHERE schemaObject.name = N'dbo'
  AND tableObject.name <> N'__EFMigrationsHistory';

CREATE TABLE #ActualPrimaryKeys (
    TableName sysname NOT NULL,
    ConstraintName sysname NOT NULL,
    Columns nvarchar(max) NOT NULL,
    PRIMARY KEY (TableName, ConstraintName)
);

INSERT INTO #ActualPrimaryKeys (TableName, ConstraintName, Columns)
SELECT
    tableObject.name,
    keyObject.name,
    STRING_AGG(CONVERT(nvarchar(max), columnObject.name), N',')
        WITHIN GROUP (ORDER BY indexColumn.key_ordinal)
FROM sys.key_constraints keyObject
JOIN sys.tables tableObject ON tableObject.object_id = keyObject.parent_object_id
JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
JOIN sys.index_columns indexColumn
    ON indexColumn.object_id = tableObject.object_id
   AND indexColumn.index_id = keyObject.unique_index_id
JOIN sys.columns columnObject
    ON columnObject.object_id = tableObject.object_id
   AND columnObject.column_id = indexColumn.column_id
WHERE schemaObject.name = N'dbo'
  AND keyObject.type = N'PK'
GROUP BY tableObject.name, keyObject.name;

CREATE TABLE #ActualIndexes (
    TableName sysname NOT NULL,
    IndexName sysname NOT NULL,
    IsUnique bit NOT NULL,
    Columns nvarchar(max) NOT NULL,
    HasFilter bit NOT NULL,
    PRIMARY KEY (TableName, IndexName)
);

INSERT INTO #ActualIndexes (TableName, IndexName, IsUnique, Columns, HasFilter)
SELECT
    tableObject.name,
    indexObject.name,
    indexObject.is_unique,
    STRING_AGG(CONVERT(nvarchar(max), columnObject.name), N',')
        WITHIN GROUP (ORDER BY indexColumn.key_ordinal),
    indexObject.has_filter
FROM sys.indexes indexObject
JOIN sys.tables tableObject ON tableObject.object_id = indexObject.object_id
JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
JOIN sys.index_columns indexColumn
    ON indexColumn.object_id = tableObject.object_id
   AND indexColumn.index_id = indexObject.index_id
JOIN sys.columns columnObject
    ON columnObject.object_id = tableObject.object_id
   AND columnObject.column_id = indexColumn.column_id
WHERE schemaObject.name = N'dbo'
  AND indexObject.name IS NOT NULL
  AND indexObject.is_primary_key = 0
  AND indexObject.is_unique_constraint = 0
  AND indexObject.is_hypothetical = 0
  AND indexColumn.is_included_column = 0
  AND indexColumn.key_ordinal > 0
GROUP BY tableObject.name, indexObject.name, indexObject.is_unique, indexObject.has_filter;

CREATE TABLE #ActualForeignKeys (
    TableName sysname NOT NULL,
    ConstraintName sysname NOT NULL,
    Columns nvarchar(max) NOT NULL,
    ReferencedTable sysname NOT NULL,
    ReferencedColumns nvarchar(max) NOT NULL,
    DeleteAction nvarchar(60) NOT NULL,
    PRIMARY KEY (TableName, ConstraintName)
);

INSERT INTO #ActualForeignKeys
    (TableName, ConstraintName, Columns, ReferencedTable, ReferencedColumns, DeleteAction)
SELECT
    parentTable.name,
    foreignKey.name,
    STRING_AGG(CONVERT(nvarchar(max), parentColumn.name), N',')
        WITHIN GROUP (ORDER BY foreignKeyColumn.constraint_column_id),
    referencedTable.name,
    STRING_AGG(CONVERT(nvarchar(max), referencedColumn.name), N',')
        WITHIN GROUP (ORDER BY foreignKeyColumn.constraint_column_id),
    REPLACE(foreignKey.delete_referential_action_desc, N'_', N' ')
FROM sys.foreign_keys foreignKey
JOIN sys.tables parentTable ON parentTable.object_id = foreignKey.parent_object_id
JOIN sys.schemas parentSchema ON parentSchema.schema_id = parentTable.schema_id
JOIN sys.tables referencedTable ON referencedTable.object_id = foreignKey.referenced_object_id
JOIN sys.foreign_key_columns foreignKeyColumn
    ON foreignKeyColumn.constraint_object_id = foreignKey.object_id
JOIN sys.columns parentColumn
    ON parentColumn.object_id = parentTable.object_id
   AND parentColumn.column_id = foreignKeyColumn.parent_column_id
JOIN sys.columns referencedColumn
    ON referencedColumn.object_id = referencedTable.object_id
   AND referencedColumn.column_id = foreignKeyColumn.referenced_column_id
WHERE parentSchema.name = N'dbo'
GROUP BY parentTable.name, foreignKey.name, referencedTable.name,
    foreignKey.delete_referential_action_desc;

-- Tables
INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Missing table', N'dbo.' + expected.TableName, N'Present', N'Missing'
FROM #ExpectedTables expected
WHERE OBJECT_ID(N'dbo.' + QUOTENAME(expected.TableName), N'U') IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Unexpected table', schemaObject.name + N'.' + tableObject.name, N'Absent', N'Present'
FROM sys.tables tableObject
JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
LEFT JOIN #ExpectedTables expected
    ON schemaObject.name = N'dbo' AND expected.TableName = tableObject.name
WHERE tableObject.name <> N'__EFMigrationsHistory'
  AND expected.TableName IS NULL;

-- Columns
INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Missing column', N'dbo.' + expected.TableName + N'.' + expected.ColumnName,
    expected.TypeName, N'Missing'
FROM #ExpectedColumns expected
LEFT JOIN #ActualColumns actual
    ON actual.TableName = expected.TableName
   AND actual.ColumnName = expected.ColumnName
WHERE actual.ColumnName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Unexpected column', N'dbo.' + actual.TableName + N'.' + actual.ColumnName,
    N'Absent', actual.TypeName
FROM #ActualColumns actual
JOIN #ExpectedTables expectedTable ON expectedTable.TableName = actual.TableName
LEFT JOIN #ExpectedColumns expected
    ON expected.TableName = actual.TableName
   AND expected.ColumnName = actual.ColumnName
WHERE expected.ColumnName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Column mismatch', N'dbo.' + expected.TableName + N'.' + expected.ColumnName,
    expected.TypeName + N'; nullable=' + CONVERT(nvarchar(1), expected.IsNullable) +
        N'; identity=' + CONVERT(nvarchar(1), expected.IsIdentity),
    actual.TypeName + N'; nullable=' + CONVERT(nvarchar(1), actual.IsNullable) +
        N'; identity=' + CONVERT(nvarchar(1), actual.IsIdentity)
FROM #ExpectedColumns expected
JOIN #ActualColumns actual
    ON actual.TableName = expected.TableName
   AND actual.ColumnName = expected.ColumnName
WHERE actual.TypeName <> expected.TypeName
   OR actual.IsNullable <> expected.IsNullable
   OR actual.IsIdentity <> expected.IsIdentity;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Unexpected default', N'dbo.' + tableObject.name + N'.' + columnObject.name,
    N'No default constraint', defaultObject.definition
FROM sys.default_constraints defaultObject
JOIN sys.tables tableObject ON tableObject.object_id = defaultObject.parent_object_id
JOIN sys.schemas schemaObject ON schemaObject.schema_id = tableObject.schema_id
JOIN sys.columns columnObject
    ON columnObject.object_id = tableObject.object_id
   AND columnObject.column_id = defaultObject.parent_column_id
JOIN #ExpectedColumns expected
    ON expected.TableName = tableObject.name
   AND expected.ColumnName = columnObject.name
WHERE schemaObject.name = N'dbo';

-- Primary keys
INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Missing primary key', N'dbo.' + expected.TableName + N'.' + expected.ConstraintName,
    expected.Columns, N'Missing'
FROM #ExpectedPrimaryKeys expected
LEFT JOIN #ActualPrimaryKeys actual
    ON actual.TableName = expected.TableName
   AND actual.ConstraintName = expected.ConstraintName
WHERE actual.ConstraintName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Unexpected primary key', N'dbo.' + actual.TableName + N'.' + actual.ConstraintName,
    N'Absent', actual.Columns
FROM #ActualPrimaryKeys actual
JOIN #ExpectedTables expectedTable ON expectedTable.TableName = actual.TableName
LEFT JOIN #ExpectedPrimaryKeys expected
    ON expected.TableName = actual.TableName
   AND expected.ConstraintName = actual.ConstraintName
WHERE expected.ConstraintName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Primary key mismatch', N'dbo.' + expected.TableName + N'.' + expected.ConstraintName,
    expected.Columns, actual.Columns
FROM #ExpectedPrimaryKeys expected
JOIN #ActualPrimaryKeys actual
    ON actual.TableName = expected.TableName
   AND actual.ConstraintName = expected.ConstraintName
WHERE actual.Columns <> expected.Columns;

-- Indexes
INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Missing index', N'dbo.' + expected.TableName + N'.' + expected.IndexName,
    expected.Columns, N'Missing'
FROM #ExpectedIndexes expected
LEFT JOIN #ActualIndexes actual
    ON actual.TableName = expected.TableName
   AND actual.IndexName = expected.IndexName
WHERE actual.IndexName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Unexpected index', N'dbo.' + actual.TableName + N'.' + actual.IndexName,
    N'Absent', actual.Columns
FROM #ActualIndexes actual
JOIN #ExpectedTables expectedTable ON expectedTable.TableName = actual.TableName
LEFT JOIN #ExpectedIndexes expected
    ON expected.TableName = actual.TableName
   AND expected.IndexName = actual.IndexName
WHERE expected.IndexName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Index mismatch', N'dbo.' + expected.TableName + N'.' + expected.IndexName,
    expected.Columns + N'; unique=' + CONVERT(nvarchar(1), expected.IsUnique) +
        N'; filtered=' + CONVERT(nvarchar(1), expected.HasFilter),
    actual.Columns + N'; unique=' + CONVERT(nvarchar(1), actual.IsUnique) +
        N'; filtered=' + CONVERT(nvarchar(1), actual.HasFilter)
FROM #ExpectedIndexes expected
JOIN #ActualIndexes actual
    ON actual.TableName = expected.TableName
   AND actual.IndexName = expected.IndexName
WHERE actual.Columns <> expected.Columns
   OR actual.IsUnique <> expected.IsUnique
   OR actual.HasFilter <> expected.HasFilter;

-- Foreign keys
INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Missing foreign key', N'dbo.' + expected.TableName + N'.' + expected.ConstraintName,
    expected.Columns + N' -> ' + expected.ReferencedTable + N'.' + expected.ReferencedColumns,
    N'Missing'
FROM #ExpectedForeignKeys expected
LEFT JOIN #ActualForeignKeys actual
    ON actual.TableName = expected.TableName
   AND actual.ConstraintName = expected.ConstraintName
WHERE actual.ConstraintName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Unexpected foreign key', N'dbo.' + actual.TableName + N'.' + actual.ConstraintName,
    N'Absent', actual.Columns + N' -> ' + actual.ReferencedTable + N'.' + actual.ReferencedColumns
FROM #ActualForeignKeys actual
JOIN #ExpectedTables expectedTable ON expectedTable.TableName = actual.TableName
LEFT JOIN #ExpectedForeignKeys expected
    ON expected.TableName = actual.TableName
   AND expected.ConstraintName = actual.ConstraintName
WHERE expected.ConstraintName IS NULL;

INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
SELECT N'Foreign key mismatch', N'dbo.' + expected.TableName + N'.' + expected.ConstraintName,
    expected.Columns + N' -> ' + expected.ReferencedTable + N'.' + expected.ReferencedColumns +
        N'; delete=' + expected.DeleteAction,
    actual.Columns + N' -> ' + actual.ReferencedTable + N'.' + actual.ReferencedColumns +
        N'; delete=' + actual.DeleteAction
FROM #ExpectedForeignKeys expected
JOIN #ActualForeignKeys actual
    ON actual.TableName = expected.TableName
   AND actual.ConstraintName = expected.ConstraintName
WHERE actual.Columns <> expected.Columns
   OR actual.ReferencedTable <> expected.ReferencedTable
   OR actual.ReferencedColumns <> expected.ReferencedColumns
   OR actual.DeleteAction <> expected.DeleteAction;

-- Migration history is allowed to be absent before baselining, but unexpected
-- rows or an incorrect baseline row are drift.
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
    BEGIN
        INSERT INTO #Drift VALUES
            (N'Migration history mismatch', N'dbo.__EFMigrationsHistory',
             N'MigrationId + ProductVersion columns', N'Unexpected shape');
    END
    ELSE
    BEGIN
        EXEC(N'
            INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
            SELECT N''Unexpected history row'', N''dbo.__EFMigrationsHistory.'' + MigrationId,
                N''Absent'', ProductVersion
            FROM dbo.__EFMigrationsHistory
            WHERE MigrationId <> N''20260611161345_InitialSqlServer'';

            INSERT INTO #Drift (Category, ObjectName, Expected, Actual)
            SELECT N''Baseline version mismatch'', N''dbo.__EFMigrationsHistory.'' + MigrationId,
                N''10.0.3'', ProductVersion
            FROM dbo.__EFMigrationsHistory
            WHERE MigrationId = N''20260611161345_InitialSqlServer''
              AND ProductVersion <> N''10.0.3'';');
    END;
END;

SELECT Category, ObjectName, Expected, Actual
FROM #Drift
ORDER BY Category, ObjectName;

DECLARE @ErrorCount int = (SELECT COUNT(*) FROM #Drift);
DECLARE @BaselineRecorded bit = 0;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM #Drift
       WHERE Category = N'Migration history mismatch'
   )
BEGIN
    EXEC sys.sp_executesql N'
        IF EXISTS (
            SELECT 1
            FROM dbo.__EFMigrationsHistory
            WHERE MigrationId = N''20260611161345_InitialSqlServer''
              AND ProductVersion = N''10.0.3''
        )
            SET @Recorded = 1;',
        N'@Recorded bit OUTPUT', @BaselineRecorded OUTPUT;
END;

SELECT
    DB_NAME() AS DatabaseName,
    N'20260611161345_InitialSqlServer' AS BaselineMigrationId,
    @BaselineRecorded AS BaselineRecorded,
    @ErrorCount AS ErrorCount,
    CASE WHEN @ErrorCount = 0 THEN N'READY TO BASELINE'
         ELSE N'STOP - RECONCILE DRIFT BEFORE BASELINING' END AS Result;

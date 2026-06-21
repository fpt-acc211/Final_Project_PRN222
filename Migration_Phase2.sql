-- Phase 2 Migration: Role Management, User Profile, Login Rate Limiting
-- Run this script against the existing QuizManagementDB database.
-- Safe to run multiple times (uses IF NOT EXISTS checks).

USE QuizManagementDB;
GO

-- 1. Add AvatarUrl column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Users') AND name = N'AvatarUrl'
)
BEGIN
    ALTER TABLE Users ADD AvatarUrl NVARCHAR(500) NULL;
    PRINT 'Added column: Users.AvatarUrl';
END
GO

-- 2. Add IsDisabled column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Users') AND name = N'IsDisabled'
)
BEGIN
    ALTER TABLE Users ADD IsDisabled BIT NOT NULL DEFAULT 0;
    PRINT 'Added column: Users.IsDisabled';
END
GO

-- 3. Add SecurityStamp column (used to invalidate cookies on password change / role change)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Users') AND name = N'SecurityStamp'
)
BEGIN
    ALTER TABLE Users ADD SecurityStamp NVARCHAR(450) NULL;
    PRINT 'Added column: Users.SecurityStamp';
END
GO

-- Rollback plan:
-- ALTER TABLE Users DROP COLUMN AvatarUrl;
-- ALTER TABLE Users DROP COLUMN IsDisabled;
-- ALTER TABLE Users DROP COLUMN SecurityStamp;

-- =============================================
-- Migration: V107__Normalize_Legacy_Auto_UserNames
-- Author: System
-- Date: 2026-04-08
-- Description: Normalize legacy auto-generated usernames from given+family to family.given.
-- =============================================

SET NOCOUNT ON;
GO

PRINT 'Applying migration V107: normalize legacy auto usernames...';
GO

BEGIN TRANSACTION;

DECLARE
    @EntityId INT,
    @FullName NVARCHAR(255),
    @OldUserName VARCHAR(100),
    @NormalizedName NVARCHAR(255),
    @CurrentBase VARCHAR(100),
    @LegacyBase VARCHAR(100),
    @FinalUser VARCHAR(100),
    @Suffix VARCHAR(20),
    @Counter INT,
    @Exists BIT,
    @FirstSpace INT,
    @LastSpace INT;

DECLARE employee_cursor CURSOR FOR
SELECT Id, FullName, UserName
FROM Employees
WHERE ISNULL(Deleted, 0) = 0
  AND NULLIF(LTRIM(RTRIM(FullName)), '') IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(UserName)), '') IS NOT NULL;

OPEN employee_cursor;
FETCH NEXT FROM employee_cursor INTO @EntityId, @FullName, @OldUserName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @NormalizedName = LOWER(LTRIM(RTRIM(@FullName)));

    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(224), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(225), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7843), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(227), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7841), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(226), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7847), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7845), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7849), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7851), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7853), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(259), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7857), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7855), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7859), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7861), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7863), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(232), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(233), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7867), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7869), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7865), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(234), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7873), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7871), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7875), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7877), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7879), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(236), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(237), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7881), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(297), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7883), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(242), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(243), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7887), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(245), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7885), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(244), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7891), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7889), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7893), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7895), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7897), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(417), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7901), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7899), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7903), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7905), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7907), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(249), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(250), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7911), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(361), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7909), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(432), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7915), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7913), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7917), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7919), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7921), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7923), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(253), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7927), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7929), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7925), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(273), 'd');

    SET @NormalizedName = REPLACE(@NormalizedName, '.', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '-', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '_', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, ',', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, ';', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, ':', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '/', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '\', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, CHAR(39), ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, CHAR(34), ' ');

    WHILE CHARINDEX('  ', @NormalizedName) > 0
    BEGIN
        SET @NormalizedName = REPLACE(@NormalizedName, '  ', ' ');
    END

    SET @NormalizedName = LTRIM(RTRIM(@NormalizedName));
    SET @CurrentBase = NULL;
    SET @LegacyBase = NULL;

    IF @NormalizedName <> ''
    BEGIN
        SET @FirstSpace = CHARINDEX(' ', @NormalizedName);
        SET @LastSpace = LEN(@NormalizedName) - CHARINDEX(' ', REVERSE(@NormalizedName)) + 1;

        IF @FirstSpace = 0
        BEGIN
            SET @CurrentBase = @NormalizedName;
            SET @LegacyBase = @NormalizedName;
        END
        ELSE
        BEGIN
            SET @CurrentBase = SUBSTRING(@NormalizedName, 1, @FirstSpace - 1) + '.' +
                               SUBSTRING(@NormalizedName, @LastSpace + 1, LEN(@NormalizedName) - @LastSpace);
            SET @LegacyBase = SUBSTRING(@NormalizedName, @LastSpace + 1, LEN(@NormalizedName) - @LastSpace) +
                              SUBSTRING(@NormalizedName, 1, @FirstSpace - 1);
        END
    END

    IF NULLIF(@CurrentBase, '') IS NOT NULL
       AND NULLIF(@LegacyBase, '') IS NOT NULL
       AND @CurrentBase <> @LegacyBase
    BEGIN
        SET @OldUserName = LOWER(LTRIM(RTRIM(@OldUserName)));
        SET @Suffix = '';

        IF LEFT(@OldUserName, LEN(@LegacyBase)) = @LegacyBase
        BEGIN
            SET @Suffix = SUBSTRING(@OldUserName, LEN(@LegacyBase) + 1, LEN(@OldUserName) - LEN(@LegacyBase));
        END

        IF @OldUserName = @LegacyBase
           OR (LEFT(@OldUserName, LEN(@LegacyBase)) = @LegacyBase
               AND @Suffix <> ''
               AND @Suffix NOT LIKE '%[^0-9]%')
        BEGIN
            SET @FinalUser = @CurrentBase;
            SET @Counter = 2;

            IF @Suffix <> '' AND TRY_CONVERT(INT, @Suffix) IS NOT NULL AND TRY_CONVERT(INT, @Suffix) >= 2
            BEGIN
                SET @FinalUser = @CurrentBase + @Suffix;
                SET @Counter = TRY_CONVERT(INT, @Suffix) + 1;
            END

            WHILE 1 = 1
            BEGIN
                SET @Exists = 0;
                IF EXISTS (SELECT 1 FROM Employees WHERE UserName = @FinalUser AND Id <> @EntityId) SET @Exists = 1;
                IF EXISTS (SELECT 1 FROM Candidate WHERE UserName = @FinalUser) SET @Exists = 1;

                IF @Exists = 0 BREAK;

                SET @FinalUser = @CurrentBase + CAST(@Counter AS VARCHAR(10));
                SET @Counter = @Counter + 1;
            END

            UPDATE Employees SET UserName = @FinalUser WHERE Id = @EntityId;
            UPDATE BHXHItem SET UserName = @FinalUser WHERE UserName = @OldUserName;
            UPDATE TaxItem SET UserName = @FinalUser WHERE UserName = @OldUserName;
            UPDATE RelationItem SET UserName = @FinalUser WHERE UserName = @OldUserName;
        END
    END

    FETCH NEXT FROM employee_cursor INTO @EntityId, @FullName, @OldUserName;
END

CLOSE employee_cursor;
DEALLOCATE employee_cursor;

DECLARE candidate_cursor CURSOR FOR
SELECT Id, Name, UserName
FROM Candidate
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(IsEmployee, 0) = 0
  AND NULLIF(LTRIM(RTRIM(Name)), '') IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(UserName)), '') IS NOT NULL;

OPEN candidate_cursor;
FETCH NEXT FROM candidate_cursor INTO @EntityId, @FullName, @OldUserName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @NormalizedName = LOWER(LTRIM(RTRIM(@FullName)));

    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(224), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(225), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7843), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(227), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7841), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(226), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7847), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7845), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7849), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7851), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7853), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(259), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7857), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7855), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7859), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7861), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7863), 'a');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(232), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(233), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7867), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7869), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7865), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(234), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7873), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7871), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7875), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7877), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7879), 'e');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(236), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(237), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7881), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(297), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7883), 'i');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(242), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(243), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7887), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(245), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7885), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(244), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7891), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7889), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7893), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7895), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7897), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(417), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7901), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7899), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7903), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7905), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7907), 'o');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(249), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(250), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7911), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(361), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7909), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(432), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7915), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7913), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7917), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7919), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7921), 'u');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7923), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(253), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7927), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7929), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(7925), 'y');
    SET @NormalizedName = REPLACE(@NormalizedName, NCHAR(273), 'd');

    SET @NormalizedName = REPLACE(@NormalizedName, '.', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '-', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '_', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, ',', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, ';', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, ':', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '/', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, '\', ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, CHAR(39), ' ');
    SET @NormalizedName = REPLACE(@NormalizedName, CHAR(34), ' ');

    WHILE CHARINDEX('  ', @NormalizedName) > 0
    BEGIN
        SET @NormalizedName = REPLACE(@NormalizedName, '  ', ' ');
    END

    SET @NormalizedName = LTRIM(RTRIM(@NormalizedName));
    SET @CurrentBase = NULL;
    SET @LegacyBase = NULL;

    IF @NormalizedName <> ''
    BEGIN
        SET @FirstSpace = CHARINDEX(' ', @NormalizedName);
        SET @LastSpace = LEN(@NormalizedName) - CHARINDEX(' ', REVERSE(@NormalizedName)) + 1;

        IF @FirstSpace = 0
        BEGIN
            SET @CurrentBase = @NormalizedName;
            SET @LegacyBase = @NormalizedName;
        END
        ELSE
        BEGIN
            SET @CurrentBase = SUBSTRING(@NormalizedName, 1, @FirstSpace - 1) + '.' +
                               SUBSTRING(@NormalizedName, @LastSpace + 1, LEN(@NormalizedName) - @LastSpace);
            SET @LegacyBase = SUBSTRING(@NormalizedName, @LastSpace + 1, LEN(@NormalizedName) - @LastSpace) +
                              SUBSTRING(@NormalizedName, 1, @FirstSpace - 1);
        END
    END

    IF NULLIF(@CurrentBase, '') IS NOT NULL
       AND NULLIF(@LegacyBase, '') IS NOT NULL
       AND @CurrentBase <> @LegacyBase
    BEGIN
        SET @OldUserName = LOWER(LTRIM(RTRIM(@OldUserName)));
        SET @Suffix = '';

        IF LEFT(@OldUserName, LEN(@LegacyBase)) = @LegacyBase
        BEGIN
            SET @Suffix = SUBSTRING(@OldUserName, LEN(@LegacyBase) + 1, LEN(@OldUserName) - LEN(@LegacyBase));
        END

        IF @OldUserName = @LegacyBase
           OR (LEFT(@OldUserName, LEN(@LegacyBase)) = @LegacyBase
               AND @Suffix <> ''
               AND @Suffix NOT LIKE '%[^0-9]%')
        BEGIN
            SET @FinalUser = @CurrentBase;
            SET @Counter = 2;

            IF @Suffix <> '' AND TRY_CONVERT(INT, @Suffix) IS NOT NULL AND TRY_CONVERT(INT, @Suffix) >= 2
            BEGIN
                SET @FinalUser = @CurrentBase + @Suffix;
                SET @Counter = TRY_CONVERT(INT, @Suffix) + 1;
            END

            WHILE 1 = 1
            BEGIN
                SET @Exists = 0;
                IF EXISTS (SELECT 1 FROM Employees WHERE UserName = @FinalUser) SET @Exists = 1;
                IF EXISTS (SELECT 1 FROM Candidate WHERE UserName = @FinalUser AND Id <> @EntityId) SET @Exists = 1;

                IF @Exists = 0 BREAK;

                SET @FinalUser = @CurrentBase + CAST(@Counter AS VARCHAR(10));
                SET @Counter = @Counter + 1;
            END

            UPDATE Candidate SET UserName = @FinalUser WHERE Id = @EntityId;
        END
    END

    FETCH NEXT FROM candidate_cursor INTO @EntityId, @FullName, @OldUserName;
END

CLOSE candidate_cursor;
DEALLOCATE candidate_cursor;

COMMIT TRANSACTION;
GO

PRINT 'Migration V107 completed successfully.';
GO

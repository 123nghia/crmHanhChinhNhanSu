-- =============================================
-- Migration: V054__Backfill_Employee_UserName_FromFullName
-- Author: System
-- Date: 2026-01-18
-- Description: Backfill employee usernames to family.given for numeric/empty usernames.
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V054: Backfill Employee UserName...';

DECLARE @EmployeeId INT;
DECLARE @FullName NVARCHAR(255);
DECLARE @OldUserName VARCHAR(100);
DECLARE @UnsignName NVARCHAR(255);
DECLARE @BaseUser VARCHAR(100);
DECLARE @FinalUser VARCHAR(100);
DECLARE @Counter INT;
DECLARE @Exists INT;
DECLARE @FirstSpace INT;
DECLARE @LastSpace INT;

DECLARE cur CURSOR FOR
SELECT Id, FullName, UserName
FROM Employees
WHERE FullName IS NOT NULL
  AND LTRIM(RTRIM(FullName)) <> ''
  AND (UserName IS NULL OR LTRIM(RTRIM(UserName)) = '' OR UserName NOT LIKE '%[^0-9]%');

OPEN cur;
FETCH NEXT FROM cur INTO @EmployeeId, @FullName, @OldUserName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @UnsignName = LOWER(LTRIM(RTRIM(@FullName)));

    SET @UnsignName = REPLACE(@UnsignName, NCHAR(224), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(225), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7843), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(227), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7841), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(226), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7847), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7845), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7849), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7851), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7853), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(259), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7857), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7855), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7859), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7861), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7863), 'a');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(232), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(233), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7867), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7869), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7865), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(234), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7873), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7871), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7875), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7877), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7879), 'e');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(236), 'i');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(237), 'i');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7881), 'i');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(297), 'i');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7883), 'i');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(242), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(243), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7887), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(245), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7885), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(244), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7891), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7889), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7893), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7895), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7897), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(417), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7901), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7899), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7903), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7905), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7907), 'o');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(249), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(250), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7911), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(361), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7909), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(432), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7915), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7913), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7917), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7919), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7921), 'u');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7923), 'y');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(253), 'y');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7927), 'y');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7929), 'y');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(7925), 'y');
    SET @UnsignName = REPLACE(@UnsignName, NCHAR(273), 'd');

    SET @UnsignName = REPLACE(@UnsignName, '.', ' ');
    SET @UnsignName = REPLACE(@UnsignName, '-', ' ');
    SET @UnsignName = REPLACE(@UnsignName, '_', ' ');
    SET @UnsignName = REPLACE(@UnsignName, ',', ' ');
    SET @UnsignName = REPLACE(@UnsignName, ';', ' ');
    SET @UnsignName = REPLACE(@UnsignName, ':', ' ');
    SET @UnsignName = REPLACE(@UnsignName, '/', ' ');
    SET @UnsignName = REPLACE(@UnsignName, '\', ' ');
    SET @UnsignName = REPLACE(@UnsignName, CHAR(39), ' ');
    SET @UnsignName = REPLACE(@UnsignName, CHAR(34), ' ');

    WHILE CHARINDEX('  ', @UnsignName) > 0
    BEGIN
        SET @UnsignName = REPLACE(@UnsignName, '  ', ' ');
    END

    SET @UnsignName = LTRIM(RTRIM(@UnsignName));

    SET @FirstSpace = CHARINDEX(' ', @UnsignName);
    SET @LastSpace = LEN(@UnsignName) - CHARINDEX(' ', REVERSE(@UnsignName)) + 1;

    IF @FirstSpace = 0
    BEGIN
        SET @BaseUser = @UnsignName;
    END
    ELSE
    BEGIN
        SET @BaseUser = SUBSTRING(@UnsignName, 1, @FirstSpace - 1) + '.' +
                        SUBSTRING(@UnsignName, @LastSpace + 1, LEN(@UnsignName) - @LastSpace);
    END

    IF @BaseUser IS NULL OR @BaseUser = ''
    BEGIN
        SET @BaseUser = 'user' + CAST(@EmployeeId AS VARCHAR(10));
    END

    SET @FinalUser = @BaseUser;
    SET @Counter = 2;

    WHILE 1=1
    BEGIN
        SET @Exists = 0;
        IF EXISTS (SELECT 1 FROM Employees WHERE UserName = @FinalUser AND Id <> @EmployeeId) SET @Exists = 1;
        IF EXISTS (SELECT 1 FROM Candidate WHERE UserName = @FinalUser) SET @Exists = 1;

        IF @Exists = 0 BREAK;

        SET @FinalUser = @BaseUser + CAST(@Counter AS VARCHAR(10));
        SET @Counter = @Counter + 1;

        IF @Counter > 1000 BREAK;
    END

    UPDATE Employees
    SET UserName = @FinalUser
    WHERE Id = @EmployeeId;

    IF @OldUserName IS NOT NULL AND LTRIM(RTRIM(@OldUserName)) <> ''
    BEGIN
        UPDATE BHXHItem SET UserName = @FinalUser WHERE UserName = @OldUserName;
        UPDATE TaxItem SET UserName = @FinalUser WHERE UserName = @OldUserName;
        UPDATE RelationItem SET UserName = @FinalUser WHERE UserName = @OldUserName;
    END

    FETCH NEXT FROM cur INTO @EmployeeId, @FullName, @OldUserName;
END

CLOSE cur;
DEALLOCATE cur;

PRINT 'Backfill Employee UserName completed.';

COMMIT TRANSACTION;

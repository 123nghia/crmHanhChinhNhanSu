-- =============================================
-- Migration: V042__Refine_Gen_Candidate_Username
-- Author: System
-- Date: 2026-01-16
-- Description: Update Candidate UserNames using Name+Surname rule (replacing Code-based usernames)
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V042: Refine Candidate Usernames...';

-- Logic: Iterate through candidates where UserName looks like a Code (CA...) or is null
-- And update them to normalized Name+Surname

DECLARE @CandidateId INT;
DECLARE @FullName NVARCHAR(100);
DECLARE @Email VARCHAR(100);
DECLARE @Phone VARCHAR(20);
DECLARE @UnsignName VARCHAR(100);
DECLARE @FinalUser VARCHAR(50);
DECLARE @BaseUser VARCHAR(50);
DECLARE @Counter INT;
DECLARE @Exists INT;

-- Define a temporary table or just simple loop
DECLARE cur CURSOR FOR 
SELECT Id, Name, Email, Phone 
FROM Candidate 
WHERE (UserName IS NULL OR UserName LIKE 'CA%') 
  AND Name IS NOT NULL;

OPEN cur;
FETCH NEXT FROM cur INTO @CandidateId, @FullName, @Email, @Phone;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 1. Normalize Name (Remove accents and Lowercase)
    SET @UnsignName = LOWER(@FullName);
    
    -- Manual Unaccent (Common chars)
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'á', 'a'), N'à', 'a'), N'ả', 'a'), N'ã', 'a'), N'ạ', 'a');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ă', 'a'), N'ắ', 'a'), N'ằ', 'a'), N'ẳ', 'a'), N'ẵ', 'a');
    SET @UnsignName = REPLACE(REPLACE(@UnsignName, N'ặ', 'a'), N'â', 'a');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ấ', 'a'), N'ầ', 'a'), N'ẩ', 'a'), N'ẫ', 'a'), N'ậ', 'a');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'đ', 'd'), N'é', 'e'), N'è', 'e'), N'ẻ', 'e'), N'ẽ', 'e');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ẹ', 'e'), N'ê', 'e'), N'ế', 'e'), N'ề', 'e'), N'ể', 'e'), N'ễ', 'e');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ệ', 'e'), N'í', 'i'), N'ì', 'i'), N'ỉ', 'i'), N'ĩ', 'i'), N'ị', 'i');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ó', 'o'), N'ò', 'o'), N'ỏ', 'o'), N'õ', 'o'), N'ọ', 'o');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ô', 'o'), N'ố', 'o'), N'ồ', 'o'), N'ổ', 'o'), N'ỗ', 'o'), N'ộ', 'o');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ơ', 'o'), N'ớ', 'o'), N'ờ', 'o'), N'ở', 'o'), N'ỡ', 'o'), N'ợ', 'o');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'u', 'u'), N'ú', 'u'), N'ù', 'u'), N'ủ', 'u'), N'ũ', 'u');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ụ', 'u'), N'ư', 'u'), N'ứ', 'u'), N'ừ', 'u'), N'ử', 'u'), N'ữ', 'u');
    SET @UnsignName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@UnsignName, N'ự', 'u'), N'ý', 'y'), N'ỳ', 'y'), N'ỷ', 'y'), N'ỹ', 'y'), N'ỵ', 'y');
    
    -- Remove special chars
    SET @UnsignName = REPLACE(@UnsignName, N'-', '');
    SET @UnsignName = REPLACE(@UnsignName, N'.', '');
    
    -- Trim spaces logic to extract Name and Surname
    SET @UnsignName = LTRIM(RTRIM(@UnsignName));
    
    -- Extract First Name (Last word) and Surname (First word)
    DECLARE @FirstName VARCHAR(50);
    DECLARE @Surname VARCHAR(50);
    DECLARE @FirstSpace INT = CHARINDEX(' ', @UnsignName);
    DECLARE @LastSpace INT = LEN(@UnsignName) - CHARINDEX(' ', REVERSE(@UnsignName)) + 1;
    
    IF @FirstSpace = 0
    BEGIN
        -- Single name
        SET @BaseUser = @UnsignName;
    END
    ELSE
    BEGIN
        SET @Surname = SUBSTRING(@UnsignName, 1, @FirstSpace - 1);
        SET @FirstName = SUBSTRING(@UnsignName, @LastSpace + 1, LEN(@UnsignName) - @LastSpace);
        SET @BaseUser = @FirstName + @Surname;
    END
    
    -- Generate Unique
    SET @Counter = 1;
    SET @FinalUser = @BaseUser;
    
    WHILE 1=1
    BEGIN
        -- Check duplicate in Candidate (exclude self) AND Employee
        SET @Exists = 0;
        IF EXISTS (SELECT 1 FROM Candidate WHERE UserName = @FinalUser AND Id <> @CandidateId) SET @Exists = 1;
        IF EXISTS (SELECT 1 FROM Employees WHERE UserName = @FinalUser) SET @Exists = 1;
        
        IF @Exists = 0 BREAK;
        
        SET @Counter = @Counter + 1;
        SET @FinalUser = @BaseUser + CAST(@Counter AS VARCHAR(10));
    END
    
    -- Update
    UPDATE Candidate
    SET UserName = @FinalUser
    WHERE Id = @CandidateId;

    FETCH NEXT FROM cur INTO @CandidateId, @FullName, @Email, @Phone;
END

CLOSE cur;
DEALLOCATE cur;

PRINT 'Refined Usernames for candidates.';

COMMIT TRANSACTION;

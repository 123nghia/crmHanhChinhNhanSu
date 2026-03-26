-- =============================================
-- Migration: V013__Recreate_BHXHItem_TaxItem
-- Author: System
-- Date: 2026-01-06
-- Description: Drop and recreate BHXHItem and TaxItem tables based on current models
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V013: Recreate BHXHItem and TaxItem...';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_BHXHItem_getAll...';
    DROP PROCEDURE [dbo].[sp_BHXHItem_getAll];
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_BHXHItem_insert...';
    DROP PROCEDURE [dbo].[sp_BHXHItem_insert];
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_BHXHItem_update]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_BHXHItem_update...';
    DROP PROCEDURE [dbo].[sp_BHXHItem_update];
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_getAll]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_tax_getAll...';
    DROP PROCEDURE [dbo].[sp_tax_getAll];
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_tax_insert...';
    DROP PROCEDURE [dbo].[sp_tax_insert];
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_udpate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_tax_udpate...';
    DROP PROCEDURE [dbo].[sp_tax_udpate];
END

IF OBJECT_ID(N'[dbo].[BHXHItem]', N'U') IS NOT NULL
BEGIN
    PRINT '  Dropping table BHXHItem...';
    DROP TABLE [dbo].[BHXHItem];
END

IF OBJECT_ID(N'[dbo].[TaxItem]', N'U') IS NOT NULL
BEGIN
    PRINT '  Dropping table TaxItem...';
    DROP TABLE [dbo].[TaxItem];
END

PRINT '  Creating table BHXHItem...';
EXEC(N'
CREATE TABLE [dbo].[BHXHItem](
    [UserName] [varchar](8) NULL,
    [NumberCode] [nvarchar](50) NULL,
    [Relid] [nvarchar](50) NULL,
    [PITDate] [datetime] NULL,
    [EffectedFrom] [datetime] NULL,
    [RegBHYT] [nvarchar](150) NULL,
    [Number] [int] NULL,
    [RegPageNumber] [nvarchar](50) NULL,
    [Deleted] [bit] NULL,
    [IsActive] [bit] NULL,
    [CreatedBy] [int] NULL,
    [UpdatedBy] [int] NULL,
    [CreateAt] [datetime] NULL,
    [UpdateAt] [datetime] NULL,
    [Id] [int] IDENTITY(1,1) NOT NULL,
    CONSTRAINT [PK_BHXHItem] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
');

PRINT '  Creating table TaxItem...';
EXEC(N'
CREATE TABLE [dbo].[TaxItem](
    [UserName] [varchar](8) NULL,
    [CodeId] [nvarchar](50) NULL,
    [Number] [nvarchar](50) NULL,
    [ChungTuThue] [nvarchar](255) NULL,
    [PageTax] [int] NULL,
    [BiaSo] [int] NULL,
    [DependentName] [nvarchar](255) NULL,
    [Dependent] [nvarchar](50) NULL,
    [IsConfirmletter] [bit] NULL,
    [PITDate] [datetime] NULL,
    [EffectedFrom] [datetime] NULL,
    [Deleted] [bit] NULL,
    [IsActive] [bit] NULL,
    [CreatedBy] [int] NULL,
    [UpdatedBy] [int] NULL,
    [CreateAt] [datetime] NULL,
    [UpdateAt] [datetime] NULL,
    [Id] [int] IDENTITY(1,1) NOT NULL,
    CONSTRAINT [PK_TaxItem] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
');

PRINT '  Creating sp_BHXHItem_getAll...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_BHXHItem_getAll]
(
    @UserName varchar(8) = ''''
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM [dbo].[BHXHItem] WHERE [UserName] = @UserName;
END
');

PRINT '  Creating sp_BHXHItem_insert...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_BHXHItem_insert]
    @UserName NVARCHAR(100),
    @NumberCode NVARCHAR(50) = NULL,
    @Relid NVARCHAR(50) = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL,
    @RegBHYT NVARCHAR(150) = NULL,
    @Number INT = NULL,
    @RegPageNumber NVARCHAR(50) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[BHXHItem] (
        [UserName],
        [NumberCode],
        [Relid],
        [PITDate],
        [EffectedFrom],
        [RegBHYT],
        [Number],
        [RegPageNumber],
        [CreatedBy]
    )
    VALUES (
        @UserName,
        @NumberCode,
        @Relid,
        @PITDate,
        @EffectedFrom,
        @RegBHYT,
        @Number,
        @RegPageNumber,
        @CreatedBy
    );
END
');

PRINT '  Creating sp_BHXHItem_update...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_BHXHItem_update]
    @Id INT,
    @NumberCode NVARCHAR(50) = NULL,
    @Relid NVARCHAR(50) = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL,
    @RegBHYT NVARCHAR(150) = NULL,
    @Number INT = NULL,
    @RegPageNumber NVARCHAR(50) = NULL,
    @UpdatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[BHXHItem]
    SET [NumberCode] = @NumberCode,
        [Relid] = @Relid,
        [PITDate] = @PITDate,
        [EffectedFrom] = @EffectedFrom,
        [RegBHYT] = @RegBHYT,
        [Number] = @Number,
        [RegPageNumber] = @RegPageNumber,
        [UpdatedBy] = @UpdatedBy
    WHERE [Id] = @Id;
END
');

PRINT '  Creating sp_tax_getAll...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_tax_getAll]
(
    @userid varchar(8) = ''''
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM [dbo].[TaxItem] WHERE [UserName] = @userid ORDER BY [Id] DESC;
END
');

PRINT '  Creating sp_tax_insert...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_tax_insert]
    @UserName NVARCHAR(100),
    @CodeId NVARCHAR(50) = NULL,
    @Number NVARCHAR(50) = NULL,
    @ChungTuThue NVARCHAR(255) = NULL,
    @PageTax INT = NULL,
    @BiaSo INT = NULL,
    @DependentName NVARCHAR(255) = NULL,
    @Dependent NVARCHAR(50) = NULL,
    @IsConfirmletter BIT = NULL,
    @CreatedBy INT = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[TaxItem] (
        [UserName],
        [CodeId],
        [Number],
        [ChungTuThue],
        [PageTax],
        [BiaSo],
        [DependentName],
        [Dependent],
        [IsConfirmletter],
        [CreatedBy],
        [PITDate],
        [EffectedFrom]
    )
    VALUES (
        @UserName,
        @CodeId,
        @Number,
        @ChungTuThue,
        @PageTax,
        @BiaSo,
        @DependentName,
        @Dependent,
        @IsConfirmletter,
        @CreatedBy,
        @PITDate,
        @EffectedFrom
    );
END
');

PRINT '  Creating sp_tax_udpate...';
EXEC(N'
CREATE PROCEDURE [dbo].[sp_tax_udpate]
    @Id INT,
    @UserName NVARCHAR(100),
    @CodeId NVARCHAR(50) = NULL,
    @Number NVARCHAR(50) = NULL,
    @ChungTuThue NVARCHAR(255) = NULL,
    @PageTax INT = NULL,
    @BiaSo INT = NULL,
    @DependentName NVARCHAR(255) = NULL,
    @Dependent NVARCHAR(50) = NULL,
    @IsConfirmletter BIT = NULL,
    @UpdatedBy INT = NULL,
    @PITDate DATETIME = NULL,
    @EffectedFrom DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[TaxItem]
    SET [UserName] = @UserName,
        [CodeId] = @CodeId,
        [Number] = @Number,
        [ChungTuThue] = @ChungTuThue,
        [PageTax] = @PageTax,
        [BiaSo] = @BiaSo,
        [DependentName] = @DependentName,
        [Dependent] = @Dependent,
        [IsConfirmletter] = @IsConfirmletter,
        [UpdatedBy] = @UpdatedBy,
        [PITDate] = @PITDate,
        [EffectedFrom] = @EffectedFrom
    WHERE [Id] = @Id;
END
');

PRINT 'Migration V013 completed successfully';

COMMIT TRANSACTION;

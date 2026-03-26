-- =============================================
-- Migration: V014__Add_TaxItem_DependentCode
-- Author: System
-- Date: 2026-01-06
-- Description: Add DependentCode column to TaxItem and update related SPs
-- =============================================

BEGIN TRANSACTION;

PRINT 'Applying migration V014: Add DependentCode to TaxItem...';

IF COL_LENGTH('dbo.TaxItem', 'DependentCode') IS NULL
BEGIN
    PRINT '  Adding column DependentCode to TaxItem...';
    ALTER TABLE [dbo].[TaxItem] ADD [DependentCode] NVARCHAR(50) NULL;
END
ELSE
BEGIN
    PRINT '  Column DependentCode already exists, skipping';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_insert]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_tax_insert...';
    DROP PROCEDURE [dbo].[sp_tax_insert];
END

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
    @DependentCode NVARCHAR(50) = NULL,
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
        [DependentCode],
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
        @DependentCode,
        @IsConfirmletter,
        @CreatedBy,
        @PITDate,
        @EffectedFrom
    );
END
');

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_tax_udpate]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  Dropping sp_tax_udpate...';
    DROP PROCEDURE [dbo].[sp_tax_udpate];
END

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
    @DependentCode NVARCHAR(50) = NULL,
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
        [DependentCode] = @DependentCode,
        [IsConfirmletter] = @IsConfirmletter,
        [UpdatedBy] = @UpdatedBy,
        [PITDate] = @PITDate,
        [EffectedFrom] = @EffectedFrom
    WHERE [Id] = @Id;
END
');

PRINT 'Migration V014 completed successfully';

COMMIT TRANSACTION;

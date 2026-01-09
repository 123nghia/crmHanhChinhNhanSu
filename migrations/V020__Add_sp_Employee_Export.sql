-- =============================================
-- Migration: V020__Add_sp_Employee_Export
-- Author: System
-- Date: 2026-01-08
-- Description: Tạo stored procedure sp_Employee_Export để xuất dữ liệu ra Excel
--              Dựa trên sp_Employee_getAll_Extended nhưng bỏ phân trang và thêm join HDLD, Tax, BHXH
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_Employee_Export]') AND type IN (N'P', N'PC'))
BEGIN
    PRINT '  → Dropping existing procedure sp_Employee_Export...';
    DROP PROCEDURE [dbo].[sp_Employee_Export];
END
GO

PRINT '  → Creating procedure sp_Employee_Export...';
GO

CREATE PROCEDURE [dbo].[sp_Employee_Export]
(
    @Token nvarchar(255) = '',      
    @OrderBy nvarchar(255) = '',    
    @UserId int = null,
    @MemberId int = null,
    @GroupId int = null,
    @Status int = -1,
    @StatusWork nvarchar(50) = null,
    @DocumentStatus nvarchar(50) = null,
    @fromDate DATETIME2 = null,        
    @toDate DATETIME2 = null,
    @IsDeleted bit = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @where nvarchar(max) = ' WHERE 1=1 ';
    DECLARE @mainClause nvarchar(max);
    DECLARE @params nvarchar(2000);

    -- Base Select
    SET @mainClause = ' 
    SELECT 
        d.*,
        dbo.getDisplayMasterData(d.Status) AS StatusText,
        dbo.getDisplayMasterData(d.StatusWork) AS StatusWorkText,
        dbo.getDisplayMasterData(d.DocumentStatus) AS DocumentStatusText,
        dbo.getDisplayMasterdata(d.DepartmentCode) AS DepartmentText,
        dbo.getDisplayMasterdata(d.PositionCode) AS PositionText,
        dbo.getDisplayMasterdata(d.EducationLevel) AS EducationLevelText,
        dbo.getDisplayMasterdata(d.Maritalstatus) AS MaritalstatusText,
        dbo.getDisplayMasterdata(d.Religion) AS ReligionText,
        CONVERT(varchar(10), d.Dob, 103) AS DobDisplay,
        CONVERT(varchar(10), d.Onboard, 103) AS OnboardDisplay,
        CONVERT(varchar(10), d.NationalDate, 103) AS NationalDateDisplay,
        gm.GroupId,
        g.Name AS GroupName,
        
        -- HDLD Info
        hdld.NoAgree AS HD_SoHD,
        hdld.Start AS HD_NgayBatDau,
        hdld.End AS HD_NgayKetThuc,
        hdld.CodeId AS HD_LoaiHD,
        
        -- Tax Info
        tax.Number AS Tax_MST,
        tax.PITDate AS Tax_NgayCap,
        tax.EffectedFrom AS Tax_NgayHieuLuc,
        tax.Dependent AS Tax_NguoiPhuThuoc,
        
        -- BHXH Info
        bhxh.NumberCode AS BHXH_SoSo,
        bhxh.RegBHYT AS BHXH_NoiDangKy

    FROM Employees d 
    LEFT JOIN GroupMember gm ON d.Id = gm.MemberId AND ISNULL(gm.Deleted, 0) = 0
    LEFT JOIN [Group] g ON gm.GroupId = g.Id AND ISNULL(g.Deleted, 0) = 0 
    
    -- Join HDLD (Latest)
    OUTER APPLY (
        SELECT TOP 1 * FROM hdldItem h 
        WHERE h.UserId = d.UserName AND ISNULL(h.Deleted, 0) = 0
        ORDER BY h.Start DESC, h.Id DESC
    ) hdld

    -- Join Tax (Latest)
    OUTER APPLY (
        SELECT TOP 1 * FROM TaxItem t 
        WHERE t.UserName = d.UserName AND ISNULL(t.Deleted, 0) = 0
        ORDER BY t.Id DESC
    ) tax

    -- Join BHXH (Latest)
    OUTER APPLY (
        SELECT TOP 1 * FROM BHXHItem b 
        WHERE b.UserName = d.UserName AND ISNULL(b.Deleted, 0) = 0
        ORDER BY b.Id DESC
    ) bhxh
    ';

    -- Filters
    IF (@Token IS NOT NULL AND @Token <> '')       
    BEGIN
        SET @where += ' AND (d.UserName LIKE N''%'' + @Token + ''%'' OR d.FullName LIKE N''%'' + @Token + ''%'' OR d.Phone LIKE N''%'' + @Token + ''%'') ';
    END

    IF (@GroupId > 0)
    BEGIN
        SET @where += ' AND gm.GroupId = @GroupId ';
    END

    IF (@IsDeleted = 0)
    BEGIN
        SET @where += ' AND ISNULL(d.Deleted, 0) = 0 ';
    END

    IF (@Status > -1)
    BEGIN
        SET @where += ' AND d.IsActive = @Status ';
    END

    IF (@StatusWork IS NOT NULL AND @StatusWork <> '' AND @StatusWork <> '-1')
    BEGIN
        SET @where += ' AND d.StatusWork = @StatusWork ';
    END

    IF (@DocumentStatus IS NOT NULL AND @DocumentStatus <> '' AND @DocumentStatus <> '-1')
    BEGIN
        SET @where += ' AND d.DocumentStatus = @DocumentStatus ';
    END

    IF (@fromDate IS NOT NULL)        
    BEGIN 
        SET @where += ' AND d.CreateAt >= @fromDate ';
    END

    IF (@toDate IS NOT NULL)
    BEGIN
        SET @where += ' AND d.CreateAt <= @toDate ';
    END

    IF (@UserId > 0)
    BEGIN
        SET @where += ' AND d.Id IN (SELECT Id FROM getAllUserByUserId(@UserId)) ';
    END

    -- Sorting
    SET @where += ' ORDER BY ';
    IF (@OrderBy IS NOT NULL AND @OrderBy <> '')
    BEGIN
        SET @where += @OrderBy;
    END
    ELSE
    BEGIN
        SET @where += ' d.UpdateAt DESC ';
    END

    -- No Pagination for Export

    SET @mainClause = @mainClause + @where;

    SET @params = N' @fromDate DATETIME2, @toDate DATETIME2, @Status int, @UserId int, @Token nvarchar(255), @GroupId int, @StatusWork nvarchar(50), @DocumentStatus nvarchar(50) ';

    EXECUTE sp_executesql @mainClause, @params, 
        @fromDate = @fromDate, 
        @toDate = @toDate, 
        @Status = @Status, 
        @UserId = @UserId,
        @Token = @Token,
        @GroupId = @GroupId,
        @StatusWork = @StatusWork,
        @DocumentStatus = @DocumentStatus;
END
GO

-- Add Cloud Storage and Sharing support to DocumentData
ALTER TABLE [dbo].[DocumentData] ADD [ParentId] [int] NULL;
ALTER TABLE [dbo].[DocumentData] ADD [IsFolder] [bit] NULL DEFAULT 0;
ALTER TABLE [dbo].[DocumentData] ADD [AccessLevel] [int] NULL DEFAULT 0; -- 0-Private, 1-Internal, 2-Restricted
ALTER TABLE [dbo].[DocumentData] ADD [ShareToken] [nvarchar](50) NULL;

-- Create DocumentShares table for Restricted sharing
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentShares]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[DocumentShares](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [DocumentId] [int] NOT NULL,
        [UserId] [int] NOT NULL,
        [CreateAt] [datetime] NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )
    ) ON [PRIMARY];
END

GO

-- Update sp_DocumentData_getAll to support folder navigation and access control
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_getAll]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_getAll];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_getAll]
(
    @Token nvarchar(30) = '',
    @OrderBy varchar(30) = '',
    @RelId int = -1,
    @RelCode varchar(10) = '',
    @DataType int = 0,
    @ParentId int = -1,
    @Page int = 1,
    @Limit int = 1000,
    @CurrentUserId int = -1
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @offset int = (@Page - 1) * @Limit;

    SELECT count(d.id) over() as TotalRecord,
           d.*,
           u.FullName as AuthorName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    WHERE (ISNULL(d.Deleted, 0) = 0)
      AND (@RelId = -1 OR d.RelId = @RelId)
      AND (@RelCode = '' OR d.RelCode = @RelCode)
      AND (@DataType = 0 OR d.dataType = @DataType)
      AND (
          @ParentId = -2 -- Special value for "all shared with me" or something
          OR d.ParentId = (CASE WHEN @ParentId = -1 THEN NULL ELSE @ParentId END)
      )
      AND (
          d.CreatedBy = @CurrentUserId -- Owner always has access
          OR d.AccessLevel = 1 -- Internal (everyone)
          OR (d.AccessLevel = 2 AND EXISTS (SELECT 1 FROM DocumentShares s WHERE s.DocumentId = d.Id AND s.UserId = @CurrentUserId)) -- Restricted
      )
    ORDER BY d.IsFolder DESC, d.UpdateAt DESC, d.CreateAt DESC
    OFFSET @offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

-- Create sp_DocumentData_GetById
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_GetById]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_GetById];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_GetById]
(
    @Id int
)
AS
BEGIN
    SELECT d.*, u.FullName as AuthorName
    FROM DocumentData d
    LEFT JOIN Employees u ON d.CreatedBy = u.Id
    WHERE d.Id = @Id;
END
GO

-- Update sp_DocumentData_insert
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_insert]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_insert];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_insert]
(
    @RelId int,
    @RelCode varchar(10),
    @DisplayText nvarchar(500),
    @ValueFile nvarchar(1000),
    @Code varchar(10),
    @DataType int,
    @ParentId int,
    @IsFolder bit,
    @AccessLevel int,
    @CreatedBy int
)
AS
BEGIN
    INSERT INTO [dbo].[DocumentData]
    (
        [RelId], [RelCode], [DisplayText], [ValueFile], [Code], [dataType], 
        [ParentId], [IsFolder], [AccessLevel], [CreatedBy], [CreateAt], [UpdateAt], [IsActive], [Deleted]
    )
    VALUES
    (
        @RelId, @RelCode, @DisplayText, @ValueFile, @Code, @DataType,
        (CASE WHEN @ParentId <= 0 THEN NULL ELSE @ParentId END), @IsFolder, @AccessLevel, @CreatedBy, GETDATE(), GETDATE(), 1, 0
    );
    SELECT SCOPE_IDENTITY();
END
GO

-- Update sp_DocumentData_update
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_update];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_update]
(
    @Id int,
    @DisplayText nvarchar(500),
    @ValueFile nvarchar(1000),
    @ParentId int,
    @AccessLevel int,
    @UpdatedBy int
)
AS
BEGIN
    UPDATE [dbo].[DocumentData]
    SET [DisplayText] = @DisplayText,
        [ValueFile] = @ValueFile,
        [ParentId] = (CASE WHEN @ParentId <= 0 THEN NULL ELSE @ParentId END),
        [AccessLevel] = @AccessLevel,
        [UpdatedBy] = @UpdatedBy,
        [UpdateAt] = GETDATE()
    WHERE [Id] = @Id;
END
GO

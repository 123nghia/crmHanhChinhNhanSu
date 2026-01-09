-- Update sp_DocumentData_update to include more fields
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_DocumentData_update]') AND type IN (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_DocumentData_update];
GO

CREATE PROCEDURE [dbo].[sp_DocumentData_update]
(
    @Id int,
    @DisplayText nvarchar(500) = NULL,
    @ValueFile nvarchar(1000) = NULL,
    @ParentId int = NULL,
    @AccessLevel int = 0,
    @Code varchar(10) = NULL,
    @DataType int = NULL,
    @UpdatedBy int = NULL
)
AS
BEGIN
    UPDATE [dbo].[DocumentData]
    SET [DisplayText] = ISNULL(@DisplayText, [DisplayText]),
        [ValueFile] = ISNULL(@ValueFile, [ValueFile]),
        [ParentId] = (CASE WHEN @ParentId <= 0 THEN NULL ELSE ISNULL(@ParentId, [ParentId]) END),
        [AccessLevel] = ISNULL(@AccessLevel, [AccessLevel]),
        [Code] = ISNULL(@Code, [Code]),
        [dataType] = ISNULL(@DataType, [dataType]),
        [UpdatedBy] = @UpdatedBy,
        [UpdateAt] = GETDATE()
    WHERE [Id] = @Id;
END
GO

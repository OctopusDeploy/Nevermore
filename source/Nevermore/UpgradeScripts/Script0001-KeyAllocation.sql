IF NOT EXISTS (SELECT NULL FROM sys.tables WHERE name = 'KeyAllocation' AND SCHEMA_NAME(schema_id) = 'dbo')
	CREATE TABLE dbo.KeyAllocation
	(
		CollectionName nvarchar(50) constraint PK_KeyAllocation_CollectionName primary key,
		Allocated bigint not null
	)
GO

IF EXISTS (SELECT NULL FROM sys.procedures WHERE name = 'GetNextKeyBlock' AND SCHEMA_NAME(schema_id) = 'dbo')
	DROP PROCEDURE dbo.GetNextKeyBlock
GO

CREATE PROCEDURE dbo.GetNextKeyBlock
(
	@collectionName nvarchar(50),
	@blockSize int
)
AS
BEGIN
	SET NOCOUNT ON
	DECLARE @result bigint

	UPDATE KeyAllocation
		SET @result = Allocated = (Allocated + @blockSize)
		WHERE CollectionName = @collectionName

	if (@@ROWCOUNT = 0)
	begin
		INSERT INTO KeyAllocation (CollectionName, Allocated) values (@collectionName, @blockSize)
		SELECT CAST(@blockSize as bigint) -- type must be the same as @result
	end

	SELECT @result
END
GO
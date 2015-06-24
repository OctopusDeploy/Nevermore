CREATE TABLE KeyAllocation
(
	CollectionName nvarchar(50) constraint PK_KeyAllocation_CollectionName primary key,
	Allocated int not null
)
GO

CREATE PROCEDURE GetNextKeyBlock 
(
	@collectionName nvarchar(50),
	@blockSize int
)
AS
BEGIN
	SET NOCOUNT ON
	DECLARE @result int
	
	UPDATE KeyAllocation
		SET @result = Allocated = (Allocated + @blocksize)
		WHERE CollectionName = @collectionName
	
	if (@@ROWCOUNT = 0)
	begin
		INSERT INTO KeyAllocation (CollectionName, Allocated) values (@collectionName, @blockSize)
		SELECT @blockSize
	end

	SELECT @result
END
GO
CREATE PROCEDURE [dbo].[ConnectionState_Heartbeat]
	@EntityId NVARCHAR(50),
	@EntityIdUnique NVARCHAR(50)
AS
BEGIN
	UPDATE 
		[ConnectionState] 
	SET 
		[Timestamp] = SYSUTCDATETIME()
	WHERE
		EntityId = @EntityId AND
		EntityIdUnique = @EntityIdUnique
END
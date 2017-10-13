CREATE PROCEDURE [dbo].[ConnectionState_RemoveConnection]
	@EntityId NVARCHAR(50),
	@EntityIdUnique NVARCHAR(50)
AS
BEGIN
	DELETE FROM 
		[ConnectionState] 
	WHERE 
		EntityId = @EntityId AND
		EntityIdUnique = @EntityIdUnique
END

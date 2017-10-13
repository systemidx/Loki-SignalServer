CREATE PROCEDURE [dbo].[ConnectionState_AddConnection]
	@EntityId			nvarchar(50),
	@EntityIdUnique		nvarchar(50)
AS
BEGIN
	INSERT INTO [ConnectionState] (EntityId, EntityIdUnique) VALUES (@EntityId, @EntityIdUnique)
END
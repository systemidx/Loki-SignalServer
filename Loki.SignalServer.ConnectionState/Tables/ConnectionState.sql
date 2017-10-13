CREATE TABLE [dbo].[ConnectionState]
(
	[EntityId] NVARCHAR(50) NOT NULL,
	[EntityIdUnique] NVARCHAR(50) NOT NULL,
	[Timestamp] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()

    CONSTRAINT [PK_ConnectionState] PRIMARY KEY ([EntityId], [EntityIdUnique]),     
)

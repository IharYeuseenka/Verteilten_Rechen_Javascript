UPDATE [Entities]
SET [IsLocked] = 0
WHERE [IsLocked] = 1
AND DATEDIFF(MINUTE,GETDATE(),[LockTime]) > 10;

CREATE TABLE  [Loader](
     [ID]        INTEGER
    ,[Text]      NVARCHAR(255)
    ,[Hash]      NVARCHAR(32)
    ,[IsLocked]  BIT
    ,[LockTime]  DATETIME
);

INSERT INTO [Loader]
SELECT TOP 100 * 
FROM [Entities] 
WHERE [IsLocked] = 0
AND [Hash] IS NULL;

UPDATE [Entities]
SET  [IsLocked] = 1
    ,[LockTime] = GETDATE()
WHERE [ID] IN (SELECT [ID] FROM [Loader]);

SELECT [Text] FROM [Loader];

DROP TABLE  [Loader];
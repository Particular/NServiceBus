DECLARE @MessageID uniqueidentifier = NULL -- All messages by default

DECLARE @FailedQSearchString VARCHAR(MAX) = '"NServiceBus.FailedQ":"'
DECLARE @ToReturn table (Id uniqueidentifier NOT NULL PRIMARY KEY, FailedQ sysname)

INSERT INTO @ToReturn
SELECT
	Id,
	SUBSTRING(Headers, FailedQStart, CHARINDEX('"', Headers, FailedQStart) - FailedQStart) FailedQ
FROM (
	SELECT
		Id,
		Headers,
		CHARINDEX(@FailedQSearchString, Headers) + LEN(@FailedQSearchString) FailedQStart
	FROM dbo.error
	WHERE
		(@MessageID IS NULL OR Id = @MessageID) AND
		CHARINDEX(@FailedQSearchString, Headers) > 0 AND
		CHARINDEX(@FailedQSearchString, Headers) < (LEN(Headers) - CHARINDEX('"', REVERSE(Headers)))
) Matches

-- Need to execute dynamic sql to account for any failed q table name.
-- Each row could be a different q, so need to use a cursor.
DECLARE @CurrentId uniqueidentifier, @CurrentFailedQ sysname
DECLARE @ReturnSql NVARCHAR(MAX)
DECLARE ToReturnCursor CURSOR LOCAL FAST_FORWARD
FOR SELECT Id, FailedQ FROM @ToReturn

OPEN ToReturnCursor

FETCH NEXT FROM ToReturnCursor
INTO @CurrentId, @CurrentFailedQ

WHILE @@FETCH_STATUS = 0
BEGIN
	IF OBJECT_ID('dbo.[' + @CurrentFailedQ + ']') IS NULL
		PRINT 'WARNING: Ignoring bad FailedQ ' + @CurrentFailedQ
	ELSE
	BEGIN
		SET @ReturnSql = N'
			DELETE FROM dbo.error
			OUTPUT deleted.Id, deleted.CorrelationId, deleted.ReplyToAddress, deleted.Recoverable, deleted.Expires, deleted.Headers, deleted.Body
			INTO dbo.[' + @CurrentFailedQ + N']
			WHERE Id = @Id
		'
		
		EXEC sp_executesql @ReturnSql, N'@Id uniqueidentifier', @Id = @CurrentId
	END
	
	FETCH NEXT FROM ToReturnCursor
	INTO @CurrentId, @CurrentFailedQ
END

CLOSE ToReturnCursor
DEALLOCATE ToReturnCursor

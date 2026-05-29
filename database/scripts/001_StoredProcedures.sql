-- Run against your SOPS database after EF schema is created.
-- Azure SQL Database / SQL Server

IF TYPE_ID(N'dbo.OrderTableType') IS NULL
BEGIN
    CREATE TYPE dbo.OrderTableType AS TABLE
    (
        ExternalId  UNIQUEIDENTIFIER NOT NULL,
        CustomerId  UNIQUEIDENTIFIER NOT NULL,
        TotalAmount DECIMAL(18,2)    NOT NULL,
        Status      NVARCHAR(50)     NOT NULL,
        CreatedAt   DATETIME2(7)     NOT NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_EnqueueOrder
    @OrderId   UNIQUEIDENTIFIER,
    @Payload   NVARCHAR(MAX),
    @CreatedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.OutboxOrders (OrderId, Payload, Status, CreatedAt, RetryCount)
    VALUES (@OrderId, @Payload, N'Pending', @CreatedAt, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_DequeueOrders
    @BatchSize INT = 500
AS
BEGIN
    SET NOCOUNT ON;
    WITH CTE AS (
        SELECT TOP (@BatchSize) Id, OrderId, Payload
        FROM   dbo.OutboxOrders WITH (UPDLOCK, READPAST)
        WHERE  Status = N'Pending'
          AND  (NextRetryAt IS NULL OR NextRetryAt <= SYSUTCDATETIME())
        ORDER  BY CreatedAt ASC
    )
    UPDATE CTE SET Status = N'Processing'
    OUTPUT INSERTED.Id, INSERTED.OrderId, INSERTED.Payload;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_BulkInsertOrders
    @Orders dbo.OrderTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    MERGE dbo.Orders AS target
    USING @Orders AS source
        ON target.ExternalId = source.ExternalId
    WHEN NOT MATCHED THEN
        INSERT (Id, ExternalId, CustomerId, TotalAmount, Status, CreatedAt)
        VALUES (NEWID(), source.ExternalId, source.CustomerId,
                source.TotalAmount, source.Status, source.CreatedAt);
    SELECT @@ROWCOUNT AS InsertedCount;
END
GO

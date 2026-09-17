-- WEC ERP initial SQL Server schema
-- Run against the ERP database after creating the database.
-- The application supplies GUIDs and timestamps; defaults are included for direct SQL safety.

IF OBJECT_ID(N'dbo.Items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Items
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Items PRIMARY KEY,
        Sku nvarchar(100) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Type int NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Items_IsActive DEFAULT (1),
        CONSTRAINT UQ_Items_Sku UNIQUE (Sku)
    );
END;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(300) NOT NULL,
        Phone nvarchar(50) NOT NULL,
        Email nvarchar(320) NOT NULL,
        TaxNumber nvarchar(50) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
        CreatedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_Customers_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Customers_Code UNIQUE (Code)
    );
END;
GO

IF OBJECT_ID(N'dbo.Quotations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quotations
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Quotations PRIMARY KEY,
        Number nvarchar(60) NOT NULL,
        CustomerId uniqueidentifier NOT NULL,
        Status int NOT NULL,
        CurrencyCode nvarchar(3) NOT NULL,
        Subtotal decimal(19,4) NOT NULL,
        DiscountAmount decimal(19,4) NOT NULL,
        TaxAmount decimal(19,4) NOT NULL,
        Total decimal(19,4) NOT NULL,
        ValidUntil datetimeoffset(7) NULL,
        Notes nvarchar(max) NOT NULL,
        CreatedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_Quotations_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Quotations_Number UNIQUE (Number),
        CONSTRAINT FK_Quotations_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.QuotationLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuotationLines
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_QuotationLines PRIMARY KEY,
        QuotationId uniqueidentifier NOT NULL,
        ItemId uniqueidentifier NOT NULL,
        Description nvarchar(500) NOT NULL,
        Quantity decimal(19,4) NOT NULL,
        UnitPrice decimal(19,4) NOT NULL,
        DiscountPercent decimal(9,4) NOT NULL,
        TaxPercent decimal(9,4) NOT NULL,
        LineSubtotal decimal(19,4) NOT NULL,
        LineDiscount decimal(19,4) NOT NULL,
        LineTax decimal(19,4) NOT NULL,
        LineTotal decimal(19,4) NOT NULL,
        CONSTRAINT FK_QuotationLines_Quotations_QuotationId FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations (Id) ON DELETE CASCADE,
        CONSTRAINT FK_QuotationLines_Items_ItemId FOREIGN KEY (ItemId) REFERENCES dbo.Items (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.SalesOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrders
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_SalesOrders PRIMARY KEY,
        Number nvarchar(60) NOT NULL,
        CustomerId uniqueidentifier NOT NULL,
        SourceQuotationId uniqueidentifier NULL,
        Status int NOT NULL,
        CurrencyCode nvarchar(3) NOT NULL,
        Subtotal decimal(19,4) NOT NULL,
        DiscountAmount decimal(19,4) NOT NULL,
        TaxAmount decimal(19,4) NOT NULL,
        Total decimal(19,4) NOT NULL,
        CreatedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_SalesOrders_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_SalesOrders_Number UNIQUE (Number),
        CONSTRAINT FK_SalesOrders_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_SalesOrders_Quotations_SourceQuotationId FOREIGN KEY (SourceQuotationId) REFERENCES dbo.Quotations (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesOrders_SourceQuotationId' AND object_id = OBJECT_ID(N'dbo.SalesOrders'))
BEGIN
    CREATE UNIQUE INDEX IX_SalesOrders_SourceQuotationId
        ON dbo.SalesOrders (SourceQuotationId)
        WHERE SourceQuotationId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.SalesOrderLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrderLines
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_SalesOrderLines PRIMARY KEY,
        SalesOrderId uniqueidentifier NOT NULL,
        ItemId uniqueidentifier NOT NULL,
        Description nvarchar(500) NOT NULL,
        Quantity decimal(19,4) NOT NULL,
        UnitPrice decimal(19,4) NOT NULL,
        DiscountPercent decimal(9,4) NOT NULL,
        TaxPercent decimal(9,4) NOT NULL,
        LineSubtotal decimal(19,4) NOT NULL,
        LineDiscount decimal(19,4) NOT NULL,
        LineTax decimal(19,4) NOT NULL,
        LineTotal decimal(19,4) NOT NULL,
        CONSTRAINT FK_SalesOrderLines_SalesOrders_SalesOrderId FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrders (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SalesOrderLines_Items_ItemId FOREIGN KEY (ItemId) REFERENCES dbo.Items (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Resources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resources
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Resources PRIMARY KEY,
        Code nvarchar(50) NOT NULL,
        Name nvarchar(200) NOT NULL,
        Type int NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Resources_IsActive DEFAULT (1),
        CONSTRAINT UQ_Resources_Code UNIQUE (Code)
    );
END;
GO

IF OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bookings
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
        Number nvarchar(60) NOT NULL,
        CustomerId uniqueidentifier NOT NULL,
        ItemId uniqueidentifier NOT NULL,
        ResourceId uniqueidentifier NULL,
        StartsUtc datetimeoffset(7) NOT NULL,
        EndsUtc datetimeoffset(7) NOT NULL,
        Status int NOT NULL,
        Notes nvarchar(max) NOT NULL,
        CreatedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_Bookings_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Bookings_Number UNIQUE (Number),
        CONSTRAINT FK_Bookings_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_Bookings_Items_ItemId FOREIGN KEY (ItemId) REFERENCES dbo.Items (Id),
        CONSTRAINT FK_Bookings_Resources_ResourceId FOREIGN KEY (ResourceId) REFERENCES dbo.Resources (Id)
    );
END;
GO

-- Useful operational indexes for the current API queries.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Quotations_CreatedUtc' AND object_id = OBJECT_ID(N'dbo.Quotations'))
    CREATE INDEX IX_Quotations_CreatedUtc ON dbo.Quotations (CreatedUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesOrders_CreatedUtc' AND object_id = OBJECT_ID(N'dbo.SalesOrders'))
    CREATE INDEX IX_SalesOrders_CreatedUtc ON dbo.SalesOrders (CreatedUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_ResourceTime' AND object_id = OBJECT_ID(N'dbo.Bookings'))
    CREATE INDEX IX_Bookings_ResourceTime ON dbo.Bookings (ResourceId, StartsUtc, EndsUtc);
GO

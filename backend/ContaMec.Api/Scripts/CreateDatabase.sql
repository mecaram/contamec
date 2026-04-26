/*
  ContaMec - SQL Server: creación de base y esquema (dbo), alineado con ContaMecDbContext / entidades actuales.

  Ajustar @DatabaseName si no usas "ContaMec".

  sqlcmd:  sqlcmd -S <SERVIDOR> -E -i CreateDatabase.sql
  SSMS:   abrir y ejecutar (F5).

  Para bases ya existentes con esquema antiguo, ejecutar después: UpdateDatabase.sql
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DatabaseName sysname = N'ContaMec';

IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName) + N';';
    EXEC (@sql);
END
GO

USE [ContaMec];
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        [Id]            INT            NOT NULL IDENTITY(1, 1),
        [Name]          VARCHAR(50)    NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        [Id]            INT            NOT NULL IDENTITY(1, 1),
        [Name]          VARCHAR(80)    NOT NULL,
        [PasswordHash]  VARCHAR(256)   NOT NULL,
        [IsActive]      BIT            NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [UserRoleId]    INT            NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Users_UserRoles] FOREIGN KEY ([UserRoleId])
            REFERENCES dbo.UserRoles ([Id])
            ON DELETE SET NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Name]
        ON dbo.Users ([Name] ASC);
END
GO

IF OBJECT_ID(N'dbo.BalanceTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BalanceTypes
    (
        [Id]            INT            NOT NULL IDENTITY(1, 1),
        [Name]          VARCHAR(50)    NOT NULL,
        CONSTRAINT [PK_BalanceTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.Cuentas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cuentas
    (
        [Id]            INT            NOT NULL IDENTITY(1, 1),
        [Name]          VARCHAR(50)    NULL,
        [Type]          VARCHAR(50)    NULL,
        CONSTRAINT [PK_Cuentas] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.Cierres', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cierres
    (
        [Id]            INT            NOT NULL IDENTITY(1, 1),
        [OpenDate]      DATETIME       NULL,
        [CloseDate]     DATETIME       NULL,
        [IsClosed]      BIT            NULL,
        [Result]        MONEY          NULL,
        CONSTRAINT [PK_Cierres] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.ClosureBalances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClosureBalances
    (
        [Id]            INT            NOT NULL IDENTITY(1, 1),
        [ClosureId]     INT            NOT NULL,
        [BalanceTypeId] INT            NOT NULL,
        [Amount]        MONEY          NULL,
        CONSTRAINT [PK_ClosureBalances] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ClosureBalances_Cierres] FOREIGN KEY ([ClosureId])
            REFERENCES dbo.Cierres ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ClosureBalances_BalanceTypes] FOREIGN KEY ([BalanceTypeId])
            REFERENCES dbo.BalanceTypes ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.Ingresos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ingresos
    (
        [Id]               INT         NOT NULL IDENTITY(1, 1),
        [ClosureId]        INT         NULL,
        [EmissionDate]     DATETIME    NULL,
        [AccountId]        INT         NULL,
        [Detail]           VARCHAR(80) NULL,
        [Amount]           MONEY       NULL,
        [CreatedByUserId]  INT         NULL,
        [CreatedAt]        DATETIME    NULL,
        CONSTRAINT [PK_Ingresos] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Ingresos_Cierres] FOREIGN KEY ([ClosureId])
            REFERENCES dbo.Cierres ([Id]),
        CONSTRAINT [FK_Ingresos_Cuentas] FOREIGN KEY ([AccountId])
            REFERENCES dbo.Cuentas ([Id]),
        CONSTRAINT [FK_Ingresos_Users_CreatedBy] FOREIGN KEY ([CreatedByUserId])
            REFERENCES dbo.Users ([Id])
    );
END
GO

IF OBJECT_ID(N'dbo.Egresos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Egresos
    (
        [Id]               INT         NOT NULL IDENTITY(1, 1),
        [ClosureId]        INT         NULL,
        [EmissionDate]     DATETIME    NULL,
        [AccountId]        INT         NULL,
        [Detail]           VARCHAR(80) NULL,
        [Amount]           MONEY       NULL,
        [CreatedByUserId]  INT         NULL,
        [CreatedAt]        DATETIME    NULL,
        CONSTRAINT [PK_Egresos] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Egresos_Cierres] FOREIGN KEY ([ClosureId])
            REFERENCES dbo.Cierres ([Id]),
        CONSTRAINT [FK_Egresos_Cuentas] FOREIGN KEY ([AccountId])
            REFERENCES dbo.Cuentas ([Id]),
        CONSTRAINT [FK_Egresos_Users_CreatedBy] FOREIGN KEY ([CreatedByUserId])
            REFERENCES dbo.Users ([Id])
    );
END
GO

PRINT N'CreateDatabase: base y tablas listas (esquema actual).';
GO

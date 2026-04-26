/*
  ContaMec - SQL Server: actualizar una base EXISTENTE en el servidor al esquema actual (ContaMecDbContext).

  Idempotente: se puede ejecutar varias veces; solo aplica cambios pendientes.

  Orden recomendado:
    1) Copia de seguridad de la base.
    2) Ejecutar este script en SSMS o sqlcmd contra la instancia correcta.

  Ajustar el nombre de la base si no es ContaMec.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [ContaMec];
GO

/* ---- Users: Username -> Name (índice único) ---- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'Username') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'Name') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'IX_Users_Username')
        DROP INDEX [IX_Users_Username] ON dbo.Users;

    EXEC sys.sp_rename N'dbo.Users.Username', N'Name', N'COLUMN';

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Name] ON dbo.Users ([Name] ASC);
END
GO

/* ---- UserRoles: RoleName -> Name ---- */
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.UserRoles', N'RoleName') IS NOT NULL
   AND COL_LENGTH(N'dbo.UserRoles', N'Name') IS NULL
BEGIN
    EXEC sys.sp_rename N'dbo.UserRoles.RoleName', N'Name', N'COLUMN';
END
GO

/* ---- Users: UserId -> Id ---- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'UserId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'Id') IS NULL
BEGIN
    EXEC sys.sp_rename N'dbo.Users.UserId', N'Id', N'COLUMN';
END
GO

/* ---- BalanceTypes: BalanceTypeId -> Id ---- */
IF OBJECT_ID(N'dbo.BalanceTypes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.BalanceTypes', N'BalanceTypeId') IS NOT NULL
   AND COL_LENGTH(N'dbo.BalanceTypes', N'Id') IS NULL
BEGIN
    EXEC sys.sp_rename N'dbo.BalanceTypes.BalanceTypeId', N'Id', N'COLUMN';
END
GO

/* ---- Cuentas: AccountId -> Id, AccountName -> Name, AccountType -> Type ---- */
IF OBJECT_ID(N'dbo.Cuentas', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cuentas', N'AccountId') IS NOT NULL AND COL_LENGTH(N'dbo.Cuentas', N'Id') IS NULL
        EXEC sys.sp_rename N'dbo.Cuentas.AccountId', N'Id', N'COLUMN';

    IF COL_LENGTH(N'dbo.Cuentas', N'AccountName') IS NOT NULL AND COL_LENGTH(N'dbo.Cuentas', N'Name') IS NULL
        EXEC sys.sp_rename N'dbo.Cuentas.AccountName', N'Name', N'COLUMN';

    IF COL_LENGTH(N'dbo.Cuentas', N'AccountType') IS NOT NULL AND COL_LENGTH(N'dbo.Cuentas', N'Type') IS NULL
        EXEC sys.sp_rename N'dbo.Cuentas.AccountType', N'Type', N'COLUMN';
END
GO

/* ---- Cierres: ClosureId -> Id; FixedFund -> Result ---- */
IF OBJECT_ID(N'dbo.Cierres', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cierres', N'ClosureId') IS NOT NULL AND COL_LENGTH(N'dbo.Cierres', N'Id') IS NULL
        EXEC sys.sp_rename N'dbo.Cierres.ClosureId', N'Id', N'COLUMN';

    IF COL_LENGTH(N'dbo.Cierres', N'Result') IS NULL
        ALTER TABLE dbo.Cierres ADD [Result] MONEY NULL;

    IF COL_LENGTH(N'dbo.Cierres', N'FixedFund') IS NOT NULL
    BEGIN
        UPDATE dbo.Cierres
        SET [Result] = [FixedFund]
        WHERE [Result] IS NULL AND [FixedFund] IS NOT NULL;

        ALTER TABLE dbo.Cierres DROP COLUMN [FixedFund];
    END
END
GO

/* ---- ClosureBalances: BalanceId -> Id ---- */
IF OBJECT_ID(N'dbo.ClosureBalances', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ClosureBalances', N'BalanceId') IS NOT NULL
   AND COL_LENGTH(N'dbo.ClosureBalances', N'Id') IS NULL
BEGIN
    EXEC sys.sp_rename N'dbo.ClosureBalances.BalanceId', N'Id', N'COLUMN';
END
GO

/* ---- Ingresos: IncomeId -> Id; Date -> EmissionDate ---- */
IF OBJECT_ID(N'dbo.Ingresos', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ingresos', N'IncomeId') IS NOT NULL AND COL_LENGTH(N'dbo.Ingresos', N'Id') IS NULL
        EXEC sys.sp_rename N'dbo.Ingresos.IncomeId', N'Id', N'COLUMN';

    IF COL_LENGTH(N'dbo.Ingresos', N'Date') IS NOT NULL AND COL_LENGTH(N'dbo.Ingresos', N'EmissionDate') IS NULL
        EXEC sys.sp_rename N'dbo.Ingresos.Date', N'EmissionDate', N'COLUMN';
END
GO

/* ---- Egresos: ExpenseId -> Id; Date -> EmissionDate ---- */
IF OBJECT_ID(N'dbo.Egresos', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Egresos', N'ExpenseId') IS NOT NULL AND COL_LENGTH(N'dbo.Egresos', N'Id') IS NULL
        EXEC sys.sp_rename N'dbo.Egresos.ExpenseId', N'Id', N'COLUMN';

    IF COL_LENGTH(N'dbo.Egresos', N'Date') IS NOT NULL AND COL_LENGTH(N'dbo.Egresos', N'EmissionDate') IS NULL
        EXEC sys.sp_rename N'dbo.Egresos.Date', N'EmissionDate', N'COLUMN';
END
GO

/* ---- Ingresos / Egresos: columnas de auditoría si faltan ---- */
IF OBJECT_ID(N'dbo.Ingresos', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Ingresos', N'CreatedByUserId') IS NULL
        ALTER TABLE dbo.Ingresos ADD [CreatedByUserId] INT NULL;

    IF COL_LENGTH(N'dbo.Ingresos', N'CreatedAt') IS NULL
        ALTER TABLE dbo.Ingresos ADD [CreatedAt] DATETIME NULL;
END
GO

IF OBJECT_ID(N'dbo.Egresos', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Egresos', N'CreatedByUserId') IS NULL
        ALTER TABLE dbo.Egresos ADD [CreatedByUserId] INT NULL;

    IF COL_LENGTH(N'dbo.Egresos', N'CreatedAt') IS NULL
        ALTER TABLE dbo.Egresos ADD [CreatedAt] DATETIME NULL;
END
GO

PRINT N'UpdateDatabase: migración de columnas aplicada (o ya estaba al día).';
GO

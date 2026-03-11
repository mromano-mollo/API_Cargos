/*
    CARGOS - Database setup and sync.

    The procedure dbo.Cargos_Sync_Contratti_Frontiera expects source view:
      dbo.Cargos_Vista_Contratti

    Required identity columns:
      - ContractNo or [Contract No_] (legacy ContractId accepted)
      - ContractLineNo

    Optional internal metadata columns:
      - BranchId
      - BranchEmail

    Required mandatory CaRGOS columns:
      CONTRATTO_ID
      CONTRATTO_DATA
      CONTRATTO_TIPOP
      CONTRATTO_CHECKOUT_DATA
      CONTRATTO_CHECKOUT_LUOGO_COD
      CONTRATTO_CHECKOUT_INDIRIZZO
      CONTRATTO_CHECKIN_DATA
      CONTRATTO_CHECKIN_LUOGO_COD
      CONTRATTO_CHECKIN_INDIRIZZO
      OPERATORE_ID
      AGENZIA_ID
      AGENZIA_NOME
      AGENZIA_LUOGO_COD
      AGENZIA_INDIRIZZO
      AGENZIA_RECAPITO_TEL
      VEICOLO_TIPO
      VEICOLO_MARCA
      VEICOLO_MODELLO
      VEICOLO_TARGA
      CONDUCENTE_CONTRAENTE_COGNOME
      CONDUCENTE_CONTRAENTE_NOME
      CONDUCENTE_CONTRAENTE_NASCITA_DATA
      CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD
      CONDUCENTE_CONTRAENTE_CITTADINANZA_COD
      CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD
      CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO
      CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD
      CONDUCENTE_CONTRAENTE_PATENTE_NUMERO
      CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD

    Optional line payload:
      RecordLine or CargosRecordLine
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Cargos_Contratti', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Contratti
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Contratti PRIMARY KEY,
        ContractNo NVARCHAR(50) NOT NULL,
        [ContractLineNo] BIGINT NOT NULL,
        CargosContractId NVARCHAR(50) NOT NULL,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        ContrattoId NVARCHAR(50) NULL,
        ContrattoData DATETIME2(0) NULL,
        ContrattoTipoP NVARCHAR(1) NULL,
        ContrattoCheckoutData DATETIME2(0) NULL,
        ContrattoCheckoutLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckoutIndirizzo NVARCHAR(150) NULL,
        ContrattoCheckinData DATETIME2(0) NULL,
        ContrattoCheckinLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckinIndirizzo NVARCHAR(150) NULL,
        OperatoreId NVARCHAR(50) NULL,
        AgenziaId NVARCHAR(30) NULL,
        AgenziaNome NVARCHAR(70) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(150) NULL,
        AgenziaRecapitoTel NVARCHAR(20) NULL,
        VeicoloTipo NVARCHAR(2) NULL,
        VeicoloMarca NVARCHAR(50) NULL,
        VeicoloModello NVARCHAR(100) NULL,
        VeicoloTarga NVARCHAR(15) NULL,
        ConducenteContraenteCognome NVARCHAR(50) NULL,
        ConducenteContraenteNome NVARCHAR(38) NULL,
        ConducenteContraenteNascitaData DATETIME2(0) NULL,
        ConducenteContraenteNascitaLuogoCod NVARCHAR(9) NULL,
        ConducenteContraenteCittadinanzaCod NVARCHAR(9) NULL,
        ConducenteContraenteDocideTipoCod NVARCHAR(5) NULL,
        ConducenteContraenteDocideNumero NVARCHAR(20) NULL,
        ConducenteContraenteDocideLuogorilCod NVARCHAR(9) NULL,
        ConducenteContraentePatenteNumero NVARCHAR(20) NULL,
        ConducenteContraentePatenteLuogorilCod NVARCHAR(9) NULL,
        RecordLine NVARCHAR(2000) NULL,
        DateFingerprint NVARCHAR(128) NOT NULL,
        PayloadFingerprint NVARCHAR(128) NOT NULL,
        DataFingerprint NVARCHAR(128) NOT NULL,
        LastQueuedFingerprint NVARCHAR(128) NULL,
        LastQueuedAt DATETIME2 NULL,
        LastSeenAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );

    ALTER TABLE dbo.Cargos_Contratti
        ADD CONSTRAINT UQ_Cargos_Contratti_ContractLine UNIQUE (ContractNo, [ContractLineNo]);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Agenzie
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Agenzie PRIMARY KEY,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        AgenziaId NVARCHAR(50) NOT NULL,
        AgenziaNome NVARCHAR(70) NOT NULL,
        AgenziaLuogoValue NVARCHAR(9) NOT NULL,
        AgenziaCity NVARCHAR(100) NULL,
        AgenziaCounty NVARCHAR(10) NULL,
        AgenziaPostCode NVARCHAR(20) NULL,
        AgenziaIndirizzo NVARCHAR(150) NOT NULL,
        AgenziaRecapitoTel NVARCHAR(20) NOT NULL,
        PayloadFingerprint NVARCHAR(128) NOT NULL,
        LastQueuedFingerprint NVARCHAR(128) NULL,
        LastQueuedAt DATETIME2 NULL,
        LastSeenAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );

    ALTER TABLE dbo.Cargos_Agenzie
        ADD CONSTRAINT UQ_Cargos_Agenzie_Branch UNIQUE (BranchId);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie_Frontiera', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Agenzie_Frontiera
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Agenzie_Frontiera PRIMARY KEY,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        AgenziaId NVARCHAR(50) NOT NULL,
        AgenziaNome NVARCHAR(70) NOT NULL,
        AgenziaLuogoValue NVARCHAR(9) NOT NULL,
        AgenziaCity NVARCHAR(100) NULL,
        AgenziaCounty NVARCHAR(10) NULL,
        AgenziaPostCode NVARCHAR(20) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(150) NOT NULL,
        AgenziaRecapitoTel NVARCHAR(20) NOT NULL,
        Reason NVARCHAR(30) NOT NULL,
        SnapshotHash NVARCHAR(128) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        LastError NVARCHAR(MAX) NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AttemptCount DEFAULT (0),
        LastAttemptAt DATETIME2 NULL,
        NextRetryAt DATETIME2 NULL,
        ClaimedBy NVARCHAR(100) NULL,
        ClaimedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'BranchId') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD BranchId NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Agenzie_BranchId DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'BranchEmail') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD BranchEmail NVARCHAR(255) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaId') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaId NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaId DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaId') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaId') <> 100
        ALTER TABLE dbo.Cargos_Agenzie ALTER COLUMN AgenziaId NVARCHAR(50) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaNome') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaNome NVARCHAR(70) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaNome DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaNome') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaNome') < 140
        ALTER TABLE dbo.Cargos_Agenzie ALTER COLUMN AgenziaNome NVARCHAR(70) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaLuogoValue') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaLuogoValue NVARCHAR(9) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaLuogoValue DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaLuogoValue') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaLuogoValue') <> 18
        ALTER TABLE dbo.Cargos_Agenzie ALTER COLUMN AgenziaLuogoValue NVARCHAR(9) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaCity') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaCity NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaCounty') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaCounty NVARCHAR(10) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaPostCode') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaPostCode NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaIndirizzo') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaIndirizzo NVARCHAR(150) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaIndirizzo DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaIndirizzo') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Agenzie ALTER COLUMN AgenziaIndirizzo NVARCHAR(150) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaRecapitoTel') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaRecapitoTel NVARCHAR(20) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaRecapitoTel DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaRecapitoTel') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaRecapitoTel') < 40
        ALTER TABLE dbo.Cargos_Agenzie ALTER COLUMN AgenziaRecapitoTel NVARCHAR(20) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'PayloadFingerprint') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD PayloadFingerprint NVARCHAR(128) NOT NULL CONSTRAINT DF_Cargos_Agenzie_PayloadFingerprint DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'LastQueuedFingerprint') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD LastQueuedFingerprint NVARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'LastQueuedAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD LastQueuedAt DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'LastSeenAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD LastSeenAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Agenzie_LastSeenAt DEFAULT (SYSUTCDATETIME());
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'CreatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Agenzie_CreatedAt DEFAULT (SYSUTCDATETIME());
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Agenzie_UpdatedAt DEFAULT (SYSUTCDATETIME());

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE [type] = 'UQ'
          AND [name] = N'UQ_Cargos_Agenzie_Branch'
          AND parent_object_id = OBJECT_ID(N'dbo.Cargos_Agenzie')
    )
        ALTER TABLE dbo.Cargos_Agenzie
            ADD CONSTRAINT UQ_Cargos_Agenzie_Branch UNIQUE (BranchId);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie_Frontiera', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'BranchId') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD BranchId NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_BranchId DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'BranchEmail') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD BranchEmail NVARCHAR(255) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaId') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaId NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaId DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaId') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaId') <> 100
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ALTER COLUMN AgenziaId NVARCHAR(50) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaNome') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaNome NVARCHAR(70) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaNome DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaNome') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaNome') < 140
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ALTER COLUMN AgenziaNome NVARCHAR(70) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaLuogoValue') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaLuogoValue NVARCHAR(9) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaLuogoValue DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaLuogoValue') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaLuogoValue') <> 18
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ALTER COLUMN AgenziaLuogoValue NVARCHAR(9) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaCity') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaCity NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaCounty') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaCounty NVARCHAR(10) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaPostCode') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaPostCode NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaLuogoCod') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaLuogoCod NVARCHAR(9) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaIndirizzo') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaIndirizzo NVARCHAR(150) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaIndirizzo DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaIndirizzo') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ALTER COLUMN AgenziaIndirizzo NVARCHAR(150) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaRecapitoTel') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaRecapitoTel NVARCHAR(20) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaRecapitoTel DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaRecapitoTel') IS NOT NULL
       AND COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaRecapitoTel') < 40
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ALTER COLUMN AgenziaRecapitoTel NVARCHAR(20) NOT NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'Reason') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD Reason NVARCHAR(30) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_Reason DEFAULT (N'INITIAL_LOAD');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'SnapshotHash') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD SnapshotHash NVARCHAR(128) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_SnapshotHash DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'Status') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_Status DEFAULT (N'PENDING');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'LastError') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD LastError NVARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AttemptCount') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AttemptCount INT NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AttemptCount_Migrate DEFAULT (0);
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'LastAttemptAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD LastAttemptAt DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'NextRetryAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD NextRetryAt DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'ClaimedBy') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD ClaimedBy NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'ClaimedAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD ClaimedAt DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'CreatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_CreatedAt DEFAULT (SYSUTCDATETIME());
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_UpdatedAt DEFAULT (SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie_Frontiera', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UQ_Cargos_Agenzie_Frontiera_Snapshot'
          AND object_id = OBJECT_ID(N'dbo.Cargos_Agenzie_Frontiera')
    )
        DROP INDEX UQ_Cargos_Agenzie_Frontiera_Snapshot ON dbo.Cargos_Agenzie_Frontiera;

    CREATE UNIQUE INDEX UQ_Cargos_Agenzie_Frontiera_Snapshot
    ON dbo.Cargos_Agenzie_Frontiera (BranchId, SnapshotHash)
    WHERE SnapshotHash IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie_Frontiera', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_Cargos_Agenzie_Frontiera_StatusRetry'
         AND object_id = OBJECT_ID(N'dbo.Cargos_Agenzie_Frontiera')
   )
BEGIN
    CREATE INDEX IX_Cargos_Agenzie_Frontiera_StatusRetry
    ON dbo.Cargos_Agenzie_Frontiera (Status, NextRetryAt, CreatedAt);
END;
GO

CREATE OR ALTER PROCEDURE dbo.Cargos_Sync_Agenzie_Frontiera
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.Cargos_Vista_Agenzie', N'V') IS NULL
        THROW 50101, 'View dbo.Cargos_Vista_Agenzie not found.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'BranchId') IS NULL
        THROW 50102, 'View dbo.Cargos_Vista_Agenzie must expose BranchId.', 1;

    DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();
    DECLARE @BranchEmailExpression NVARCHAR(256) = N'NULL';
    DECLARE @AgenziaIdExpression NVARCHAR(256);
    DECLARE @AgenziaNomeExpression NVARCHAR(256);
    DECLARE @AgenziaLuogoValueExpression NVARCHAR(256);
    DECLARE @AgenziaCityExpression NVARCHAR(256) = N'NULL';
    DECLARE @AgenziaCountyExpression NVARCHAR(256) = N'NULL';
    DECLARE @AgenziaPostCodeExpression NVARCHAR(256) = N'NULL';
    DECLARE @AgenziaIndirizzoExpression NVARCHAR(256);
    DECLARE @AgenziaRecapitoTelExpression NVARCHAR(256);

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'BranchEmail') IS NOT NULL
        SET @BranchEmailExpression = N'CAST(v.BranchEmail AS NVARCHAR(255))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaId') IS NOT NULL
        SET @AgenziaIdExpression = N'CAST(v.AgenziaId AS NVARCHAR(50))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_ID') IS NOT NULL
        SET @AgenziaIdExpression = N'CAST(v.AGENZIA_ID AS NVARCHAR(50))';
    ELSE
        THROW 50103, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaId or AGENZIA_ID.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaNome') IS NOT NULL
        SET @AgenziaNomeExpression = N'CAST(v.AgenziaNome AS NVARCHAR(70))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_NOME') IS NOT NULL
        SET @AgenziaNomeExpression = N'CAST(v.AGENZIA_NOME AS NVARCHAR(70))';
    ELSE
        THROW 50104, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaNome or AGENZIA_NOME.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaLuogoValue') IS NOT NULL
        SET @AgenziaLuogoValueExpression = N'CAST(v.AgenziaLuogoValue AS NVARCHAR(9))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaLuogoCod') IS NOT NULL
        SET @AgenziaLuogoValueExpression = N'CAST(v.AgenziaLuogoCod AS NVARCHAR(9))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_LUOGO_COD') IS NOT NULL
        SET @AgenziaLuogoValueExpression = N'CAST(v.AGENZIA_LUOGO_COD AS NVARCHAR(9))';
    ELSE
        THROW 50105, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaLuogoValue/AgenziaLuogoCod or AGENZIA_LUOGO_COD.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaCity') IS NOT NULL
        SET @AgenziaCityExpression = N'CAST(v.AgenziaCity AS NVARCHAR(100))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaCounty') IS NOT NULL
        SET @AgenziaCountyExpression = N'CAST(v.AgenziaCounty AS NVARCHAR(10))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaPostCode') IS NOT NULL
        SET @AgenziaPostCodeExpression = N'CAST(v.AgenziaPostCode AS NVARCHAR(20))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaIndirizzo') IS NOT NULL
        SET @AgenziaIndirizzoExpression = N'CAST(v.AgenziaIndirizzo AS NVARCHAR(150))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_INDIRIZZO') IS NOT NULL
        SET @AgenziaIndirizzoExpression = N'CAST(v.AGENZIA_INDIRIZZO AS NVARCHAR(150))';
    ELSE
        THROW 50106, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaIndirizzo or AGENZIA_INDIRIZZO.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaRecapitoTel') IS NOT NULL
        SET @AgenziaRecapitoTelExpression = N'CAST(v.AgenziaRecapitoTel AS NVARCHAR(20))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_RECAPITO_TEL') IS NOT NULL
        SET @AgenziaRecapitoTelExpression = N'CAST(v.AGENZIA_RECAPITO_TEL AS NVARCHAR(20))';
    ELSE
        THROW 50107, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaRecapitoTel or AGENZIA_RECAPITO_TEL.', 1;

    DECLARE @Queued TABLE
    (
        BranchId NVARCHAR(50) NOT NULL,
        SnapshotHash NVARCHAR(128) NOT NULL
    );

    IF OBJECT_ID(N'tempdb..#SourceAgenzie') IS NOT NULL
        DROP TABLE #SourceAgenzie;

    CREATE TABLE #SourceAgenzie
    (
        BranchId NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
        BranchEmail NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        AgenziaId NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
        AgenziaNome NVARCHAR(70) COLLATE DATABASE_DEFAULT NOT NULL,
        AgenziaLuogoValue NVARCHAR(9) COLLATE DATABASE_DEFAULT NOT NULL,
        AgenziaCity NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
        AgenziaCounty NVARCHAR(10) COLLATE DATABASE_DEFAULT NULL,
        AgenziaPostCode NVARCHAR(20) COLLATE DATABASE_DEFAULT NULL,
        AgenziaIndirizzo NVARCHAR(150) COLLATE DATABASE_DEFAULT NOT NULL,
        AgenziaRecapitoTel NVARCHAR(20) COLLATE DATABASE_DEFAULT NOT NULL,
        PayloadFingerprint NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
        SnapshotHash NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
        QueueReason NVARCHAR(30) COLLATE DATABASE_DEFAULT NULL
    );

    DECLARE @Sql NVARCHAR(MAX) = N'
        INSERT INTO #SourceAgenzie
        (
            BranchId,
            BranchEmail,
            AgenziaId,
            AgenziaNome,
            AgenziaLuogoValue,
            AgenziaCity,
            AgenziaCounty,
            AgenziaPostCode,
            AgenziaIndirizzo,
            AgenziaRecapitoTel
        )
        SELECT
            CAST(v.BranchId AS NVARCHAR(50)),
            ' + @BranchEmailExpression + N',
            ' + @AgenziaIdExpression + N',
            ' + @AgenziaNomeExpression + N',
            ' + @AgenziaLuogoValueExpression + N',
            ' + @AgenziaCityExpression + N',
            ' + @AgenziaCountyExpression + N',
            ' + @AgenziaPostCodeExpression + N',
            ' + @AgenziaIndirizzoExpression + N',
            ' + @AgenziaRecapitoTelExpression + N'
        FROM dbo.Cargos_Vista_Agenzie v;';

    EXEC sys.sp_executesql @Sql;

    UPDATE s
    SET
        s.PayloadFingerprint = CONVERT(
            NVARCHAR(128),
            HASHBYTES(
                'SHA2_256',
                CONCAT(
                    ISNULL(s.BranchEmail, ''), '|',
                    ISNULL(s.AgenziaId, ''), '|',
                    ISNULL(s.AgenziaNome, ''), '|',
                    ISNULL(s.AgenziaLuogoValue, ''), '|',
                    ISNULL(s.AgenziaCity, ''), '|',
                    ISNULL(s.AgenziaCounty, ''), '|',
                    ISNULL(s.AgenziaPostCode, ''), '|',
                    ISNULL(s.AgenziaIndirizzo, ''), '|',
                    ISNULL(s.AgenziaRecapitoTel, '')
                )
            ),
            2
        ),
        s.SnapshotHash = CONVERT(
            NVARCHAR(128),
            HASHBYTES(
                'SHA2_256',
                CONCAT(ISNULL(s.BranchId, ''), '|', ISNULL(s.PayloadFingerprint, ''))
            ),
            2
        )
    FROM #SourceAgenzie s;

    UPDATE s
    SET
        s.QueueReason =
            CASE
                WHEN a.BranchId IS NULL OR a.LastQueuedFingerprint IS NULL THEN 'INITIAL_LOAD'
                WHEN ISNULL(a.LastQueuedFingerprint, '') <> ISNULL(s.SnapshotHash, '') THEN 'DATA_CHANGE'
                ELSE NULL
            END
    FROM #SourceAgenzie s
    LEFT JOIN dbo.Cargos_Agenzie a
        ON a.BranchId = s.BranchId;

    MERGE dbo.Cargos_Agenzie AS tgt
    USING #SourceAgenzie AS src
        ON tgt.BranchId = src.BranchId
    WHEN MATCHED THEN
        UPDATE SET
            tgt.BranchEmail = src.BranchEmail,
            tgt.AgenziaId = src.AgenziaId,
            tgt.AgenziaNome = src.AgenziaNome,
            tgt.AgenziaLuogoValue = src.AgenziaLuogoValue,
            tgt.AgenziaCity = src.AgenziaCity,
            tgt.AgenziaCounty = src.AgenziaCounty,
            tgt.AgenziaPostCode = src.AgenziaPostCode,
            tgt.AgenziaIndirizzo = src.AgenziaIndirizzo,
            tgt.AgenziaRecapitoTel = src.AgenziaRecapitoTel,
            tgt.PayloadFingerprint = src.PayloadFingerprint,
            tgt.LastSeenAt = @NowUtc,
            tgt.UpdatedAt = @NowUtc
    WHEN NOT MATCHED THEN
        INSERT
        (
            BranchId, BranchEmail, AgenziaId, AgenziaNome, AgenziaLuogoValue, AgenziaCity, AgenziaCounty, AgenziaPostCode, AgenziaIndirizzo, AgenziaRecapitoTel,
            PayloadFingerprint, LastQueuedFingerprint, LastQueuedAt, LastSeenAt, CreatedAt, UpdatedAt
        )
        VALUES
        (
            src.BranchId, src.BranchEmail, src.AgenziaId, src.AgenziaNome, src.AgenziaLuogoValue, src.AgenziaCity, src.AgenziaCounty, src.AgenziaPostCode, src.AgenziaIndirizzo, src.AgenziaRecapitoTel,
            src.PayloadFingerprint, NULL, NULL, @NowUtc, @NowUtc, @NowUtc
        );

    INSERT INTO dbo.Cargos_Agenzie_Frontiera
    (
        BranchId, BranchEmail, AgenziaId, AgenziaNome, AgenziaLuogoValue, AgenziaCity, AgenziaCounty, AgenziaPostCode, AgenziaLuogoCod, AgenziaIndirizzo, AgenziaRecapitoTel,
        Reason, SnapshotHash, Status, AttemptCount, CreatedAt, UpdatedAt
    )
    OUTPUT inserted.BranchId, inserted.SnapshotHash
        INTO @Queued (BranchId, SnapshotHash)
    SELECT
        s.BranchId, s.BranchEmail, s.AgenziaId, s.AgenziaNome, s.AgenziaLuogoValue, s.AgenziaCity, s.AgenziaCounty, s.AgenziaPostCode, NULL, s.AgenziaIndirizzo, s.AgenziaRecapitoTel,
        s.QueueReason, s.SnapshotHash, 'PENDING', 0, @NowUtc, @NowUtc
    FROM #SourceAgenzie s
    WHERE s.QueueReason IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Cargos_Agenzie_Frontiera f
          WHERE f.BranchId = s.BranchId
            AND f.SnapshotHash = s.SnapshotHash
      );

    UPDATE a
    SET
        a.LastQueuedFingerprint = q.SnapshotHash,
        a.LastQueuedAt = @NowUtc,
        a.UpdatedAt = @NowUtc
    FROM dbo.Cargos_Agenzie a
    INNER JOIN @Queued q
        ON q.BranchId = a.BranchId;

    SELECT QueuedItems = COUNT(1)
    FROM @Queued;
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Contratti_Frontiera', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Contratti_Frontiera
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Contratti_Frontiera PRIMARY KEY,
        ContractNo NVARCHAR(50) NOT NULL,
        [ContractLineNo] BIGINT NOT NULL,
        CargosContractId NVARCHAR(50) NOT NULL,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        ContrattoId NVARCHAR(50) NULL,
        ContrattoData DATETIME2(0) NULL,
        ContrattoTipoP NVARCHAR(1) NULL,
        ContrattoCheckoutData DATETIME2(0) NULL,
        ContrattoCheckoutLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckoutIndirizzo NVARCHAR(150) NULL,
        ContrattoCheckinData DATETIME2(0) NULL,
        ContrattoCheckinLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckinIndirizzo NVARCHAR(150) NULL,
        OperatoreId NVARCHAR(50) NULL,
        AgenziaId NVARCHAR(30) NULL,
        AgenziaNome NVARCHAR(70) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(150) NULL,
        AgenziaRecapitoTel NVARCHAR(20) NULL,
        VeicoloTipo NVARCHAR(2) NULL,
        VeicoloMarca NVARCHAR(50) NULL,
        VeicoloModello NVARCHAR(100) NULL,
        VeicoloTarga NVARCHAR(15) NULL,
        ConducenteContraenteCognome NVARCHAR(50) NULL,
        ConducenteContraenteNome NVARCHAR(38) NULL,
        ConducenteContraenteNascitaData DATETIME2(0) NULL,
        ConducenteContraenteNascitaLuogoCod NVARCHAR(9) NULL,
        ConducenteContraenteCittadinanzaCod NVARCHAR(9) NULL,
        ConducenteContraenteDocideTipoCod NVARCHAR(5) NULL,
        ConducenteContraenteDocideNumero NVARCHAR(20) NULL,
        ConducenteContraenteDocideLuogorilCod NVARCHAR(9) NULL,
        ConducenteContraentePatenteNumero NVARCHAR(20) NULL,
        ConducenteContraentePatenteLuogorilCod NVARCHAR(9) NULL,
        Reason NVARCHAR(30) NOT NULL,
        SnapshotHash NVARCHAR(128) NOT NULL,
        RecordLine NVARCHAR(2000) NULL,
        Status NVARCHAR(30) NOT NULL,
        MissingFields NVARCHAR(MAX) NULL,
        LastError NVARCHAR(MAX) NULL,
        TransactionId NVARCHAR(100) NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_AttemptCount DEFAULT (0),
        LastAttemptAt DATETIME2 NULL,
        NextRetryAt DATETIME2 NULL,
        ClaimedBy NVARCHAR(100) NULL,
        ClaimedAt DATETIME2 NULL,
        LastMissingEmailAt DATETIME2 NULL,
        LastMissingFieldsHash NVARCHAR(128) NULL,
        LastRejectEmailAt DATETIME2 NULL,
        LastRejectHash NVARCHAR(128) NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Tabella
    (
        TableId INT NOT NULL CONSTRAINT PK_Cargos_Tabella PRIMARY KEY,
        TableName NVARCHAR(50) NOT NULL,
        LastSyncedAt DATETIME2 NULL,
        LastSyncStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Cargos_Tabella_LastSyncStatus DEFAULT (N'NEVER'),
        LastSyncError NVARCHAR(MAX) NULL,
        [SyncedRowCount] INT NOT NULL CONSTRAINT DF_Cargos_Tabella_SyncedRowCount DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella_Righe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Tabella_Righe
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Tabella_Righe PRIMARY KEY,
        TableId INT NOT NULL,
        RowNumber INT NOT NULL,
        Code NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(255) NULL,
        Column3 NVARCHAR(255) NULL,
        Column4 NVARCHAR(255) NULL,
        Column5 NVARCHAR(255) NULL,
        Column6 NVARCHAR(255) NULL,
        Column7 NVARCHAR(255) NULL,
        Column8 NVARCHAR(255) NULL,
        RawLine NVARCHAR(2000) NOT NULL,
        SyncedAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );

    ALTER TABLE dbo.Cargos_Tabella_Righe
        ADD CONSTRAINT FK_Cargos_Tabella_Righe_Tabella
        FOREIGN KEY (TableId) REFERENCES dbo.Cargos_Tabella (TableId);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Contratti', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ContractNo') IS NULL
       AND COL_LENGTH(N'dbo.Cargos_Contratti', N'ContractId') IS NOT NULL
        EXEC sys.sp_rename N'dbo.Cargos_Contratti.ContractId', N'ContractNo', N'COLUMN';

    DECLARE @ContrattiColumns TABLE
    (
        ColumnName SYSNAME NOT NULL,
        ColumnDefinition NVARCHAR(400) NOT NULL
    );

    INSERT INTO @ContrattiColumns (ColumnName, ColumnDefinition)
    VALUES
        (N'ContractNo', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_ContractNo DEFAULT (N'''')'),
        (N'ContractLineNo', N'BIGINT NOT NULL CONSTRAINT DF_Cargos_Contratti_ContractLineNo DEFAULT (0)'),
        (N'CargosContractId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_CargosContractId DEFAULT (N'''')'),
        (N'BranchId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_BranchId DEFAULT (N'''')'),
        (N'BranchEmail', N'NVARCHAR(255) NULL'),
        (N'ContrattoId', N'NVARCHAR(50) NULL'),
        (N'ContrattoData', N'DATETIME2(0) NULL'),
        (N'ContrattoTipoP', N'NVARCHAR(1) NULL'),
        (N'ContrattoCheckoutData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckoutLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckoutIndirizzo', N'NVARCHAR(150) NULL'),
        (N'ContrattoCheckinData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckinLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckinIndirizzo', N'NVARCHAR(150) NULL'),
        (N'OperatoreId', N'NVARCHAR(50) NULL'),
        (N'AgenziaId', N'NVARCHAR(30) NULL'),
        (N'AgenziaNome', N'NVARCHAR(70) NULL'),
        (N'AgenziaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'AgenziaIndirizzo', N'NVARCHAR(150) NULL'),
        (N'AgenziaRecapitoTel', N'NVARCHAR(20) NULL'),
        (N'VeicoloTipo', N'NVARCHAR(2) NULL'),
        (N'VeicoloMarca', N'NVARCHAR(50) NULL'),
        (N'VeicoloModello', N'NVARCHAR(100) NULL'),
        (N'VeicoloTarga', N'NVARCHAR(15) NULL'),
        (N'ConducenteContraenteCognome', N'NVARCHAR(50) NULL'),
        (N'ConducenteContraenteNome', N'NVARCHAR(38) NULL'),
        (N'ConducenteContraenteNascitaData', N'DATETIME2(0) NULL'),
        (N'ConducenteContraenteNascitaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraenteCittadinanzaCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraenteDocideTipoCod', N'NVARCHAR(5) NULL'),
        (N'ConducenteContraenteDocideNumero', N'NVARCHAR(20) NULL'),
        (N'ConducenteContraenteDocideLuogorilCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraentePatenteNumero', N'NVARCHAR(20) NULL'),
        (N'ConducenteContraentePatenteLuogorilCod', N'NVARCHAR(9) NULL'),
        (N'RecordLine', N'NVARCHAR(2000) NULL'),
        (N'DateFingerprint', N'NVARCHAR(128) NOT NULL CONSTRAINT DF_Cargos_Contratti_DateFingerprint DEFAULT (N'''')'),
        (N'PayloadFingerprint', N'NVARCHAR(128) NOT NULL CONSTRAINT DF_Cargos_Contratti_PayloadFingerprint DEFAULT (N'''')'),
        (N'DataFingerprint', N'NVARCHAR(128) NOT NULL CONSTRAINT DF_Cargos_Contratti_DataFingerprint DEFAULT (N'''')'),
        (N'LastQueuedFingerprint', N'NVARCHAR(128) NULL'),
        (N'LastQueuedAt', N'DATETIME2 NULL'),
        (N'LastSeenAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Contratti_LastSeenAt DEFAULT (SYSUTCDATETIME())'),
        (N'CreatedAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Contratti_CreatedAt DEFAULT (SYSUTCDATETIME())'),
        (N'UpdatedAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Contratti_UpdatedAt DEFAULT (SYSUTCDATETIME())');

    DECLARE @SqlAddContratti NVARCHAR(MAX) = N'';
    SELECT @SqlAddContratti = @SqlAddContratti +
        N'IF COL_LENGTH(N''dbo.Cargos_Contratti'', N''' + c.ColumnName + N''') IS NULL ' +
        N'ALTER TABLE dbo.Cargos_Contratti ADD ' + QUOTENAME(c.ColumnName) + N' ' + c.ColumnDefinition + N';'
    FROM @ContrattiColumns c;

    EXEC sys.sp_executesql @SqlAddContratti;

    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ContrattoId') < 100
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN ContrattoId NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ContrattoCheckoutIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN ContrattoCheckoutIndirizzo NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ContrattoCheckinIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN ContrattoCheckinIndirizzo NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'OperatoreId') < 100
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN OperatoreId NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'AgenziaId') < 60
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN AgenziaId NVARCHAR(30) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'AgenziaNome') < 140
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN AgenziaNome NVARCHAR(70) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'AgenziaIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN AgenziaIndirizzo NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'AgenziaRecapitoTel') < 40
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN AgenziaRecapitoTel NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'VeicoloMarca') < 100
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN VeicoloMarca NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'VeicoloModello') < 200
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN VeicoloModello NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'VeicoloTarga') < 30
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN VeicoloTarga NVARCHAR(15) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ConducenteContraenteCognome') < 100
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN ConducenteContraenteCognome NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ConducenteContraenteCittadinanzaCod') < 18
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN ConducenteContraenteCittadinanzaCod NVARCHAR(9) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti', N'ConducenteContraenteDocideTipoCod') < 10
        ALTER TABLE dbo.Cargos_Contratti ALTER COLUMN ConducenteContraenteDocideTipoCod NVARCHAR(5) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE [type] = 'UQ'
          AND [name] = N'UQ_Cargos_Contratti_Contract'
          AND parent_object_id = OBJECT_ID(N'dbo.Cargos_Contratti')
    )
        ALTER TABLE dbo.Cargos_Contratti DROP CONSTRAINT UQ_Cargos_Contratti_Contract;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE [type] = 'UQ'
          AND [name] = N'UQ_Cargos_Contratti_ContractLine'
          AND parent_object_id = OBJECT_ID(N'dbo.Cargos_Contratti')
    )
        ALTER TABLE dbo.Cargos_Contratti
            ADD CONSTRAINT UQ_Cargos_Contratti_ContractLine UNIQUE (ContractNo, [ContractLineNo]);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Contratti_Frontiera', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ContractNo') IS NULL
       AND COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ContractId') IS NOT NULL
        EXEC sys.sp_rename N'dbo.Cargos_Contratti_Frontiera.ContractId', N'ContractNo', N'COLUMN';

    DECLARE @FrontieraColumns TABLE
    (
        ColumnName SYSNAME NOT NULL,
        ColumnDefinition NVARCHAR(400) NOT NULL
    );

    INSERT INTO @FrontieraColumns (ColumnName, ColumnDefinition)
    VALUES
        (N'ContractNo', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_ContractNo DEFAULT (N'''')'),
        (N'ContractLineNo', N'BIGINT NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_ContractLineNo DEFAULT (0)'),
        (N'CargosContractId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_CargosContractId DEFAULT (N'''')'),
        (N'BranchId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_BranchId DEFAULT (N'''')'),
        (N'BranchEmail', N'NVARCHAR(255) NULL'),
        (N'ContrattoId', N'NVARCHAR(50) NULL'),
        (N'ContrattoData', N'DATETIME2(0) NULL'),
        (N'ContrattoTipoP', N'NVARCHAR(1) NULL'),
        (N'ContrattoCheckoutData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckoutLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckoutIndirizzo', N'NVARCHAR(150) NULL'),
        (N'ContrattoCheckinData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckinLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckinIndirizzo', N'NVARCHAR(150) NULL'),
        (N'OperatoreId', N'NVARCHAR(50) NULL'),
        (N'AgenziaId', N'NVARCHAR(30) NULL'),
        (N'AgenziaNome', N'NVARCHAR(70) NULL'),
        (N'AgenziaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'AgenziaIndirizzo', N'NVARCHAR(150) NULL'),
        (N'AgenziaRecapitoTel', N'NVARCHAR(20) NULL'),
        (N'VeicoloTipo', N'NVARCHAR(2) NULL'),
        (N'VeicoloMarca', N'NVARCHAR(50) NULL'),
        (N'VeicoloModello', N'NVARCHAR(100) NULL'),
        (N'VeicoloTarga', N'NVARCHAR(15) NULL'),
        (N'ConducenteContraenteCognome', N'NVARCHAR(50) NULL'),
        (N'ConducenteContraenteNome', N'NVARCHAR(38) NULL'),
        (N'ConducenteContraenteNascitaData', N'DATETIME2(0) NULL'),
        (N'ConducenteContraenteNascitaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraenteCittadinanzaCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraenteDocideTipoCod', N'NVARCHAR(5) NULL'),
        (N'ConducenteContraenteDocideNumero', N'NVARCHAR(20) NULL'),
        (N'ConducenteContraenteDocideLuogorilCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraentePatenteNumero', N'NVARCHAR(20) NULL'),
        (N'ConducenteContraentePatenteLuogorilCod', N'NVARCHAR(9) NULL'),
        (N'Reason', N'NVARCHAR(30) NULL'),
        (N'SnapshotHash', N'NVARCHAR(128) NULL'),
        (N'RecordLine', N'NVARCHAR(2000) NULL'),
        (N'Status', N'NVARCHAR(30) NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_Status DEFAULT (N''PENDING'')'),
        (N'MissingFields', N'NVARCHAR(MAX) NULL'),
        (N'LastError', N'NVARCHAR(MAX) NULL'),
        (N'TransactionId', N'NVARCHAR(100) NULL'),
        (N'AttemptCount', N'INT NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_AttemptCount_Migrate DEFAULT (0)'),
        (N'LastAttemptAt', N'DATETIME2 NULL'),
        (N'NextRetryAt', N'DATETIME2 NULL'),
        (N'ClaimedBy', N'NVARCHAR(100) NULL'),
        (N'ClaimedAt', N'DATETIME2 NULL'),
        (N'LastMissingEmailAt', N'DATETIME2 NULL'),
        (N'LastMissingFieldsHash', N'NVARCHAR(128) NULL'),
        (N'LastRejectEmailAt', N'DATETIME2 NULL'),
        (N'LastRejectHash', N'NVARCHAR(128) NULL'),
        (N'CreatedAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_CreatedAt DEFAULT (SYSUTCDATETIME())'),
        (N'UpdatedAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_UpdatedAt DEFAULT (SYSUTCDATETIME())');

    DECLARE @SqlAddFrontiera NVARCHAR(MAX) = N'';
    SELECT @SqlAddFrontiera = @SqlAddFrontiera +
        N'IF COL_LENGTH(N''dbo.Cargos_Contratti_Frontiera'', N''' + c.ColumnName + N''') IS NULL ' +
        N'ALTER TABLE dbo.Cargos_Contratti_Frontiera ADD ' + QUOTENAME(c.ColumnName) + N' ' + c.ColumnDefinition + N';'
    FROM @FrontieraColumns c;

    EXEC sys.sp_executesql @SqlAddFrontiera;

    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ContrattoId') < 100
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN ContrattoId NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ContrattoCheckoutIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN ContrattoCheckoutIndirizzo NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ContrattoCheckinIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN ContrattoCheckinIndirizzo NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'OperatoreId') < 100
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN OperatoreId NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'AgenziaId') < 60
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN AgenziaId NVARCHAR(30) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'AgenziaNome') < 140
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN AgenziaNome NVARCHAR(70) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'AgenziaIndirizzo') < 300
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN AgenziaIndirizzo NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'AgenziaRecapitoTel') < 40
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN AgenziaRecapitoTel NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'VeicoloMarca') < 100
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN VeicoloMarca NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'VeicoloModello') < 200
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN VeicoloModello NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'VeicoloTarga') < 30
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN VeicoloTarga NVARCHAR(15) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ConducenteContraenteCognome') < 100
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN ConducenteContraenteCognome NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ConducenteContraenteCittadinanzaCod') < 18
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN ConducenteContraenteCittadinanzaCod NVARCHAR(9) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Contratti_Frontiera', N'ConducenteContraenteDocideTipoCod') < 10
        ALTER TABLE dbo.Cargos_Contratti_Frontiera ALTER COLUMN ConducenteContraenteDocideTipoCod NVARCHAR(5) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'TableName') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD TableName NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Tabella_TableName DEFAULT (N'');

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'LastSyncedAt') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD LastSyncedAt DATETIME2 NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'LastSyncStatus') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD LastSyncStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Cargos_Tabella_LastSyncStatus_Migrate DEFAULT (N'NEVER');

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'LastSyncError') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD LastSyncError NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'SyncedRowCount') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD [SyncedRowCount] INT NOT NULL CONSTRAINT DF_Cargos_Tabella_SyncedRowCount_Migrate DEFAULT (0);

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'CreatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_CreatedAt_Migrate DEFAULT (SYSUTCDATETIME());

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_UpdatedAt_Migrate DEFAULT (SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella_Righe', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'TableId') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD TableId INT NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_TableId DEFAULT (0);

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'RowNumber') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD RowNumber INT NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_RowNumber DEFAULT (0);

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Code') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Code NVARCHAR(100) NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_Code DEFAULT (N'');

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Description') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD [Description] NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Column3') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Column3 NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Column4') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Column4 NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Column5') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Column5 NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Column6') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Column6 NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Column7') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Column7 NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'Column8') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD Column8 NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'RawLine') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD RawLine NVARCHAR(2000) NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_RawLine DEFAULT (N'');

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'SyncedAt') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD SyncedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_SyncedAt DEFAULT (SYSUTCDATETIME());

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'CreatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_CreatedAt_Migrate DEFAULT (SYSUTCDATETIME());

    IF COL_LENGTH(N'dbo.Cargos_Tabella_Righe', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.Cargos_Tabella_Righe ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Tabella_Righe_UpdatedAt_Migrate DEFAULT (SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella', N'U') IS NOT NULL
BEGIN
    MERGE dbo.Cargos_Tabella AS tgt
    USING
    (
        SELECT 0 AS TableId, N'TIPO_PAGAMENTO' AS TableName
        UNION ALL SELECT 1, N'LUOGHI'
        UNION ALL SELECT 2, N'TIPO_VEICOLO'
        UNION ALL SELECT 3, N'TIPO_DOCUMENTO'
    ) AS src
        ON tgt.TableId = src.TableId
    WHEN MATCHED THEN
        UPDATE SET
            tgt.TableName = src.TableName,
            tgt.UpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (TableId, TableName, LastSyncedAt, LastSyncStatus, LastSyncError, [SyncedRowCount], CreatedAt, UpdatedAt)
        VALUES (src.TableId, src.TableName, NULL, N'NEVER', NULL, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella_Righe', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'UQ_Cargos_Tabella_Righe_TableRow'
         AND object_id = OBJECT_ID(N'dbo.Cargos_Tabella_Righe')
   )
BEGIN
    CREATE UNIQUE INDEX UQ_Cargos_Tabella_Righe_TableRow
    ON dbo.Cargos_Tabella_Righe (TableId, RowNumber);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Tabella_Righe', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_Cargos_Tabella_Righe_TableCode'
         AND object_id = OBJECT_ID(N'dbo.Cargos_Tabella_Righe')
   )
BEGIN
    CREATE INDEX IX_Cargos_Tabella_Righe_TableCode
    ON dbo.Cargos_Tabella_Righe (TableId, Code);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Contratti_Frontiera', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UQ_Cargos_Contratti_Frontiera_Snapshot'
          AND object_id = OBJECT_ID(N'dbo.Cargos_Contratti_Frontiera')
    )
        DROP INDEX UQ_Cargos_Contratti_Frontiera_Snapshot ON dbo.Cargos_Contratti_Frontiera;

    CREATE UNIQUE INDEX UQ_Cargos_Contratti_Frontiera_Snapshot
    ON dbo.Cargos_Contratti_Frontiera (ContractNo, [ContractLineNo], SnapshotHash)
    WHERE SnapshotHash IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Contratti_Frontiera', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_Cargos_Contratti_Frontiera_StatusRetry'
         AND object_id = OBJECT_ID(N'dbo.Cargos_Contratti_Frontiera')
   )
BEGIN
    CREATE INDEX IX_Cargos_Contratti_Frontiera_StatusRetry
    ON dbo.Cargos_Contratti_Frontiera (Status, NextRetryAt, CreatedAt);
END;
GO

CREATE OR ALTER PROCEDURE dbo.Cargos_Sync_Contratti_Frontiera
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.Cargos_Vista_Contratti', N'V') IS NULL
        THROW 50001, 'View dbo.Cargos_Vista_Contratti not found.', 1;

    DECLARE @RequiredColumns TABLE (ColumnName SYSNAME NOT NULL);
    INSERT INTO @RequiredColumns (ColumnName)
    VALUES
        (N'CONTRATTO_ID'),
        (N'CONTRATTO_DATA'),
        (N'CONTRATTO_TIPOP'),
        (N'CONTRATTO_CHECKOUT_DATA'),
        (N'CONTRATTO_CHECKOUT_LUOGO_COD'),
        (N'CONTRATTO_CHECKOUT_INDIRIZZO'),
        (N'CONTRATTO_CHECKIN_DATA'),
        (N'CONTRATTO_CHECKIN_LUOGO_COD'),
        (N'CONTRATTO_CHECKIN_INDIRIZZO'),
        (N'OPERATORE_ID'),
        (N'AGENZIA_ID'),
        (N'AGENZIA_NOME'),
        (N'AGENZIA_LUOGO_COD'),
        (N'AGENZIA_INDIRIZZO'),
        (N'AGENZIA_RECAPITO_TEL'),
        (N'VEICOLO_TIPO'),
        (N'VEICOLO_MARCA'),
        (N'VEICOLO_MODELLO'),
        (N'VEICOLO_TARGA'),
        (N'CONDUCENTE_CONTRAENTE_COGNOME'),
        (N'CONDUCENTE_CONTRAENTE_NOME'),
        (N'CONDUCENTE_CONTRAENTE_NASCITA_DATA'),
        (N'CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD'),
        (N'CONDUCENTE_CONTRAENTE_CITTADINANZA_COD'),
        (N'CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD'),
        (N'CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO'),
        (N'CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD'),
        (N'CONDUCENTE_CONTRAENTE_PATENTE_NUMERO'),
        (N'CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD');

    DECLARE @MissingColumns NVARCHAR(MAX) = N'';
    SELECT @MissingColumns = @MissingColumns + CASE WHEN LEN(@MissingColumns) = 0 THEN N'' ELSE N', ' END + r.ColumnName
    FROM @RequiredColumns r
    WHERE COL_LENGTH(N'dbo.Cargos_Vista_Contratti', r.ColumnName) IS NULL;

    IF LEN(@MissingColumns) > 0
    BEGIN
        DECLARE @MissingMessage NVARCHAR(2048) = N'View dbo.Cargos_Vista_Contratti missing required columns: ' + @MissingColumns;
        THROW 50002, @MissingMessage, 1;
    END;

    DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();
    DECLARE @TodayLocalDate DATE = CONVERT(DATE, SYSDATETIME());
    DECLARE @ContractNoExpression NVARCHAR(256);
    DECLARE @ContractLineNoExpression NVARCHAR(256);
    DECLARE @CargosContractIdExpression NVARCHAR(256);
    DECLARE @BranchIdExpression NVARCHAR(256) = N'CAST(N'''' AS NVARCHAR(50))';
    DECLARE @BranchEmailExpression NVARCHAR(256) = N'NULL';
    DECLARE @RecordLineExpression NVARCHAR(256) = N'NULL';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'ContractNo') IS NOT NULL
        SET @ContractNoExpression = N'CAST(v.ContractNo AS NVARCHAR(50))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'ContractId') IS NOT NULL
        SET @ContractNoExpression = N'CAST(v.ContractId AS NVARCHAR(50))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'Contract No_') IS NOT NULL
        SET @ContractNoExpression = N'CAST(v.[Contract No_] AS NVARCHAR(50))';
    ELSE
        THROW 50003, N'View dbo.Cargos_Vista_Contratti must expose ContractNo/ContractId or [Contract No_].', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'ContractLineNo') IS NOT NULL
        SET @ContractLineNoExpression = N'CAST(v.ContractLineNo AS BIGINT)';
    ELSE
        THROW 50004, N'View dbo.Cargos_Vista_Contratti must expose ContractLineNo or LineNo or [Line No_].', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'CargosContractId') IS NOT NULL
        SET @CargosContractIdExpression = N'CAST(v.CargosContractId AS NVARCHAR(50))';
    ELSE
        SET @CargosContractIdExpression = @ContractNoExpression;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'BranchId') IS NOT NULL
        SET @BranchIdExpression = N'CAST(v.BranchId AS NVARCHAR(50))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'BranchEmail') IS NOT NULL
        SET @BranchEmailExpression = N'CAST(v.BranchEmail AS NVARCHAR(255))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'RecordLine') IS NOT NULL
        SET @RecordLineExpression = N'CAST(v.RecordLine AS NVARCHAR(2000))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'CargosRecordLine') IS NOT NULL
        SET @RecordLineExpression = N'CAST(v.CargosRecordLine AS NVARCHAR(2000))';

    DECLARE @Queued TABLE
    (
        ContractNo NVARCHAR(50) NOT NULL,
        [ContractLineNo] BIGINT NOT NULL,
        SnapshotHash NVARCHAR(128) NOT NULL
    );

    IF OBJECT_ID(N'tempdb..#SourceContracts') IS NOT NULL
        DROP TABLE #SourceContracts;

    CREATE TABLE #SourceContracts
    (
        ContractNo NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
        [ContractLineNo] BIGINT NOT NULL,
        CargosContractId NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
        BranchId NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
        BranchEmail NVARCHAR(255) COLLATE DATABASE_DEFAULT NULL,
        ContrattoId NVARCHAR(50) COLLATE DATABASE_DEFAULT NULL,
        ContrattoData DATETIME2(0) NULL,
        ContrattoTipoP NVARCHAR(1) COLLATE DATABASE_DEFAULT NULL,
        ContrattoCheckoutData DATETIME2(0) NULL,
        ContrattoCheckoutLuogoCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        ContrattoCheckoutIndirizzo NVARCHAR(150) COLLATE DATABASE_DEFAULT NULL,
        ContrattoCheckinData DATETIME2(0) NULL,
        ContrattoCheckinLuogoCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        ContrattoCheckinIndirizzo NVARCHAR(150) COLLATE DATABASE_DEFAULT NULL,
        OperatoreId NVARCHAR(50) COLLATE DATABASE_DEFAULT NULL,
        AgenziaId NVARCHAR(30) COLLATE DATABASE_DEFAULT NULL,
        AgenziaNome NVARCHAR(70) COLLATE DATABASE_DEFAULT NULL,
        AgenziaLuogoCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        AgenziaIndirizzo NVARCHAR(150) COLLATE DATABASE_DEFAULT NULL,
        AgenziaRecapitoTel NVARCHAR(20) COLLATE DATABASE_DEFAULT NULL,
        VeicoloTipo NVARCHAR(2) COLLATE DATABASE_DEFAULT NULL,
        VeicoloMarca NVARCHAR(50) COLLATE DATABASE_DEFAULT NULL,
        VeicoloModello NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
        VeicoloTarga NVARCHAR(15) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteCognome NVARCHAR(50) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteNome NVARCHAR(38) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteNascitaData DATETIME2(0) NULL,
        ConducenteContraenteNascitaLuogoCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteCittadinanzaCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteDocideTipoCod NVARCHAR(5) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteDocideNumero NVARCHAR(20) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraenteDocideLuogorilCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraentePatenteNumero NVARCHAR(20) COLLATE DATABASE_DEFAULT NULL,
        ConducenteContraentePatenteLuogorilCod NVARCHAR(9) COLLATE DATABASE_DEFAULT NULL,
        RecordLine NVARCHAR(2000) COLLATE DATABASE_DEFAULT NULL,
        DateFingerprint NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
        PayloadFingerprint NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
        SnapshotHash NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
        QueueReason NVARCHAR(30) COLLATE DATABASE_DEFAULT NULL
    );

    DECLARE @Sql NVARCHAR(MAX) = N'
        INSERT INTO #SourceContracts
        (
            ContractNo, ContractLineNo, CargosContractId, BranchId, BranchEmail,
            ContrattoId, ContrattoData, ContrattoTipoP,
            ContrattoCheckoutData, ContrattoCheckoutLuogoCod, ContrattoCheckoutIndirizzo,
            ContrattoCheckinData, ContrattoCheckinLuogoCod, ContrattoCheckinIndirizzo,
            OperatoreId, AgenziaId, AgenziaNome, AgenziaLuogoCod, AgenziaIndirizzo, AgenziaRecapitoTel,
            VeicoloTipo, VeicoloMarca, VeicoloModello, VeicoloTarga,
            ConducenteContraenteCognome, ConducenteContraenteNome, ConducenteContraenteNascitaData,
            ConducenteContraenteNascitaLuogoCod, ConducenteContraenteCittadinanzaCod,
            ConducenteContraenteDocideTipoCod, ConducenteContraenteDocideNumero,
            ConducenteContraenteDocideLuogorilCod, ConducenteContraentePatenteNumero,
            ConducenteContraentePatenteLuogorilCod, RecordLine
        )
        SELECT
            ' + @ContractNoExpression + N',
            ' + @ContractLineNoExpression + N',
            CAST(ISNULL(' + @CargosContractIdExpression + N', ' + @ContractNoExpression + N') AS NVARCHAR(50)),
            ' + @BranchIdExpression + N',
            ' + @BranchEmailExpression + N',
            CAST(v.CONTRATTO_ID AS NVARCHAR(50)),
            TRY_CAST(v.CONTRATTO_DATA AS DATETIME2(0)),
            CAST(v.CONTRATTO_TIPOP AS NVARCHAR(1)),
            TRY_CAST(v.CONTRATTO_CHECKOUT_DATA AS DATETIME2(0)),
            CAST(v.CONTRATTO_CHECKOUT_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.CONTRATTO_CHECKOUT_INDIRIZZO AS NVARCHAR(150)),
            TRY_CAST(v.CONTRATTO_CHECKIN_DATA AS DATETIME2(0)),
            CAST(v.CONTRATTO_CHECKIN_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.CONTRATTO_CHECKIN_INDIRIZZO AS NVARCHAR(150)),
            CAST(v.OPERATORE_ID AS NVARCHAR(50)),
            CAST(v.AGENZIA_ID AS NVARCHAR(30)),
            CAST(v.AGENZIA_NOME AS NVARCHAR(70)),
            CAST(v.AGENZIA_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.AGENZIA_INDIRIZZO AS NVARCHAR(150)),
            CAST(v.AGENZIA_RECAPITO_TEL AS NVARCHAR(20)),
            CAST(v.VEICOLO_TIPO AS NVARCHAR(2)),
            CAST(v.VEICOLO_MARCA AS NVARCHAR(50)),
            CAST(v.VEICOLO_MODELLO AS NVARCHAR(100)),
            CAST(v.VEICOLO_TARGA AS NVARCHAR(15)),
            CAST(v.CONDUCENTE_CONTRAENTE_COGNOME AS NVARCHAR(50)),
            CAST(v.CONDUCENTE_CONTRAENTE_NOME AS NVARCHAR(38)),
            TRY_CAST(v.CONDUCENTE_CONTRAENTE_NASCITA_DATA AS DATETIME2(0)),
            CAST(v.CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.CONDUCENTE_CONTRAENTE_CITTADINANZA_COD AS NVARCHAR(9)),
            CAST(v.CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD AS NVARCHAR(5)),
            CAST(v.CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO AS NVARCHAR(20)),
            CAST(v.CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD AS NVARCHAR(9)),
            CAST(v.CONDUCENTE_CONTRAENTE_PATENTE_NUMERO AS NVARCHAR(20)),
            CAST(v.CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD AS NVARCHAR(9)),
            ' + @RecordLineExpression + N'
        FROM dbo.Cargos_Vista_Contratti v;';

    EXEC sys.sp_executesql @Sql;

    -- Contracts still extracted as open in the source view, but with a planned
    -- check-in date already in the past, must be resent once per day with today's
    -- date. We preserve the original time-of-day if present.
    UPDATE s
    SET
        s.ContrattoCheckinData = DATEADD(
            SECOND,
            DATEDIFF(SECOND, CAST(s.ContrattoCheckinData AS DATE), s.ContrattoCheckinData),
            CAST(@TodayLocalDate AS DATETIME2(0))
        )
    FROM #SourceContracts s
    WHERE s.ContrattoCheckinData IS NOT NULL
      AND CAST(s.ContrattoCheckinData AS DATE) < @TodayLocalDate;

    UPDATE s
    SET
        s.DateFingerprint = CONVERT(
            NVARCHAR(128),
            HASHBYTES(
                'SHA2_256',
                CONCAT(
                    ISNULL(CONVERT(VARCHAR(33), s.ContrattoCheckinData, 126), ''),
                    '|',
                    ISNULL(CONVERT(VARCHAR(33), s.ContrattoCheckoutData, 126), '')
                )
            ),
            2
        ),
        s.PayloadFingerprint = CONVERT(
            NVARCHAR(128),
            HASHBYTES(
                'SHA2_256',
                CONCAT(
                    ISNULL(s.CargosContractId, ''), '|',
                    ISNULL(s.ContrattoId, ''), '|',
                    ISNULL(CONVERT(VARCHAR(33), s.ContrattoData, 126), ''), '|',
                    ISNULL(s.ContrattoTipoP, ''), '|',
                    ISNULL(CONVERT(VARCHAR(33), s.ContrattoCheckoutData, 126), ''), '|',
                    ISNULL(s.ContrattoCheckoutLuogoCod, ''), '|',
                    ISNULL(s.ContrattoCheckoutIndirizzo, ''), '|',
                    ISNULL(CONVERT(VARCHAR(33), s.ContrattoCheckinData, 126), ''), '|',
                    ISNULL(s.ContrattoCheckinLuogoCod, ''), '|',
                    ISNULL(s.ContrattoCheckinIndirizzo, ''), '|',
                    ISNULL(s.OperatoreId, ''), '|',
                    ISNULL(s.AgenziaId, ''), '|',
                    ISNULL(s.AgenziaNome, ''), '|',
                    ISNULL(s.AgenziaLuogoCod, ''), '|',
                    ISNULL(s.AgenziaIndirizzo, ''), '|',
                    ISNULL(s.AgenziaRecapitoTel, ''), '|',
                    ISNULL(s.VeicoloTipo, ''), '|',
                    ISNULL(s.VeicoloMarca, ''), '|',
                    ISNULL(s.VeicoloModello, ''), '|',
                    ISNULL(s.VeicoloTarga, ''), '|',
                    ISNULL(s.ConducenteContraenteCognome, ''), '|',
                    ISNULL(s.ConducenteContraenteNome, ''), '|',
                    ISNULL(CONVERT(VARCHAR(33), s.ConducenteContraenteNascitaData, 126), ''), '|',
                    ISNULL(s.ConducenteContraenteNascitaLuogoCod, ''), '|',
                    ISNULL(s.ConducenteContraenteCittadinanzaCod, ''), '|',
                    ISNULL(s.ConducenteContraenteDocideTipoCod, ''), '|',
                    ISNULL(s.ConducenteContraenteDocideNumero, ''), '|',
                    ISNULL(s.ConducenteContraenteDocideLuogorilCod, ''), '|',
                    ISNULL(s.ConducenteContraentePatenteNumero, ''), '|',
                    ISNULL(s.ConducenteContraentePatenteLuogorilCod, '')
                )
            ),
            2
        )
    FROM #SourceContracts s;

    UPDATE s
    SET
        s.SnapshotHash = CONVERT(
            NVARCHAR(128),
            HASHBYTES(
                'SHA2_256',
                CONCAT(
                    ISNULL(s.DateFingerprint, ''),
                    '|',
                    ISNULL(s.PayloadFingerprint, '')
                )
            ),
            2
        )
    FROM #SourceContracts s;

    UPDATE s
    SET
        s.QueueReason =
            CASE
                WHEN c.ContractNo IS NULL OR c.LastQueuedFingerprint IS NULL THEN 'INITIAL_SEND'
                WHEN ISNULL(c.DateFingerprint, '') <> ISNULL(s.DateFingerprint, '') THEN 'DATE_CHANGE'
                WHEN ISNULL(c.LastQueuedFingerprint, '') <> ISNULL(s.SnapshotHash, '')
                     AND ISNULL(lastf.Status, '') <> 'SENT_OK' THEN 'DATA_FIX'
                ELSE NULL
            END
    FROM #SourceContracts s
    LEFT JOIN dbo.Cargos_Contratti c
        ON c.ContractNo = s.ContractNo
       AND c.[ContractLineNo] = s.[ContractLineNo]
    OUTER APPLY
    (
        SELECT TOP (1)
            f.Status
        FROM dbo.Cargos_Contratti_Frontiera f
        WHERE f.ContractNo = s.ContractNo
          AND f.[ContractLineNo] = s.[ContractLineNo]
        ORDER BY f.CreatedAt DESC, f.Id DESC
    ) lastf;

    MERGE dbo.Cargos_Contratti AS tgt
    USING #SourceContracts AS src
        ON tgt.ContractNo = src.ContractNo
       AND tgt.[ContractLineNo] = src.[ContractLineNo]
    WHEN MATCHED THEN
        UPDATE SET
            tgt.CargosContractId = src.CargosContractId,
            tgt.BranchId = src.BranchId,
            tgt.BranchEmail = src.BranchEmail,
            tgt.ContrattoId = src.ContrattoId,
            tgt.ContrattoData = src.ContrattoData,
            tgt.ContrattoTipoP = src.ContrattoTipoP,
            tgt.ContrattoCheckoutData = src.ContrattoCheckoutData,
            tgt.ContrattoCheckoutLuogoCod = src.ContrattoCheckoutLuogoCod,
            tgt.ContrattoCheckoutIndirizzo = src.ContrattoCheckoutIndirizzo,
            tgt.ContrattoCheckinData = src.ContrattoCheckinData,
            tgt.ContrattoCheckinLuogoCod = src.ContrattoCheckinLuogoCod,
            tgt.ContrattoCheckinIndirizzo = src.ContrattoCheckinIndirizzo,
            tgt.OperatoreId = src.OperatoreId,
            tgt.AgenziaId = src.AgenziaId,
            tgt.AgenziaNome = src.AgenziaNome,
            tgt.AgenziaLuogoCod = src.AgenziaLuogoCod,
            tgt.AgenziaIndirizzo = src.AgenziaIndirizzo,
            tgt.AgenziaRecapitoTel = src.AgenziaRecapitoTel,
            tgt.VeicoloTipo = src.VeicoloTipo,
            tgt.VeicoloMarca = src.VeicoloMarca,
            tgt.VeicoloModello = src.VeicoloModello,
            tgt.VeicoloTarga = src.VeicoloTarga,
            tgt.ConducenteContraenteCognome = src.ConducenteContraenteCognome,
            tgt.ConducenteContraenteNome = src.ConducenteContraenteNome,
            tgt.ConducenteContraenteNascitaData = src.ConducenteContraenteNascitaData,
            tgt.ConducenteContraenteNascitaLuogoCod = src.ConducenteContraenteNascitaLuogoCod,
            tgt.ConducenteContraenteCittadinanzaCod = src.ConducenteContraenteCittadinanzaCod,
            tgt.ConducenteContraenteDocideTipoCod = src.ConducenteContraenteDocideTipoCod,
            tgt.ConducenteContraenteDocideNumero = src.ConducenteContraenteDocideNumero,
            tgt.ConducenteContraenteDocideLuogorilCod = src.ConducenteContraenteDocideLuogorilCod,
            tgt.ConducenteContraentePatenteNumero = src.ConducenteContraentePatenteNumero,
            tgt.ConducenteContraentePatenteLuogorilCod = src.ConducenteContraentePatenteLuogorilCod,
            tgt.RecordLine = src.RecordLine,
            tgt.DateFingerprint = src.DateFingerprint,
            tgt.PayloadFingerprint = src.PayloadFingerprint,
            tgt.DataFingerprint = src.SnapshotHash,
            tgt.LastSeenAt = @NowUtc,
            tgt.UpdatedAt = @NowUtc
    WHEN NOT MATCHED THEN
        INSERT
        (
            ContractNo, [ContractLineNo], CargosContractId, BranchId, BranchEmail,
            ContrattoId, ContrattoData, ContrattoTipoP,
            ContrattoCheckoutData, ContrattoCheckoutLuogoCod, ContrattoCheckoutIndirizzo,
            ContrattoCheckinData, ContrattoCheckinLuogoCod, ContrattoCheckinIndirizzo,
            OperatoreId, AgenziaId, AgenziaNome, AgenziaLuogoCod, AgenziaIndirizzo, AgenziaRecapitoTel,
            VeicoloTipo, VeicoloMarca, VeicoloModello, VeicoloTarga,
            ConducenteContraenteCognome, ConducenteContraenteNome, ConducenteContraenteNascitaData,
            ConducenteContraenteNascitaLuogoCod, ConducenteContraenteCittadinanzaCod,
            ConducenteContraenteDocideTipoCod, ConducenteContraenteDocideNumero,
            ConducenteContraenteDocideLuogorilCod, ConducenteContraentePatenteNumero,
            ConducenteContraentePatenteLuogorilCod,
            RecordLine, DateFingerprint, PayloadFingerprint, DataFingerprint,
            LastQueuedFingerprint, LastQueuedAt, LastSeenAt, CreatedAt, UpdatedAt
        )
        VALUES
        (
            src.ContractNo, src.[ContractLineNo], src.CargosContractId, src.BranchId, src.BranchEmail,
            src.ContrattoId, src.ContrattoData, src.ContrattoTipoP,
            src.ContrattoCheckoutData, src.ContrattoCheckoutLuogoCod, src.ContrattoCheckoutIndirizzo,
            src.ContrattoCheckinData, src.ContrattoCheckinLuogoCod, src.ContrattoCheckinIndirizzo,
            src.OperatoreId, src.AgenziaId, src.AgenziaNome, src.AgenziaLuogoCod, src.AgenziaIndirizzo, src.AgenziaRecapitoTel,
            src.VeicoloTipo, src.VeicoloMarca, src.VeicoloModello, src.VeicoloTarga,
            src.ConducenteContraenteCognome, src.ConducenteContraenteNome, src.ConducenteContraenteNascitaData,
            src.ConducenteContraenteNascitaLuogoCod, src.ConducenteContraenteCittadinanzaCod,
            src.ConducenteContraenteDocideTipoCod, src.ConducenteContraenteDocideNumero,
            src.ConducenteContraenteDocideLuogorilCod, src.ConducenteContraentePatenteNumero,
            src.ConducenteContraentePatenteLuogorilCod,
            src.RecordLine, src.DateFingerprint, src.PayloadFingerprint, src.SnapshotHash,
            NULL, NULL, @NowUtc, @NowUtc, @NowUtc
        );

    INSERT INTO dbo.Cargos_Contratti_Frontiera
    (
        ContractNo, [ContractLineNo], CargosContractId, BranchId, BranchEmail,
        ContrattoId, ContrattoData, ContrattoTipoP,
        ContrattoCheckoutData, ContrattoCheckoutLuogoCod, ContrattoCheckoutIndirizzo,
        ContrattoCheckinData, ContrattoCheckinLuogoCod, ContrattoCheckinIndirizzo,
        OperatoreId, AgenziaId, AgenziaNome, AgenziaLuogoCod, AgenziaIndirizzo, AgenziaRecapitoTel,
        VeicoloTipo, VeicoloMarca, VeicoloModello, VeicoloTarga,
        ConducenteContraenteCognome, ConducenteContraenteNome, ConducenteContraenteNascitaData,
        ConducenteContraenteNascitaLuogoCod, ConducenteContraenteCittadinanzaCod,
        ConducenteContraenteDocideTipoCod, ConducenteContraenteDocideNumero,
        ConducenteContraenteDocideLuogorilCod, ConducenteContraentePatenteNumero,
        ConducenteContraentePatenteLuogorilCod,
        Reason, SnapshotHash, RecordLine, Status, AttemptCount,
        LastMissingEmailAt, LastMissingFieldsHash, LastRejectEmailAt, LastRejectHash,
        CreatedAt, UpdatedAt
    )
    OUTPUT inserted.ContractNo, inserted.[ContractLineNo], inserted.SnapshotHash
        INTO @Queued (ContractNo, [ContractLineNo], SnapshotHash)
    SELECT
        s.ContractNo, s.[ContractLineNo], s.CargosContractId, s.BranchId, s.BranchEmail,
        s.ContrattoId, s.ContrattoData, s.ContrattoTipoP,
        s.ContrattoCheckoutData, s.ContrattoCheckoutLuogoCod, s.ContrattoCheckoutIndirizzo,
        s.ContrattoCheckinData, s.ContrattoCheckinLuogoCod, s.ContrattoCheckinIndirizzo,
        s.OperatoreId, s.AgenziaId, s.AgenziaNome, s.AgenziaLuogoCod, s.AgenziaIndirizzo, s.AgenziaRecapitoTel,
        s.VeicoloTipo, s.VeicoloMarca, s.VeicoloModello, s.VeicoloTarga,
        s.ConducenteContraenteCognome, s.ConducenteContraenteNome, s.ConducenteContraenteNascitaData,
        s.ConducenteContraenteNascitaLuogoCod, s.ConducenteContraenteCittadinanzaCod,
        s.ConducenteContraenteDocideTipoCod, s.ConducenteContraenteDocideNumero,
        s.ConducenteContraenteDocideLuogorilCod, s.ConducenteContraentePatenteNumero,
        s.ConducenteContraentePatenteLuogorilCod,
        s.QueueReason, s.SnapshotHash, s.RecordLine, 'PENDING', 0,
        lastf.LastMissingEmailAt, lastf.LastMissingFieldsHash, lastf.LastRejectEmailAt, lastf.LastRejectHash,
        @NowUtc, @NowUtc
    FROM #SourceContracts s
    OUTER APPLY
    (
        SELECT TOP (1)
            f.LastMissingEmailAt,
            f.LastMissingFieldsHash,
            f.LastRejectEmailAt,
            f.LastRejectHash
        FROM dbo.Cargos_Contratti_Frontiera f
        WHERE f.ContractNo = s.ContractNo
          AND f.[ContractLineNo] = s.[ContractLineNo]
        ORDER BY f.CreatedAt DESC, f.Id DESC
    ) lastf
    WHERE s.QueueReason IS NOT NULL
      AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.Cargos_Contratti_Frontiera f
        WHERE f.ContractNo = s.ContractNo
          AND f.[ContractLineNo] = s.[ContractLineNo]
          AND f.SnapshotHash = s.SnapshotHash
    );

    UPDATE c
    SET
        c.LastQueuedFingerprint = q.SnapshotHash,
        c.LastQueuedAt = @NowUtc,
        c.UpdatedAt = @NowUtc
    FROM dbo.Cargos_Contratti c
    INNER JOIN @Queued q
        ON q.ContractNo = c.ContractNo
       AND q.[ContractLineNo] = c.[ContractLineNo];

    SELECT QueuedItems = COUNT(1)
    FROM @Queued;
END;
GO

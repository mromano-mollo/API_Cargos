/*
    CARGOS - Database setup and sync.

    The procedure dbo.Cargos_Sync_Contratti_Frontiera expects source view:
      dbo.Cargos_Vista_Contratti

    Required identity columns:
      - ContractNo or [Contract No_] (legacy ContractId accepted)
      - LineNo or [Line No_]
      - BranchId

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
        [LineNo] BIGINT NOT NULL,
        CargosContractId NVARCHAR(50) NOT NULL,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        ContrattoId NVARCHAR(10) NULL,
        ContrattoData DATETIME2(0) NULL,
        ContrattoTipoP NVARCHAR(1) NULL,
        ContrattoCheckoutData DATETIME2(0) NULL,
        ContrattoCheckoutLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckoutIndirizzo NVARCHAR(80) NULL,
        ContrattoCheckinData DATETIME2(0) NULL,
        ContrattoCheckinLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckinIndirizzo NVARCHAR(80) NULL,
        OperatoreId NVARCHAR(9) NULL,
        AgenziaId NVARCHAR(9) NULL,
        AgenziaNome NVARCHAR(38) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(80) NULL,
        AgenziaRecapitoTel NVARCHAR(15) NULL,
        VeicoloTipo NVARCHAR(2) NULL,
        VeicoloMarca NVARCHAR(20) NULL,
        VeicoloModello NVARCHAR(20) NULL,
        VeicoloTarga NVARCHAR(10) NULL,
        ConducenteContraenteCognome NVARCHAR(38) NULL,
        ConducenteContraenteNome NVARCHAR(38) NULL,
        ConducenteContraenteNascitaData DATETIME2(0) NULL,
        ConducenteContraenteNascitaLuogoCod NVARCHAR(9) NULL,
        ConducenteContraenteCittadinanzaCod NVARCHAR(3) NULL,
        ConducenteContraenteDocideTipoCod NVARCHAR(2) NULL,
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
        ADD CONSTRAINT UQ_Cargos_Contratti_ContractLine UNIQUE (ContractNo, [LineNo]);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Agenzie', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Agenzie
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Agenzie PRIMARY KEY,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        AgenziaId NVARCHAR(9) NOT NULL,
        AgenziaNome NVARCHAR(38) NOT NULL,
        AgenziaLuogoValue NVARCHAR(255) NOT NULL,
        AgenziaCity NVARCHAR(100) NULL,
        AgenziaCounty NVARCHAR(10) NULL,
        AgenziaPostCode NVARCHAR(20) NULL,
        AgenziaIndirizzo NVARCHAR(80) NOT NULL,
        AgenziaRecapitoTel NVARCHAR(15) NOT NULL,
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
        AgenziaId NVARCHAR(9) NOT NULL,
        AgenziaNome NVARCHAR(38) NOT NULL,
        AgenziaLuogoValue NVARCHAR(255) NOT NULL,
        AgenziaCity NVARCHAR(100) NULL,
        AgenziaCounty NVARCHAR(10) NULL,
        AgenziaPostCode NVARCHAR(20) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(80) NOT NULL,
        AgenziaRecapitoTel NVARCHAR(15) NOT NULL,
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
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaId NVARCHAR(9) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaId DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaNome') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaNome NVARCHAR(38) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaNome DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaLuogoValue') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaLuogoValue NVARCHAR(255) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaLuogoValue DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaCity') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaCity NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaCounty') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaCounty NVARCHAR(10) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaPostCode') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaPostCode NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaIndirizzo') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaIndirizzo NVARCHAR(80) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaIndirizzo DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie', N'AgenziaRecapitoTel') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie ADD AgenziaRecapitoTel NVARCHAR(15) NOT NULL CONSTRAINT DF_Cargos_Agenzie_AgenziaRecapitoTel DEFAULT (N'');
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
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaId NVARCHAR(9) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaId DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaNome') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaNome NVARCHAR(38) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaNome DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaLuogoValue') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaLuogoValue NVARCHAR(255) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaLuogoValue DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaCity') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaCity NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaCounty') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaCounty NVARCHAR(10) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaPostCode') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaPostCode NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaLuogoCod') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaLuogoCod NVARCHAR(9) NULL;
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaIndirizzo') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaIndirizzo NVARCHAR(80) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaIndirizzo DEFAULT (N'');
    IF COL_LENGTH(N'dbo.Cargos_Agenzie_Frontiera', N'AgenziaRecapitoTel') IS NULL
        ALTER TABLE dbo.Cargos_Agenzie_Frontiera ADD AgenziaRecapitoTel NVARCHAR(15) NOT NULL CONSTRAINT DF_Cargos_Agenzie_Frontiera_AgenziaRecapitoTel DEFAULT (N'');
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
        SET @AgenziaIdExpression = N'CAST(v.AgenziaId AS NVARCHAR(9))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_ID') IS NOT NULL
        SET @AgenziaIdExpression = N'CAST(v.AGENZIA_ID AS NVARCHAR(9))';
    ELSE
        THROW 50103, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaId or AGENZIA_ID.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaNome') IS NOT NULL
        SET @AgenziaNomeExpression = N'CAST(v.AgenziaNome AS NVARCHAR(38))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_NOME') IS NOT NULL
        SET @AgenziaNomeExpression = N'CAST(v.AGENZIA_NOME AS NVARCHAR(38))';
    ELSE
        THROW 50104, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaNome or AGENZIA_NOME.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaLuogoValue') IS NOT NULL
        SET @AgenziaLuogoValueExpression = N'CAST(v.AgenziaLuogoValue AS NVARCHAR(255))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaLuogoCod') IS NOT NULL
        SET @AgenziaLuogoValueExpression = N'CAST(v.AgenziaLuogoCod AS NVARCHAR(255))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_LUOGO_COD') IS NOT NULL
        SET @AgenziaLuogoValueExpression = N'CAST(v.AGENZIA_LUOGO_COD AS NVARCHAR(255))';
    ELSE
        THROW 50105, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaLuogoValue/AgenziaLuogoCod or AGENZIA_LUOGO_COD.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaCity') IS NOT NULL
        SET @AgenziaCityExpression = N'CAST(v.AgenziaCity AS NVARCHAR(100))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaCounty') IS NOT NULL
        SET @AgenziaCountyExpression = N'CAST(v.AgenziaCounty AS NVARCHAR(10))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaPostCode') IS NOT NULL
        SET @AgenziaPostCodeExpression = N'CAST(v.AgenziaPostCode AS NVARCHAR(20))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaIndirizzo') IS NOT NULL
        SET @AgenziaIndirizzoExpression = N'CAST(v.AgenziaIndirizzo AS NVARCHAR(80))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_INDIRIZZO') IS NOT NULL
        SET @AgenziaIndirizzoExpression = N'CAST(v.AGENZIA_INDIRIZZO AS NVARCHAR(80))';
    ELSE
        THROW 50106, 'View dbo.Cargos_Vista_Agenzie must expose AgenziaIndirizzo or AGENZIA_INDIRIZZO.', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AgenziaRecapitoTel') IS NOT NULL
        SET @AgenziaRecapitoTelExpression = N'CAST(v.AgenziaRecapitoTel AS NVARCHAR(15))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Agenzie', N'AGENZIA_RECAPITO_TEL') IS NOT NULL
        SET @AgenziaRecapitoTelExpression = N'CAST(v.AGENZIA_RECAPITO_TEL AS NVARCHAR(15))';
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
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        AgenziaId NVARCHAR(9) NOT NULL,
        AgenziaNome NVARCHAR(38) NOT NULL,
        AgenziaLuogoValue NVARCHAR(255) NOT NULL,
        AgenziaCity NVARCHAR(100) NULL,
        AgenziaCounty NVARCHAR(10) NULL,
        AgenziaPostCode NVARCHAR(20) NULL,
        AgenziaIndirizzo NVARCHAR(80) NOT NULL,
        AgenziaRecapitoTel NVARCHAR(15) NOT NULL,
        PayloadFingerprint NVARCHAR(128) NULL,
        SnapshotHash NVARCHAR(128) NULL,
        QueueReason NVARCHAR(30) NULL
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

IF OBJECT_ID(N'dbo.Cargos_Frontiera', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cargos_Frontiera
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos_Frontiera PRIMARY KEY,
        ContractNo NVARCHAR(50) NOT NULL,
        [LineNo] BIGINT NOT NULL,
        CargosContractId NVARCHAR(50) NOT NULL,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        ContrattoId NVARCHAR(10) NULL,
        ContrattoData DATETIME2(0) NULL,
        ContrattoTipoP NVARCHAR(1) NULL,
        ContrattoCheckoutData DATETIME2(0) NULL,
        ContrattoCheckoutLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckoutIndirizzo NVARCHAR(80) NULL,
        ContrattoCheckinData DATETIME2(0) NULL,
        ContrattoCheckinLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckinIndirizzo NVARCHAR(80) NULL,
        OperatoreId NVARCHAR(9) NULL,
        AgenziaId NVARCHAR(9) NULL,
        AgenziaNome NVARCHAR(38) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(80) NULL,
        AgenziaRecapitoTel NVARCHAR(15) NULL,
        VeicoloTipo NVARCHAR(2) NULL,
        VeicoloMarca NVARCHAR(20) NULL,
        VeicoloModello NVARCHAR(20) NULL,
        VeicoloTarga NVARCHAR(10) NULL,
        ConducenteContraenteCognome NVARCHAR(38) NULL,
        ConducenteContraenteNome NVARCHAR(38) NULL,
        ConducenteContraenteNascitaData DATETIME2(0) NULL,
        ConducenteContraenteNascitaLuogoCod NVARCHAR(9) NULL,
        ConducenteContraenteCittadinanzaCod NVARCHAR(3) NULL,
        ConducenteContraenteDocideTipoCod NVARCHAR(2) NULL,
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
        AttemptCount INT NOT NULL CONSTRAINT DF_Cargos_Frontiera_AttemptCount DEFAULT (0),
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
        [RowCount] INT NOT NULL CONSTRAINT DF_Cargos_Tabella_RowCount DEFAULT (0),
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
        (N'LineNo', N'BIGINT NOT NULL CONSTRAINT DF_Cargos_Contratti_LineNo DEFAULT (0)'),
        (N'CargosContractId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_CargosContractId DEFAULT (N'''')'),
        (N'BranchId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Contratti_BranchId DEFAULT (N'''')'),
        (N'BranchEmail', N'NVARCHAR(255) NULL'),
        (N'ContrattoId', N'NVARCHAR(10) NULL'),
        (N'ContrattoData', N'DATETIME2(0) NULL'),
        (N'ContrattoTipoP', N'NVARCHAR(1) NULL'),
        (N'ContrattoCheckoutData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckoutLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckoutIndirizzo', N'NVARCHAR(80) NULL'),
        (N'ContrattoCheckinData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckinLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckinIndirizzo', N'NVARCHAR(80) NULL'),
        (N'OperatoreId', N'NVARCHAR(9) NULL'),
        (N'AgenziaId', N'NVARCHAR(9) NULL'),
        (N'AgenziaNome', N'NVARCHAR(38) NULL'),
        (N'AgenziaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'AgenziaIndirizzo', N'NVARCHAR(80) NULL'),
        (N'AgenziaRecapitoTel', N'NVARCHAR(15) NULL'),
        (N'VeicoloTipo', N'NVARCHAR(2) NULL'),
        (N'VeicoloMarca', N'NVARCHAR(20) NULL'),
        (N'VeicoloModello', N'NVARCHAR(20) NULL'),
        (N'VeicoloTarga', N'NVARCHAR(10) NULL'),
        (N'ConducenteContraenteCognome', N'NVARCHAR(38) NULL'),
        (N'ConducenteContraenteNome', N'NVARCHAR(38) NULL'),
        (N'ConducenteContraenteNascitaData', N'DATETIME2(0) NULL'),
        (N'ConducenteContraenteNascitaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraenteCittadinanzaCod', N'NVARCHAR(3) NULL'),
        (N'ConducenteContraenteDocideTipoCod', N'NVARCHAR(2) NULL'),
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
            ADD CONSTRAINT UQ_Cargos_Contratti_ContractLine UNIQUE (ContractNo, [LineNo]);
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Frontiera', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Cargos_Frontiera', N'ContractNo') IS NULL
       AND COL_LENGTH(N'dbo.Cargos_Frontiera', N'ContractId') IS NOT NULL
        EXEC sys.sp_rename N'dbo.Cargos_Frontiera.ContractId', N'ContractNo', N'COLUMN';

    DECLARE @FrontieraColumns TABLE
    (
        ColumnName SYSNAME NOT NULL,
        ColumnDefinition NVARCHAR(400) NOT NULL
    );

    INSERT INTO @FrontieraColumns (ColumnName, ColumnDefinition)
    VALUES
        (N'ContractNo', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Frontiera_ContractNo DEFAULT (N'''')'),
        (N'LineNo', N'BIGINT NOT NULL CONSTRAINT DF_Cargos_Frontiera_LineNo DEFAULT (0)'),
        (N'CargosContractId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Frontiera_CargosContractId DEFAULT (N'''')'),
        (N'BranchId', N'NVARCHAR(50) NOT NULL CONSTRAINT DF_Cargos_Frontiera_BranchId DEFAULT (N'''')'),
        (N'BranchEmail', N'NVARCHAR(255) NULL'),
        (N'ContrattoId', N'NVARCHAR(10) NULL'),
        (N'ContrattoData', N'DATETIME2(0) NULL'),
        (N'ContrattoTipoP', N'NVARCHAR(1) NULL'),
        (N'ContrattoCheckoutData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckoutLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckoutIndirizzo', N'NVARCHAR(80) NULL'),
        (N'ContrattoCheckinData', N'DATETIME2(0) NULL'),
        (N'ContrattoCheckinLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ContrattoCheckinIndirizzo', N'NVARCHAR(80) NULL'),
        (N'OperatoreId', N'NVARCHAR(9) NULL'),
        (N'AgenziaId', N'NVARCHAR(9) NULL'),
        (N'AgenziaNome', N'NVARCHAR(38) NULL'),
        (N'AgenziaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'AgenziaIndirizzo', N'NVARCHAR(80) NULL'),
        (N'AgenziaRecapitoTel', N'NVARCHAR(15) NULL'),
        (N'VeicoloTipo', N'NVARCHAR(2) NULL'),
        (N'VeicoloMarca', N'NVARCHAR(20) NULL'),
        (N'VeicoloModello', N'NVARCHAR(20) NULL'),
        (N'VeicoloTarga', N'NVARCHAR(10) NULL'),
        (N'ConducenteContraenteCognome', N'NVARCHAR(38) NULL'),
        (N'ConducenteContraenteNome', N'NVARCHAR(38) NULL'),
        (N'ConducenteContraenteNascitaData', N'DATETIME2(0) NULL'),
        (N'ConducenteContraenteNascitaLuogoCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraenteCittadinanzaCod', N'NVARCHAR(3) NULL'),
        (N'ConducenteContraenteDocideTipoCod', N'NVARCHAR(2) NULL'),
        (N'ConducenteContraenteDocideNumero', N'NVARCHAR(20) NULL'),
        (N'ConducenteContraenteDocideLuogorilCod', N'NVARCHAR(9) NULL'),
        (N'ConducenteContraentePatenteNumero', N'NVARCHAR(20) NULL'),
        (N'ConducenteContraentePatenteLuogorilCod', N'NVARCHAR(9) NULL'),
        (N'Reason', N'NVARCHAR(30) NULL'),
        (N'SnapshotHash', N'NVARCHAR(128) NULL'),
        (N'RecordLine', N'NVARCHAR(2000) NULL'),
        (N'Status', N'NVARCHAR(30) NOT NULL CONSTRAINT DF_Cargos_Frontiera_Status DEFAULT (N''PENDING'')'),
        (N'MissingFields', N'NVARCHAR(MAX) NULL'),
        (N'LastError', N'NVARCHAR(MAX) NULL'),
        (N'TransactionId', N'NVARCHAR(100) NULL'),
        (N'AttemptCount', N'INT NOT NULL CONSTRAINT DF_Cargos_Frontiera_AttemptCount_Migrate DEFAULT (0)'),
        (N'LastAttemptAt', N'DATETIME2 NULL'),
        (N'NextRetryAt', N'DATETIME2 NULL'),
        (N'ClaimedBy', N'NVARCHAR(100) NULL'),
        (N'ClaimedAt', N'DATETIME2 NULL'),
        (N'LastMissingEmailAt', N'DATETIME2 NULL'),
        (N'LastMissingFieldsHash', N'NVARCHAR(128) NULL'),
        (N'LastRejectEmailAt', N'DATETIME2 NULL'),
        (N'LastRejectHash', N'NVARCHAR(128) NULL'),
        (N'CreatedAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Frontiera_CreatedAt DEFAULT (SYSUTCDATETIME())'),
        (N'UpdatedAt', N'DATETIME2 NOT NULL CONSTRAINT DF_Cargos_Frontiera_UpdatedAt DEFAULT (SYSUTCDATETIME())');

    DECLARE @SqlAddFrontiera NVARCHAR(MAX) = N'';
    SELECT @SqlAddFrontiera = @SqlAddFrontiera +
        N'IF COL_LENGTH(N''dbo.Cargos_Frontiera'', N''' + c.ColumnName + N''') IS NULL ' +
        N'ALTER TABLE dbo.Cargos_Frontiera ADD ' + QUOTENAME(c.ColumnName) + N' ' + c.ColumnDefinition + N';'
    FROM @FrontieraColumns c;

    EXEC sys.sp_executesql @SqlAddFrontiera;
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

    IF COL_LENGTH(N'dbo.Cargos_Tabella', N'RowCount') IS NULL
        ALTER TABLE dbo.Cargos_Tabella ADD [RowCount] INT NOT NULL CONSTRAINT DF_Cargos_Tabella_RowCount_Migrate DEFAULT (0);

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
        INSERT (TableId, TableName, LastSyncedAt, LastSyncStatus, LastSyncError, [RowCount], CreatedAt, UpdatedAt)
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

IF OBJECT_ID(N'dbo.Cargos_Frontiera', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UQ_Cargos_Frontiera_Snapshot'
          AND object_id = OBJECT_ID(N'dbo.Cargos_Frontiera')
    )
        DROP INDEX UQ_Cargos_Frontiera_Snapshot ON dbo.Cargos_Frontiera;

    CREATE UNIQUE INDEX UQ_Cargos_Frontiera_Snapshot
    ON dbo.Cargos_Frontiera (ContractNo, [LineNo], SnapshotHash)
    WHERE SnapshotHash IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cargos_Frontiera', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_Cargos_Frontiera_StatusRetry'
         AND object_id = OBJECT_ID(N'dbo.Cargos_Frontiera')
   )
BEGIN
    CREATE INDEX IX_Cargos_Frontiera_StatusRetry
    ON dbo.Cargos_Frontiera (Status, NextRetryAt, CreatedAt);
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
        (N'BranchId'),
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
    DECLARE @ContractNoExpression NVARCHAR(256);
    DECLARE @LineNoExpression NVARCHAR(256);
    DECLARE @CargosContractIdExpression NVARCHAR(256);
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

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'LineNo') IS NOT NULL
        SET @LineNoExpression = N'CAST(v.LineNo AS BIGINT)';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'Line No_') IS NOT NULL
        SET @LineNoExpression = N'CAST(v.[Line No_] AS BIGINT)';
    ELSE
        THROW 50004, N'View dbo.Cargos_Vista_Contratti must expose LineNo or [Line No_].', 1;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'CargosContractId') IS NOT NULL
        SET @CargosContractIdExpression = N'CAST(v.CargosContractId AS NVARCHAR(50))';
    ELSE
        SET @CargosContractIdExpression = @ContractNoExpression;

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'BranchEmail') IS NOT NULL
        SET @BranchEmailExpression = N'CAST(v.BranchEmail AS NVARCHAR(255))';

    IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'RecordLine') IS NOT NULL
        SET @RecordLineExpression = N'CAST(v.RecordLine AS NVARCHAR(2000))';
    ELSE IF COL_LENGTH(N'dbo.Cargos_Vista_Contratti', N'CargosRecordLine') IS NOT NULL
        SET @RecordLineExpression = N'CAST(v.CargosRecordLine AS NVARCHAR(2000))';

    DECLARE @Queued TABLE
    (
        ContractNo NVARCHAR(50) NOT NULL,
        [LineNo] BIGINT NOT NULL,
        SnapshotHash NVARCHAR(128) NOT NULL
    );

    IF OBJECT_ID(N'tempdb..#SourceContracts') IS NOT NULL
        DROP TABLE #SourceContracts;

    CREATE TABLE #SourceContracts
    (
        ContractNo NVARCHAR(50) NOT NULL,
        [LineNo] BIGINT NOT NULL,
        CargosContractId NVARCHAR(50) NOT NULL,
        BranchId NVARCHAR(50) NOT NULL,
        BranchEmail NVARCHAR(255) NULL,
        ContrattoId NVARCHAR(10) NULL,
        ContrattoData DATETIME2(0) NULL,
        ContrattoTipoP NVARCHAR(1) NULL,
        ContrattoCheckoutData DATETIME2(0) NULL,
        ContrattoCheckoutLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckoutIndirizzo NVARCHAR(80) NULL,
        ContrattoCheckinData DATETIME2(0) NULL,
        ContrattoCheckinLuogoCod NVARCHAR(9) NULL,
        ContrattoCheckinIndirizzo NVARCHAR(80) NULL,
        OperatoreId NVARCHAR(9) NULL,
        AgenziaId NVARCHAR(9) NULL,
        AgenziaNome NVARCHAR(38) NULL,
        AgenziaLuogoCod NVARCHAR(9) NULL,
        AgenziaIndirizzo NVARCHAR(80) NULL,
        AgenziaRecapitoTel NVARCHAR(15) NULL,
        VeicoloTipo NVARCHAR(2) NULL,
        VeicoloMarca NVARCHAR(20) NULL,
        VeicoloModello NVARCHAR(20) NULL,
        VeicoloTarga NVARCHAR(10) NULL,
        ConducenteContraenteCognome NVARCHAR(38) NULL,
        ConducenteContraenteNome NVARCHAR(38) NULL,
        ConducenteContraenteNascitaData DATETIME2(0) NULL,
        ConducenteContraenteNascitaLuogoCod NVARCHAR(9) NULL,
        ConducenteContraenteCittadinanzaCod NVARCHAR(3) NULL,
        ConducenteContraenteDocideTipoCod NVARCHAR(2) NULL,
        ConducenteContraenteDocideNumero NVARCHAR(20) NULL,
        ConducenteContraenteDocideLuogorilCod NVARCHAR(9) NULL,
        ConducenteContraentePatenteNumero NVARCHAR(20) NULL,
        ConducenteContraentePatenteLuogorilCod NVARCHAR(9) NULL,
        RecordLine NVARCHAR(2000) NULL,
        DateFingerprint NVARCHAR(128) NULL,
        PayloadFingerprint NVARCHAR(128) NULL,
        SnapshotHash NVARCHAR(128) NULL,
        QueueReason NVARCHAR(30) NULL
    );

    DECLARE @Sql NVARCHAR(MAX) = N'
        INSERT INTO #SourceContracts
        (
            ContractNo, LineNo, CargosContractId, BranchId, BranchEmail,
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
            ' + @LineNoExpression + N',
            CAST(ISNULL(' + @CargosContractIdExpression + N', ' + @ContractNoExpression + N') AS NVARCHAR(50)),
            CAST(v.BranchId AS NVARCHAR(50)),
            ' + @BranchEmailExpression + N',
            CAST(v.CONTRATTO_ID AS NVARCHAR(10)),
            TRY_CAST(v.CONTRATTO_DATA AS DATETIME2(0)),
            CAST(v.CONTRATTO_TIPOP AS NVARCHAR(1)),
            TRY_CAST(v.CONTRATTO_CHECKOUT_DATA AS DATETIME2(0)),
            CAST(v.CONTRATTO_CHECKOUT_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.CONTRATTO_CHECKOUT_INDIRIZZO AS NVARCHAR(80)),
            TRY_CAST(v.CONTRATTO_CHECKIN_DATA AS DATETIME2(0)),
            CAST(v.CONTRATTO_CHECKIN_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.CONTRATTO_CHECKIN_INDIRIZZO AS NVARCHAR(80)),
            CAST(v.OPERATORE_ID AS NVARCHAR(9)),
            CAST(v.AGENZIA_ID AS NVARCHAR(9)),
            CAST(v.AGENZIA_NOME AS NVARCHAR(38)),
            CAST(v.AGENZIA_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.AGENZIA_INDIRIZZO AS NVARCHAR(80)),
            CAST(v.AGENZIA_RECAPITO_TEL AS NVARCHAR(15)),
            CAST(v.VEICOLO_TIPO AS NVARCHAR(2)),
            CAST(v.VEICOLO_MARCA AS NVARCHAR(20)),
            CAST(v.VEICOLO_MODELLO AS NVARCHAR(20)),
            CAST(v.VEICOLO_TARGA AS NVARCHAR(10)),
            CAST(v.CONDUCENTE_CONTRAENTE_COGNOME AS NVARCHAR(38)),
            CAST(v.CONDUCENTE_CONTRAENTE_NOME AS NVARCHAR(38)),
            TRY_CAST(v.CONDUCENTE_CONTRAENTE_NASCITA_DATA AS DATETIME2(0)),
            CAST(v.CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD AS NVARCHAR(9)),
            CAST(v.CONDUCENTE_CONTRAENTE_CITTADINANZA_COD AS NVARCHAR(3)),
            CAST(v.CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD AS NVARCHAR(2)),
            CAST(v.CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO AS NVARCHAR(20)),
            CAST(v.CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD AS NVARCHAR(9)),
            CAST(v.CONDUCENTE_CONTRAENTE_PATENTE_NUMERO AS NVARCHAR(20)),
            CAST(v.CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD AS NVARCHAR(9)),
            ' + @RecordLineExpression + N'
        FROM dbo.Cargos_Vista_Contratti v;';

    EXEC sys.sp_executesql @Sql;

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
                    ISNULL(s.BranchId, ''), '|',
                    ISNULL(s.BranchEmail, ''), '|',
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
       AND c.[LineNo] = s.[LineNo]
    OUTER APPLY
    (
        SELECT TOP (1)
            f.Status
        FROM dbo.Cargos_Frontiera f
        WHERE f.ContractNo = s.ContractNo
          AND f.[LineNo] = s.[LineNo]
        ORDER BY f.CreatedAt DESC, f.Id DESC
    ) lastf;

    MERGE dbo.Cargos_Contratti AS tgt
    USING #SourceContracts AS src
        ON tgt.ContractNo = src.ContractNo
       AND tgt.[LineNo] = src.[LineNo]
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
            ContractNo, [LineNo], CargosContractId, BranchId, BranchEmail,
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
            src.ContractNo, src.[LineNo], src.CargosContractId, src.BranchId, src.BranchEmail,
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

    INSERT INTO dbo.Cargos_Frontiera
    (
        ContractNo, [LineNo], CargosContractId, BranchId, BranchEmail,
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
    OUTPUT inserted.ContractNo, inserted.[LineNo], inserted.SnapshotHash
        INTO @Queued (ContractNo, [LineNo], SnapshotHash)
    SELECT
        s.ContractNo, s.[LineNo], s.CargosContractId, s.BranchId, s.BranchEmail,
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
        FROM dbo.Cargos_Frontiera f
        WHERE f.ContractNo = s.ContractNo
          AND f.[LineNo] = s.[LineNo]
        ORDER BY f.CreatedAt DESC, f.Id DESC
    ) lastf
    WHERE s.QueueReason IS NOT NULL
      AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.Cargos_Frontiera f
        WHERE f.ContractNo = s.ContractNo
          AND f.[LineNo] = s.[LineNo]
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
       AND q.[LineNo] = c.[LineNo];

    SELECT QueuedItems = COUNT(1)
    FROM @Queued;
END;
GO

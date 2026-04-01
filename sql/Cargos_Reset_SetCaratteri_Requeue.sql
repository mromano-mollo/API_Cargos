/*
    CARGOS - Maintenance reset for SetCaratteri rejects

    Purpose:
    - Find the latest outbox row per company/contract-line
    - Only when the latest row is:
        Status = SENT_KO_DATA
        LastError contains "Vincolo SetCaratteri"
    - Reset that row to PENDING so the worker rebuilds RecordLine
      with the current app-side normalization and retries the send

    Recommended usage:
    1) Deploy current app version with the new RecordBuilder normalization
    2) If agency source text was also cleaned in the views, refresh those views first
    3) Run this script once
    4) Start the worker again
*/

SET NOCOUNT ON;

DECLARE @NowLocal DATETIME2 = SYSDATETIME();

DECLARE @ResetRows TABLE
(
    OutboxId BIGINT NOT NULL PRIMARY KEY,
    Company NVARCHAR(50) NOT NULL,
    ContractNo NVARCHAR(50) NOT NULL,
    ContractLineNo BIGINT NOT NULL
);

;WITH Latest AS
(
    SELECT
        f.Id,
        f.Company,
        f.ContractNo,
        f.ContractLineNo,
        f.Status,
        f.LastError,
        ROW_NUMBER() OVER
        (
            PARTITION BY f.Company, f.ContractNo, f.ContractLineNo
            ORDER BY f.CreatedAt DESC, f.Id DESC
        ) AS rn
    FROM dbo.Cargos_Contratti_Frontiera f
)
UPDATE f
SET
    f.Status = N'PENDING',
    f.RecordLine = NULL,
    f.MissingFields = NULL,
    f.LastError = NULL,
    f.NextRetryAt = NULL,
    f.ClaimedBy = NULL,
    f.ClaimedAt = NULL,
    f.UpdatedAt = @NowLocal
OUTPUT
    inserted.Id,
    inserted.Company,
    inserted.ContractNo,
    inserted.ContractLineNo
INTO @ResetRows
(
    OutboxId,
    Company,
    ContractNo,
    ContractLineNo
)
FROM dbo.Cargos_Contratti_Frontiera f
INNER JOIN Latest l
    ON l.Id = f.Id
WHERE l.rn = 1
  AND l.Status = N'SENT_KO_DATA'
  AND ISNULL(l.LastError, N'') LIKE N'%Vincolo SetCaratteri%';

UPDATE c
SET
    c.Status = N'PENDING',
    c.RecordLine = NULL,
    c.UpdatedAt = @NowLocal
FROM dbo.Cargos_Contratti c
INNER JOIN @ResetRows r
    ON r.Company = c.Company
   AND r.ContractNo = c.ContractNo
   AND r.ContractLineNo = c.ContractLineNo;

SELECT
    ResetRows = COUNT(1)
FROM @ResetRows;

SELECT
    r.OutboxId,
    r.Company,
    r.ContractNo,
    r.ContractLineNo
FROM @ResetRows r
ORDER BY r.Company, r.ContractNo, r.ContractLineNo;

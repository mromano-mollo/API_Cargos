# CARGOS Integration Service - Technical Analysis (AGENTS_TA)

## 0. Goal of this document
Translate the functional analysis in `AGENTS_FA.md` into an executable technical design for the current solution:
- single VB.NET Console application
- .NET Framework 4.7.2
- no immediate split into multiple projects

This document keeps the same functional scope and acceptance criteria, but adapts architecture, tooling, and implementation sequence to the existing codebase.

---

## 1. Current baseline and architectural constraints

## 1.1 Current codebase status
- Solution: `API_Cargos.sln`
- Project: `API_Cargos.vbproj`
- Runtime: `.NET Framework 4.7.2`
- Startup: `Module1.Main()` (currently empty)
- Config: `App.config` only
- Existing references include `System.Net.Http`
- Shared project `CommonLibrary` is already included in solution and can be reused for generic infrastructure concerns.

## 1.2 Constraint-driven implications
- Worker Service (`Microsoft.Extensions.Hosting`) is not the current hosting model.
- `HttpClientFactory` is not native in classic .NET Framework console apps; use a singleton/shared `HttpClient` instance.
- `appsettings.json` is not native configuration source; use `App.config` + environment variable overrides.
- Initial delivery should stay monolithic (single project), with clean folder/module boundaries that allow future extraction.

## 1.3 CommonLibrary usage policy
- Reuse `CommonLibrary` for common features (for example logging, email sending, DB access) when available.
- Keep CARGOS-specific orchestration and domain logic inside `API_Cargos`.
- Do not modify `CommonLibrary` unless strictly needed and only for behavior that is clearly generic and reusable by multiple projects.

---

## 2. Scope mapping (FA -> TA)

The FA requirements remain unchanged. Technical adaptation:
- Keep all logic in the current project but organize by domain boundaries.
- Implement orchestration as a long-running single-instance loop inside the console process.
- Prefer DB-driven sync/enqueue: app calls SQL procedure `Cargos_Sync_Contratti_Frontiera` at run start.
- The host performs one processor cycle, sleeps for a short interval, and stops at configured cutoff time.
- Optional startup step: sync local CaRGOS reference tables from `api/Tabella` before entering the loop.
- SQL Agent can execute the same procedure independently only as optional operational fallback.
- Optional evolution path: keep code host-agnostic so it can later move to Windows Service/Worker with minimal refactor.

---

## 3. Proposed monolithic architecture inside current project

## 3.1 Folder/module layout
Suggested structure inside `API_Cargos` project:

```text
/API_Cargos
  /Contracts
    ContractDto.vb
    DriverDto.vb
    BranchDto.vb
    CargosInputDto.vb
  /Domain
    CargosStatus.vb
    ValidationResult.vb
    ValidationError.vb
    ErrorCategory.vb
    RetryPolicy.vb
  /Integration
    CargosClient.vb
    TokenProvider.vb
    CryptoService.vb
    RecordBuilder.vb
    FieldSpec.vb
    CargosResponseModels.vb
  /Validation
    ValidationService.vb
    MandatoryRulesProvider.vb
  /Notifications
    EmailService.vb
    EmailTemplates.vb
    NotificationPolicy.vb
  /Persistence
    ISyncRepository.vb
    SqlSyncRepository.vb
    ICargosContrattiFrontieraRepository.vb
    SqlCargosContrattiFrontieraRepository.vb
    OutboxRecord.vb
  /Orchestration
    CargosProcessor.vb
    BatchCoordinator.vb
  /Infrastructure
    AppSettings.vb
    Logger.vb
    Clock.vb
  Module1.vb
  App.config
```

This keeps a single deployable EXE while preserving responsibilities.

## 3.2 Main execution model
Preferred mode: long-running single-instance process.
1. Start process.
2. Load/validate configuration.
3. Enter loop while service status is active and local time is before configured cutoff (for example 22:00).
4. Inside each cycle:
   - execute sync procedure `Cargos_Sync_Contratti_Frontiera`;
   - fetch eligible outbox records from `Cargos_Contratti_Frontiera`;
   - validate mandatory/conditional data;
   - notify branch on missing mandatory fields;
   - build fixed-width record lines;
   - batch by 100;
   - optional `/api/Check`;
   - `/api/Send`;
   - persist per-record outcomes.
5. Sleep short interval between cycles (for example 10 seconds).
6. Exit gracefully when cutoff time is reached or service status becomes false.

Operational rule: only one active instance is allowed.

---

## 4. Component-level technical analysis

## 4.1 Data contracts
Define normalized DTOs independent from DB raw schema to avoid tight coupling.
Minimum objects:
- `CargosViewContractDto` (raw row extracted from `Cargos_Vista_Contratti`)
- `ContractDto`
- `DriverDto` (primary)
- `SecondDriverDto` (optional)
- `BranchDto`
- `CargosInputDto` (flattened view for validation/builder)

Key design rule: all data transformations happen before `RecordBuilder`.
Extraction eligibility rule: process only rows from `Cargos_Vista_Contratti`, which already filters signed contracts and delivered line.

## 4.2 Outbox and idempotency
Use table `Cargos_Contratti_Frontiera` as single source for processing state.

### Snapshot state table (`Cargos_Contratti`)
Keep one row per contract-line as the latest extracted state from `Cargos_Vista_Contratti`.
Core fields:
- `ContractNo` + `ContractLineNo` (unique key)
- `CargosContractId`
- `BranchId`
- all mandatory CaRGOS payload fields
- `DateFingerprint` (normalized hash from checkin/checkout)
- `PayloadFingerprint` (normalized hash from mandatory payload fields used by validation/record build)
- `LastQueuedFingerprint`
- `LastQueuedAt`
- `LastSeenAt`

### Outbox table (`Cargos_Contratti_Frontiera`)
Core fields:
- `ContractNo` + `ContractLineNo`
- `CargosContractId`
- `BranchId`
- same mandatory payload columns copied from snapshot at enqueue time
- `Reason` (`INITIAL_SEND` | `DATE_CHANGE` | `DATA_FIX`)
- `SnapshotHash`
- `Status`
- `MissingFields`
- `LastError`
- `TransactionId`
- `AttemptCount`
- `LastAttemptAt`
- `NextRetryAt`
- `LastMissingEmailAt`
- `LastMissingFieldsHash`
- `LastRejectEmailAt`
- `LastRejectHash`
- `CreatedAt`
- `UpdatedAt`

### Reference table cache
- `Cargos_Tabella`
  - one row per CaRGOS coding table synced from `api/Tabella`
  - stores sync status, last sync time, row count, and last error
- `Cargos_Tabella_Righe`
  - generic cached rows for each coding table
  - stores `TableId`, `RowNumber`, `Code`, `Description`, extra columns, and raw line

### Lookup service
- Add app lookup service on top of `Cargos_Tabella_Righe`.
- Purpose:
  - if the view already provides a valid CaRGOS code, keep it unchanged;
  - if the view provides a local/business value instead, resolve it to the correct cached CaRGOS code.
- For agency bootstrap, prefer structured location fields over one raw free-text value:
  - `AgenziaCity`
  - `AgenziaCounty`
  - `AgenziaPostCode`
- Resolution priority for `AGENZIA_LUOGO_COD`:
  1) direct code match;
  2) `city + county`;
  3) `CAP` as tie-breaker.
- Initial usage:
  - `LUOGHI` fields
  - `TIPO_VEICOLO`
  - `TIPO_DOCUMENTO`
  - `TIPO_PAGAMENTO`

### Idempotency constraints
- Unique key on `(ContractNo, ContractLineNo, SnapshotHash)` in `Cargos_Contratti_Frontiera`.
- Records with `SENT_OK` are never eligible again for the same snapshot.
- Selection query should process only the latest pending/retry snapshot per contract to avoid sending obsolete data.
- Eligibility predicate:
  - status in `PENDING`, `READY_TO_SEND`, `SENT_KO_RETRY`
  - `NextRetryAt IS NULL OR NextRetryAt <= NOW`
- `CHECK_OK` is claimable only when `CheckOnly=False`.
- `SnapshotHash` should represent the full queue-triggering snapshot, not only checkin/checkout, so the system can requeue a contract after a data fix even if dates stay unchanged.

### CaRGOS fields to send (source: P2 tracciato record)
The following matrix must be tracked in TA and used as source for view mapping + validation.

| # | Field | Mandatory | Notes |
|---|---|---|---|
| 0 | `CONTRATTO_ID` | Yes | |
| 1 | `CONTRATTO_DATA` | Yes | |
| 2 | `CONTRATTO_TIPOP` | Yes | |
| 3 | `CONTRATTO_CHECKOUT_DATA` | Yes | |
| 4 | `CONTRATTO_CHECKOUT_LUOGO_COD` | Yes | |
| 5 | `CONTRATTO_CHECKOUT_INDIRIZZO` | Yes | |
| 6 | `CONTRATTO_CHECKIN_DATA` | Yes | |
| 7 | `CONTRATTO_CHECKIN_LUOGO_COD` | Yes | |
| 8 | `CONTRATTO_CHECKIN_INDIRIZZO` | Yes | |
| 9 | `OPERATORE_ID` | Yes | |
| 10 | `AGENZIA_ID` | Yes | |
| 11 | `AGENZIA_NOME` | Yes | |
| 12 | `AGENZIA_LUOGO_COD` | Yes | |
| 13 | `AGENZIA_INDIRIZZO` | Yes | |
| 14 | `AGENZIA_RECAPITO_TEL` | Yes | |
| 15 | `VEICOLO_TIPO` | Yes | |
| 16 | `VEICOLO_MARCA` | Yes | |
| 17 | `VEICOLO_MODELLO` | Yes | |
| 18 | `VEICOLO_TARGA` | Yes | |
| 19 | `CONDUCENTE_CONTRAENTE_QUALIFICA` | No | optional |
| 20 | `CONDUCENTE_CONTRAENTE_RUOLO` | No | optional |
| 21 | `CONDUCENTE_CONTRAENTE_ESTERONASCITA_STATOCOD` | No | optional |
| 22 | `CONDUCENTE_CONTRAENTE_COGNOME` | Yes | |
| 23 | `CONDUCENTE_CONTRAENTE_NOME` | Yes | |
| 24 | `CONDUCENTE_CONTRAENTE_NASCITA_DATA` | Yes | |
| 25 | `CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD` | Yes | |
| 26 | `CONDUCENTE_CONTRAENTE_CITTADINANZA_COD` | Yes | |
| 27 | `CONDUCENTE_CONTRAENTE_RESIDENZA_LUOGO_COD` | No | conditional with field 28 |
| 28 | `CONDUCENTE_CONTRAENTE_RESIDENZA_INDIRIZZO` | No | required if field 27 is present |
| 29 | `CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD` | Yes | |
| 30 | `CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO` | Yes | |
| 31 | `CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD` | Yes | |
| 32 | `CONDUCENTE_CONTRAENTE_PATENTE_NUMERO` | Yes | |
| 33 | `CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD` | Yes | |
| 34 | `CONDUCENTE_CONTRAENTE_RECAPITO_TEL` | No | optional |
| 35 | `SECONDO_CONDUCENTE_CONTRAENTE_QUALIFICA` | No | second-driver block, all-or-nothing |
| 36 | `SECONDO_CONDUCENTE_CONTRAENTE_RUOLO` | No | second-driver block, all-or-nothing |
| 37 | `SECONDO_CONDUCENTE_CONTRAENTE_COGNOME` | No | second-driver block, all-or-nothing |
| 38 | `SECONDO_CONDUCENTE_CONTRAENTE_NOME` | No | second-driver block, all-or-nothing |
| 39 | `SECONDO_CONDUCENTE_CONTRAENTE_NASCITA_DATA` | No | second-driver block, all-or-nothing |
| 40 | `SECONDO_CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD` | No | second-driver block, all-or-nothing |
| 41 | `SECONDO_CONDUCENTE_CONTRAENTE_CITTADINANZA_COD` | No | second-driver block, all-or-nothing |
| 42 | `SECONDO_CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD` | No | second-driver block, all-or-nothing |
| 43 | `SECONDO_CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO` | No | second-driver block, all-or-nothing |
| 44 | `SECONDO_CONDUCENTE_CONTRAENTE_PATENTE_NUMERO` | No | second-driver block, all-or-nothing |
| 45 | `SECONDO_CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD` | No | second-driver block, all-or-nothing |

### Alternative approaches considered
- Pure application sync (read view + compare + enqueue in VB.NET).
  - Works, but duplicates set-based DB logic and increases app complexity.
- SQL Agent-only sync (procedure scheduled outside app).
  - Works, but adds a second scheduler and can drift from app run cadence.
- Selected for this project: app-triggered SQL sync procedure (`Cargos_Sync_Contratti_Frontiera`) + app API pipeline.

## 4.3 Status model and transitions
Statuses:
- `PENDING`
- `MISSING_DATA`
- `READY_TO_SEND`
- `CHECK_OK`
- `SENT_OK`
- `SENT_KO_RETRY`
- `SENT_KO_DATA`

Transition rules:
- `PENDING -> MISSING_DATA` on validation failure.
- `PENDING -> READY_TO_SEND` on valid record.
- `READY_TO_SEND -> CHECK_OK` when `CheckOnly=True` and CaRGOS check succeeds.
- `CHECK_OK -> SENT_OK` when `CheckOnly=False` and the same row is later sent successfully.
- `READY_TO_SEND -> SENT_OK` on per-line success.
- `READY_TO_SEND -> SENT_KO_DATA` on per-line data rejection.
- `READY_TO_SEND -> SENT_KO_RETRY` on transport/system failure.
- `SENT_KO_RETRY -> READY_TO_SEND` when retry time arrives and data still valid.
- `MISSING_DATA -> READY_TO_SEND` when missing data resolved.
- `SENT_KO_DATA -> READY_TO_SEND` when rejected payload data is corrected.

## 4.4 Mandatory validation engine
Validation pipeline:
1. SQL/view prevalidation for cheap deterministic checks:
   - obvious mandatory-field presence from source DB;
   - basic data availability checks;
   - optional precomputed missing-fields metadata for logging/diagnostics.
2. App validation as authoritative final gate:
   - mandatory fields presence (from official CaRGOS spec mapping);
   - conditional mandatory rules;
   - format rules (dates, numeric patterns, max length);
   - second-driver "all or nothing" consistency.

Output object:
- `IsValid As Boolean`
- `MissingFields As List(Of String)`
- `Errors As List(Of String)`

Implementation detail:
- Keep field metadata in a structured mapping file (JSON/CSV) versioned in repository.
- Separate field metadata from business rules to simplify updates when police record layout changes.
- SQL/view validation reduces noise and queue volume, but it must never replace the app validation step.

## 4.5 RecordBuilder (fixed-width 1505)
Requirements:
- exact length: `1505`
- fill with spaces
- 1-based `DAL..AL` mapping
- truncation if over max length
- deterministic output

Algorithm:
- initialize `Char()` buffer with 1505 spaces
- for each `FieldSpec`
  - format source value
  - enforce max length
  - copy into `(StartPos - 1)` offset
- return `New String(buffer)`

Validation guard:
- throw/return error if a spec exceeds bounds `1..1505`
- enforce non-overlapping specs at startup

## 4.6 CryptoService
Required by CaRGOS flow:
- AES CBC
- PKCS7 padding
- key = first 32 chars of `ApiKey`
- iv = next 16 chars of `ApiKey`
- output Base64

Startup precondition:
- fail fast if `ApiKey.Length < 48`

Security rule:
- never log raw token, encrypted token, or API key.

## 4.7 TokenProvider
Flow:
1. GET `/api/Token` with Basic auth (`Username:Password`).
2. Parse `access_token` and expiry.
3. Cache in memory.
4. Refresh if missing or expiring within 2 minutes.

Concurrency guard:
- use lock/sync object to avoid token stampede under parallel requests.

Failure behavior:
- classify as technical error and schedule retry.

## 4.8 CargosClient
Methods:
- `CheckAsync(lines As List(Of String))`
- `SendAsync(lines As List(Of String))`

Transport details:
- single shared `HttpClient` with timeout 30-60s
- headers on Check/Send:
  - `Authorization: Bearer <encrypted_token>`
  - `Organization: <username>`
  - `Content-Type: application/json`

Payload:
- JSON array of strings (`["line1", "line2"]`)

Response mapping:
- preserve positional correlation between request line index and response item.
- if API does not guarantee order, include local correlation strategy (e.g., line hash map).

## 4.9 Error classification
Categories:
- `DataError`:
  - validation failures before call
  - CaRGOS per-line rejections (4xx-like data semantics)
- `TechnicalError`:
  - timeout
  - DNS/network failure
  - 5xx
  - temporary auth/token endpoint issues
- `UnexpectedError`:
  - parsing/serialization defects, mapping bugs

Status mapping:
- `DataError` -> `MISSING_DATA` or `SENT_KO_DATA`
- `TechnicalError` -> `SENT_KO_RETRY`
- `UnexpectedError` -> `SENT_KO_RETRY` + high-priority logging

## 4.10 Retry policy
Exponential-ish schedule from FA:
- attempt 1: +5 min
- attempt 2: +15 min
- attempt 3: +60 min
- attempt 4+: +240 min (or configurable)
- cap attempts at 5 then keep `SENT_KO_RETRY` with long interval/manual review flag

Store:
- increment `AttemptCount`
- set `LastAttemptAt`
- compute `NextRetryAt`

## 4.11 Notifications and anti-spam
Triggers:
1. Missing mandatory data (`MISSING_DATA`)
2. CaRGOS data rejection (`SENT_KO_DATA`)

Anti-spam policy:
- send max one email/24h per contract per scenario unless detail hash changed.
- hash basis:
  - missing data: ordered `MissingFields`
  - rejection: ordered error codes/messages

Delivery mechanism:
- SMTP via `System.Net.Mail` (native) or MailKit (NuGet). For initial phase, native SMTP is sufficient.

---

## 5. Configuration model for .NET Framework console

Use `App.config` (`<connectionStrings>` + `<appSettings>`) as base, then override from environment variables.

Required keys:
- `ConnectionStrings:CargosDb` or fallback `Db.ConnectionString`
- `Db.ContractsViewName` (default `Cargos_Vista_Contratti`, primarily used by SQL sync logic)
- `Db.ContractsSyncProcedure` (default `Cargos_Sync_Contratti_Frontiera`)
- `Db.CommandTimeoutSeconds`
- `Worker.BatchSize`
- `Worker.SleepMilliseconds`
- `Worker.CutoffHour`
- `Worker.ClaimTimeoutMinutes`
- `Worker.DryRun`
- `Diagnostics.RunSelfTests`
- `Cargos.BaseUrl`
- `Cargos.TokenPath`
- `Cargos.CheckPath`
- `Cargos.TabellaPath`
- `Cargos.SendPath`
- `Cargos.Username`
- `Cargos.Password`
- `Cargos.ApiKey`
- `Cargos.Organization`
- `Cargos.HttpTimeoutSeconds`
- `Cargos.UseCheckEndpoint`
- `Cargos.CheckOnly`
- `Cargos.SyncTablesOnStartup`
- `Cargos.FailStartupIfTableSyncFails`
- `CargosWeb.BaseUrl`
- `CargosWeb.LoginPath`
- `CargosWeb.AgencyCreatePath`
- `CargosWeb.Username`
- `CargosWeb.Password`
- `CargosWeb.AuthCookieHeader`
- `CargosWeb.VerifyTokenField`
- `CargosWeb.LoginUsernameField`
- `CargosWeb.LoginPasswordField`
- `CargosWeb.SyncAgenciesOnStartup`
- `CargosWeb.FailStartupIfAgencySyncFails`
- `Email.SmtpHost`
- `Email.SmtpPort`
- `Email.User`
- `Email.Password`
- `Email.From`
- `Email.EnableSsl`
- `Email.CooldownHours`

Operational recommendation:
- in production, set secrets through environment variables or secure config provider.

Startup validation:
- fail process if mandatory settings are missing/invalid.

---

## 6. Logging and observability

Minimum logging events:
- run start/end + correlation id
- contracts fetched count
- validation failures + missing fields (field names only)
- batch send/check start/end
- per-line result (transaction id or error code)
- retry scheduling decisions
- notification sent/skipped (anti-spam reason)

Do not log:
- credentials
- API key
- access token
- full 1505 record lines in production

Correlation:
- generate one batch correlation id and propagate through all logs and DB updates.

---

## 7. Technical flow details (end-to-end)

## 7.1 One batch processing flow
1. `SyncRepository.ExecuteProcedure("Cargos_Sync_Contratti_Frontiera")`
2. Procedure responsibilities:
   - read `Cargos_Vista_Contratti` (signed + delivered line)
   - upsert `Cargos_Contratti`
   - enqueue `PENDING` rows in `Cargos_Contratti_Frontiera` with:
      - `Reason = INITIAL_SEND` for first occurrence
      - `Reason = DATE_CHANGE` for changed checkin/checkout
      - `Reason = DATA_FIX` for payload corrections on rows previously blocked/rejected
3. `OutboxRepository.GetEligible()` from `Cargos_Contratti_Frontiera`.
4. Build domain input DTOs.
5. Apply SQL/view prevalidation metadata if available.
6. Resolve coded fields through local lookup service if the view did not already provide CaRGOS codes.
7. `ValidationService.Validate()` per contract as final gate.
8. Invalid:
   - update status `MISSING_DATA`
   - apply notification policy and email if allowed
9. Valid:
   - status `READY_TO_SEND`
   - build 1505 line
10. Chunk valid lines into batches of 100.
11. If `UseCheckEndpoint=True`: call Check and handle data errors.
12. If `CheckOnly=True`: stop after successful Check and persist `CHECK_OK`.
13. Otherwise call Send for remaining lines.
14. Parse per-line outcome:
    - success -> `SENT_OK` + `TransactionId`
    - data reject -> `SENT_KO_DATA` + email policy
15. On technical failure of whole call:
    - mark impacted records as `SENT_KO_RETRY`
    - schedule `NextRetryAt`

## 7.1B Service loop
1. Host starts once.
2. Optional startup sync of local `api/Tabella` caches.
3. Execute one processor cycle.
4. Sleep configured interval (for example 10 seconds).
5. Repeat while service status is true and local time is before cutoff.
6. Prevent overlapping instances through single-instance guard.

## 7.2 Duplicate-send prevention
Before send, enforce:
- status eligibility
- not already `SENT_OK`
- no duplicate `(ContractNo, ContractLineNo, SnapshotHash)` in queue
- worker claim/reservation step before send
- do not pick an older retry row if a newer snapshot already exists for the same contract-line
- optional optimistic concurrency with `UpdatedAt`/row version

---

## 8. Database analysis (reference SQL)

Reference implementation is tracked in `sql/Cargos_Setup.sql`.
Key DB shape:

```sql
CREATE TABLE dbo.Cargos_Contratti (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ContractNo NVARCHAR(50) NOT NULL,
    ContractLineNo BIGINT NOT NULL,
    CargosContractId NVARCHAR(50) NOT NULL,
    BranchId NVARCHAR(50) NOT NULL,
    -- mandatory CaRGOS payload columns (Contratto*, OperatoreId, Agenzia*, Veicolo*, Conducente*)
    ContrattoCheckinData DATETIME2 NULL,
    ContrattoCheckoutData DATETIME2 NULL,
    DateFingerprint NVARCHAR(128) NOT NULL,
    PayloadFingerprint NVARCHAR(128) NOT NULL,
    LastQueuedFingerprint NVARCHAR(128) NULL,
    LastQueuedAt DATETIME2 NULL,
    LastSeenAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT UQ_Cargos_Contratti_ContractLine UNIQUE (ContractNo, ContractLineNo)
);

CREATE TABLE dbo.Cargos_Contratti_Frontiera (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ContractNo NVARCHAR(50) NOT NULL,
    ContractLineNo BIGINT NOT NULL,
    CargosContractId NVARCHAR(50) NOT NULL,
    BranchId NVARCHAR(50) NOT NULL,
    -- same mandatory CaRGOS payload columns snapshot
    Reason NVARCHAR(30) NOT NULL, -- INITIAL_SEND | DATE_CHANGE | DATA_FIX
    SnapshotHash NVARCHAR(128) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    MissingFields NVARCHAR(MAX) NULL,
    LastError NVARCHAR(MAX) NULL,
    TransactionId NVARCHAR(100) NULL,
    AttemptCount INT NOT NULL CONSTRAINT DF_Cargos_Contratti_Frontiera_AttemptCount DEFAULT (0),
    LastAttemptAt DATETIME2 NULL,
    NextRetryAt DATETIME2 NULL,
    LastMissingEmailAt DATETIME2 NULL,
    LastMissingFieldsHash NVARCHAR(128) NULL,
    LastRejectEmailAt DATETIME2 NULL,
    LastRejectHash NVARCHAR(128) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE UNIQUE INDEX UQ_Cargos_Contratti_Frontiera_Snapshot
ON dbo.Cargos_Contratti_Frontiera(ContractNo, ContractLineNo, SnapshotHash);

CREATE INDEX IX_Cargos_Contratti_Frontiera_StatusRetry
ON dbo.Cargos_Contratti_Frontiera(Status, NextRetryAt);
```

Notes:
- If existing DB standards require snake_case or different PK type, adapt naming only; behavior must stay identical.
- Naming convention for new CARGOS tables: always use the `Cargos_` prefix.
- Normalize `ContrattoCheckinData` and `ContrattoCheckoutData` before hashing/comparing (same timezone and precision) to avoid false change detection.

---

## 9. Testing strategy adapted to current solution

## 9.1 Unit tests (high priority)
- `RecordBuilderTests`
  - length always 1505
  - position placement correctness
  - truncation and padding
- `CryptoServiceTests`
  - known vector consistency
  - key/iv extraction checks
- `ValidationServiceTests`
  - missing mandatory detection
  - conditional rule behavior
  - second driver all-or-nothing
- `ResponseMappingTests`
  - API item -> outbox row mapping

## 9.2 Integration tests (phase 2)
- mock HTTP server for Token/Check/Send
- verify headers and payload format
- verify retry classification

## 9.3 Non-functional checks
- idempotency under rerun
- anti-spam behavior over 24h boundary
- timeout handling and retry scheduling

---

## 10. Recommended implementation sequence in this repository

## Phase A - Foundation in current console
- Add folder structure and core models.
- Add config loader and startup validation.
- Add repositories for sync-procedure execution and `Cargos_Contratti_Frontiera`.

## Phase B - Core CaRGOS integration
- Implement `CryptoService`.
- Implement `TokenProvider`.
- Implement `CargosClient` with shared `HttpClient`.
- Implement `RecordBuilder` with field-spec mapping.

## Phase C - Validation and notifications
- Implement rules provider from CaRGOS field list.
- Implement `ValidationService`.
- Implement `EmailService` + anti-spam logic.

## Phase D - Orchestration and retries
- Implement orchestration startup step: execute `Cargos_Sync_Contratti_Frontiera`.
- Implement processor pipeline and batching.
- Add optional Check call.
- Add technical retry scheduler logic.

## Phase E - Hardening
- Add full logs with correlation id.
- Add tests and edge-case handling.
- Prepare operational runbook (Task Scheduler, config, secrets).

## Priority plan for current codebase
1. Fix queue processing correctness.
   - Add worker claim/reservation for eligible rows.
   - Ensure selection ignores obsolete retries when a newer snapshot exists.
   - Keep single-instance runtime as a hard operational rule.
2. Implement dual validation end to end.
   - Add SQL/view prevalidation output.
   - Add app `ValidationService`.
   - Persist `MISSING_DATA`, `MissingFields`, and notification anti-spam fields correctly.
3. Implement payload-correction reprocessing.
   - Introduce payload-based fingerprint/change detection in addition to date-based detection.
   - Enqueue `DATA_FIX` rows when blocked/rejected payload is corrected without date changes.
4. Implement app-side `RecordBuilder`.
   - Build the 1505-char line in app from normalized payload data.
   - Keep optional DB `RecordLine` only as diagnostic/supporting data, not as the primary runtime dependency.
5. Complete API pipeline semantics.
   - Add `/api/Check` support and config toggle.
   - Improve response/error classification, especially auth-related and transient 4xx/5xx cases.
6. Implement notification flow.
   - Missing-data email.
   - CaRGOS rejection email.
   - 24h anti-spam/hash logic.
7. Introduce long-running host loop.
   - Add outer loop with sleep and 22:00 cutoff.
   - Keep graceful stop behavior and top-level fatal logging.
8. Add tests and operational hardening.
   - unit tests for validation, record build, crypto, response mapping;
   - integration tests for token/check/send;
   - observability and runbook.

## Tracked implemented updates (as of 2026-03-06)
- [x] Added `sql/Cargos_Setup.sql` as single DB deployment script.
- [x] Renamed queue naming to `Cargos_Contratti_Frontiera` and aligned project code.
- [x] Renamed identity key `ContractId` to `ContractNo`.
- [x] Added `ContractLineNo` and switched uniqueness/idempotency to contract-line.
- [x] Added mandatory CaRGOS payload columns in both `Cargos_Contratti` and `Cargos_Contratti_Frontiera`.
- [x] Updated sync procedure to ingest mandatory payload fields from `Cargos_Vista_Contratti`.
- [x] Implemented real token + send pipeline with status transitions (`SENT_OK`, `SENT_KO_DATA`, `SENT_KO_RETRY`).
- [x] Confirmed policy to reuse `CommonLibrary` for generic concerns and avoid changes unless strictly generic.
- [x] Added claim/reservation-based outbox fetch model to support single-instance safe processing.
- [x] Added app-side `ValidationService`, `RecordBuilder`, and notification services.
- [x] Added `/api/Check` client support and improved HTTP error classification.
- [x] Added long-running outer host loop with single-instance mutex and cutoff-hour stop.
- [x] Added self-test mode for record generation, validation, and crypto smoke checks.
- [x] Added startup sync service and SQL cache tables for `api/Tabella` reference data.
- [x] Added `Cargos.SyncTablesOnStartup` and `Cargos.FailStartupIfTableSyncFails` startup controls.
- [x] Added lookup service on top of `Cargos_Tabella_Righe` to resolve business values to CaRGOS codes.
- [x] Added agency bootstrap pipeline for `CARGOS_WEB/Agenzia/Create`.
- [x] Added SQL tracking tables `Cargos_Agenzie` and `Cargos_Agenzie_Frontiera`.
- [x] Added `CargosWeb.*` settings for web auth and startup agency load.
- [x] Added structured agency luogo resolution using `AgenziaCity`, `AgenziaCounty`, and `AgenziaPostCode`.

---

## 11. Risk analysis and mitigations

1. Risk: official field specs are updated by CaRGOS.
- Mitigation: externalize field specs in versioned mapping file and add startup validation.

2. Risk: response format ambiguity (order/correlation).
- Mitigation: implement strict mapping strategy and add integration tests with out-of-order responses.

3. Risk: duplicate sends during concurrent executions.
- Mitigation: unique constraint + transactional status update + single-instance scheduling.

4. Risk: noisy branch notifications.
- Mitigation: anti-spam hash + 24h cooldown persisted in outbox.

5. Risk: secrets leakage in logs/config.
- Mitigation: redaction policy + environment-variable overrides.

6. Risk: false re-send due to datetime precision/timezone mismatch.
- Mitigation: normalize checkin/checkout datetimes before comparison and use deterministic snapshot hash.

7. Risk: sync procedure not executed before send cycle.
- Mitigation: make sync-procedure execution a mandatory first step in app run; fail fast if execution fails.

---

## 12. Definition of done (technical)
A release is technically complete when:
- all FA acceptance criteria are met in the current console architecture;
- record generation is deterministic and always 1505 chars;
- token/encryption flow works against CaRGOS sandbox/official endpoint;
- statuses and retries are persisted and auditable;
- notification anti-spam rule is enforced;
- duplicate send prevention is guaranteed by both logic and DB constraints;
- extraction is driven by `Cargos_Vista_Contratti` (signed + delivered line scope);
- each app cycle starts by executing `Cargos_Sync_Contratti_Frontiera`;
- checkin/checkout changes generate a new `Cargos_Contratti_Frontiera` queue item and a new send attempt;
- unit tests cover core algorithms and error mapping.

---

## 13. Future migration path (optional)
When needed, the monolith can be split into the multi-project architecture from FA section 6 by moving folders into class libraries with minimal code changes. The core interfaces should therefore be designed now to keep that path low-risk.

END OF TECHNICAL ANALYSIS

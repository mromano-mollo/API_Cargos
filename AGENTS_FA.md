# CARGOS Integration Service (VB.NET) → Functional + Technical Specification (Codex Friendly)

## 0. Purpose
Build a background service that, for every rental contract ("nolo"), sends a POST call to the Italian State Police CaRGOS API using only **mandatory fields** initially.  
If mandatory data is missing, the service must **not** call CaRGOS and must **email the branch** responsible for the contract.

This document is written to enable an AI coding agent (Codex) to implement the solution autonomously.

---

## 1. References
- CaRGOS API portal: https://cargos.poliziadistato.it/CARGOS_API/
- Manual P1 (API methods + record layout): https://cargos.poliziadistato.it/CARGOS_API/man/P1_CaRGOS_API_Pubblicazione_3.2.pdf
- Manual P2 (record fields with DAL/AL/mandatory flag): https://cargos.poliziadistato.it/CARGOS_API/man/P2_CaRGOS_API_Esempio_Cifre_3.2.pdf

> IMPORTANT: Mandatory/optional fields and record positions are defined by official CaRGOS "tracciato record" (DAL/AL).  
> The implementation must follow P2 exactly and keep field metadata tracked in repository docs.

---

## 2. Glossary
- **Contract**: A rental contract created in our system (DB).
- **Branch**: The contract's branch (filiale) determined by location/ubication; used for notifications.
- **CaRGOS**: Police service receiving rental driver registrations.
- **Record line**: A single fixed-width string representing one contract, length **1505** chars.
- **Batch**: A single API call containing up to **100** record lines.

---

## 3. High-level Functional Requirements

### FR-01 → Process contracts
- Input source is DB view `Cargos_Vista_Contratti`.
- The view must contain only contracts in signed status and delivered line.
- The service receives a list of "contracts-to-send" with all necessary domain fields and the branch identifier.

### FR-02 → Validate mandatory fields (before calling CaRGOS)
- The service must validate that all CaRGOS fields marked **mandatory** are present.
- A first validation layer can run in SQL/view logic to detect obvious missing data early and reduce queue noise.
- The application remains the **authoritative final validator** before any `Check` or `Send` call.
- Conditional mandatory rules must be applied (examples from CaRGOS docs):
  - If "residence place" is provided, then "residence address" becomes required.
  - Second driver: either provide all required second-driver fields OR do not provide the second driver at all.

### FR-03 → Missing mandatory data â†’ notify branch
- If validation fails:
  - Do not call CaRGOS.
  - Mark contract status as `MISSING_DATA`.
  - Send an email to the contract's branch with:
    - Contract identifier(s)
    - List of missing fields (using CaRGOS field names)
    - Optional: suggested correction steps

### FR-04 → Build CaRGOS record lines
- For validated contracts, build a fixed-width record line of length **1505** using the CaRGOS field positions (DAL/AL).
- Initially send **mandatory fields only** (fields marked optional should be blank unless required by conditional rules).

### FR-05 → Send to CaRGOS
- Send record lines to CaRGOS in batches of max **100** lines per call.
- Parse response per record line and store result:
  - If success: store `transactionid` and mark `SENT_OK`.
  - If data error: mark `SENT_KO_DATA` and notify branch with CaRGOS error details.
  - If technical error: mark `SENT_KO_RETRY` (retry later).

### FR-06 → Idempotency / No duplicate sending
- The service must not send the same contract snapshot twice.
- Implement internal tracking tables where queue idempotency is keyed by `ContractNo + ContractLineNo + SnapshotHash`.

### FR-07 - Re-send when contract date fields change
- If `CONTRATTO_CHECKIN_DATA` or `CONTRATTO_CHECKOUT_DATA` changes for an already processed contract, the service must send a new call to CaRGOS with updated data.
- The new call must be tracked as a new queue item, while preserving history of previous attempts/results.

### FR-07B - Reprocess when data is fixed
- If a contract was previously blocked with `MISSING_DATA` or rejected with `SENT_KO_DATA`, and the mandatory payload is corrected, the service must reprocess it even when checkin/checkout dates did not change.
- Reprocessing after a data correction must be tracked as a new queue item, preserving prior history.

### FR-08 - Extraction + snapshot + enqueue flow
- At each processing cycle:
  1) execute sync procedure `Cargos_Sync_Contratti_Frontiera`;
  2) procedure extracts current eligible contracts from `Cargos_Vista_Contratti`;
  3) procedure upserts contract snapshot state in internal table `Cargos_Contratti`;
  4) if a new contract is found, checkin/checkout values changed, or blocked/rejected payload data was corrected, procedure inserts a new pending item into `Cargos_Contratti_Frontiera`.

---

## 4. Non-goals (initial phase)
- Full support of optional CaRGOS fields (only mandatory fields first).
- UI changes / user interface.
- Real-time sending from frontend; this is a backend worker.

---

## 5. Technical Requirements

### TR-01 → Platform & project type
- VB.NET solution
- Recommended operational mode: long-running single-instance process (console or Windows Service style host).
- The process can execute the worker cycle in a loop with a short sleep between cycles and stop at a configured daily cutoff time.

### TR-02 → HTTP client usage
- Use HttpClientFactory (or a single shared HttpClient instance) to avoid socket exhaustion.
- Apply sensible timeouts (e.g., 30–60s) and classify timeout as technical error.

### TR-03 → Authentication flow (CaRGOS)
The service must implement the official CaRGOS token flow:

1) **GET** `/api/Token` with **Basic Auth** using CaRGOS Username/Password  
2) Receive `access_token` + expiration  
3) Encrypt `access_token` using **AES CBC + PKCS7 padding** with key/iv derived from APIKEY:
   - key = first 32 characters of APIKEY
   - iv  = next 16 characters of APIKEY
   - output must be Base64
4) Use encrypted token as `Authorization: Bearer <encrypted_token>`
5) Add header: `Organization: <USERNAME>`

### TR-04 → Request payload format
For `Check` and `Send`, the body is:
- JSON array of strings: `["<recordLine1>", "<recordLine2>", ...]`

### TR-05 → Batch size limit
- Maximum 100 record lines per request.

### TR-06 → Optional pre-validation via CaRGOS Check endpoint
- Support calling `/api/Check` before `/api/Send`.
- Default behavior can be:
  - DEV/TEST: `Check` then `Send`
  - PROD: optional toggle, but recommended for troubleshooting
- Support a `CheckOnly` mode: when enabled, call only `/api/Check` and do not execute `/api/Send`.

### TR-07 - Reuse `CommonLibrary` for shared concerns
- `CommonLibrary` can be used for common capabilities already available in solution (for example logging, email sending, DB access).
- Prefer using existing `CommonLibrary` modules instead of re-implementing shared behavior in CARGOS projects.
- Avoid modifying `CommonLibrary` unless strictly needed and only for features that are truly generic and reusable outside CARGOS.

### TR-08 - Change detection strategy for checkin/checkout
- Implement an internal snapshot table `Cargos_Contratti` to store the last known values of `CONTRATTO_CHECKIN_DATA` and `CONTRATTO_CHECKOUT_DATA` per contract.
- Compare current extracted values with stored snapshot using normalized datetime values (consistent timezone and precision).
- On change detection, enqueue a new item in `Cargos_Contratti_Frontiera` with reason `DATE_CHANGE`.
- Preferred model: implement sync logic in SQL procedure `Cargos_Sync_Contratti_Frontiera` and execute it at the beginning of each app cycle.
- SQL Agent scheduling of the same procedure is optional fallback, not the primary orchestration model.

### TR-09 - Dual validation strategy
- SQL/view validation is the first layer and should handle cheap deterministic checks based on source DB data.
- App validation is mandatory and is the final gate before `Check`/`Send`.
- To support retry after data fixes, tracking must include not only date-based change detection but also payload change detection for contracts previously stuck in `MISSING_DATA` or `SENT_KO_DATA`.

### TR-10 - Long-running single-instance execution
- Preferred runtime is one active instance only.
- Each cycle should:
  1) execute sync;
  2) process up to configured batch size;
  3) sleep for a short interval (for example 10 seconds);
  4) stop after configured local cutoff time (for example 22:00).
- Overlapping instances must be prevented.

### TR-11 - Reference table sync on startup
- CaRGOS coded fields that depend on Polizia internal tables must be resolved from local cache tables populated through `api/Tabella`.
- `api/Tabella` does not return the plain `#`-separated file directly; it returns a JSON envelope with:
  - `esito`
  - `errore`
  - `filename`
  - `file` (Base64-encoded UTF-8 content to decode before parsing)
- Header skipping logic must run only on the decoded file content, not on the raw JSON response body.
- Current table-id mapping used by the app:
  - `0 = TIPO_PAGAMENTO`
  - `1 = LUOGHI` (API file currently returned as `V_COMUNI_STATI`)
  - `2 = TIPO_VEICOLO`
  - `3 = TIPO_DOCUMENTO`
- Add startup setting `Cargos.SyncTablesOnStartup`:
  - if `true`, sync local reference tables once before entering the processing loop;
  - if `false`, use the already cached local tables.
- Add startup setting `Cargos.FailStartupIfTableSyncFails`:
  - if `true`, abort startup when table sync fails;
  - if `false`, log the error and continue with the last cached data.

### TR-12 - Agency initial load through CARGOS_WEB
- Initial agency load is handled through `CARGOS_WEB/Agenzia/Create`, not through the public `CARGOS_API`.
- Current observed web login flow is two-step:
  1) GET login page `CARGOS_WEB/Login/Login`
  2) POST credentials to `CARGOS_WEB/Login/Default`
  3) user enters OTP and POSTs to `CARGOS_WEB/Login/LoginAuth`
- OTP is interactive and must be provided by the operator at runtime unless a valid pre-authenticated cookie is supplied.
- If the web session expires during agency bootstrap, the client should recreate its HTTP transport, re-authenticate, request OTP again, and retry the current agency once.
- Add startup setting `CargosWeb.SyncAgenciesOnStartup`:
  - if `true`, sync agency source rows and attempt bootstrap on CaRGOS before entering the contract loop;
  - if `false`, skip agency bootstrap.
- Add startup setting `CargosWeb.FailStartupIfAgencySyncFails`:
  - if `true`, abort startup when agency bootstrap fails;
  - if `false`, log the error and continue.
- Agency create payload must include:
  - `Agenzia.AGENZIA_ID`
  - `Agenzia.AGENZIA_NOME`
  - `Agenzia.AGENZIA_LUOGO_COD`
  - `Agenzia.AGENZIA_INDIRIZZO`
- `Agenzia.AGENZIA_RECAPITO_TEL`
- `Agenzia.AGENZIA_LUOGO_COD` must be resolved to a Polizia internal code using cached `LUOGHI` table values.
- Preferred source model for agency location is:
  - `AgenziaCity`
  - `AgenziaCounty`
  - `AgenziaPostCode`
- Resolution priority for agency luogo is:
  1) direct code if already present;
  2) `city + county`;
  3) `CAP` only as disambiguation.

---

## 6. Proposed Solution Architecture (Projects)

Create a solution with these projects (names can vary, but structure must match responsibilities):

1) **Cargos.Contracts** (Class Library)
   - DTOs representing DB extraction payload (ContractDto, DriverDto, BranchDto, etc.)

2) **Cargos.Domain** (Class Library)
   - Validation rules (mandatory/conditional)
   - Status enum/state machine
   - Error classification objects

3) **Cargos.Integration** (Class Library)
   - `CargosClient` (Token/Check/Send/Tabella)
   - `TokenProvider` with caching + refresh
   - `CryptoService` (AES encryption for token)
   - `RecordBuilder` (fixed-width line builder)
   - (Optional later) `TablesProvider` for CaRGOS "Tabella" downloads

4) **Cargos.Notifications** (Class Library)
   - Email sender service (SMTP/MailKit)
   - Email templates for missing-data & CaRGOS rejection

5) **Cargos.Worker** (Worker Service / Console)
   - Orchestrates:
     - read pending contracts (from DB extraction interface)
     - validate
     - build record lines
     - call CaRGOS
     - store outcomes
     - send notifications

6) **Cargos.Tests** (Unit Tests)
   - RecordBuilder position/length tests
   - CryptoService tests
   - Validation tests
   - Response parsing tests

---

## 7. Data Model (Outbox / Tracking)

Create a table (or equivalent storage) to track processing per contract line.
Naming convention: every new CARGOS table must start with `Cargos_` prefix.

### Reference tables
- `Cargos_Tabella`
  - stores metadata for each synced CaRGOS coding table (`LUOGHI`, `TIPO_VEICOLO`, `TIPO_DOCUMENTO`, `TIPO_PAGAMENTO`)
- `Cargos_Tabella_Righe`
  - stores cached rows downloaded from `api/Tabella`
  - generic fields: `TableId`, `RowNumber`, `Code`, `Description`, extra columns, raw line, sync timestamps

### Agency bootstrap tables
- `Cargos_Agenzie`
  - stores one current snapshot row per branch/agency source record
  - internal storage keeps `AgenziaId` at `NVARCHAR(50)` and `AgenziaLuogoValue` at `NVARCHAR(9)`
- `Cargos_Agenzie_Frontiera`
  - stores startup agency bootstrap queue and outcomes for `CARGOS_WEB/Agenzia/Create`

### Snapshot table: `Cargos_Contratti`
Purpose: keep one current state row per contract-line for change detection.
Fields:
- `Id` (PK)
- `ContractNo` (our contract number, e.g. `CTR26-xxxxxx`)
- `ContractLineNo` (contract line number)
- `CargosContractId`
- `BranchId` (optional internal metadata, not part of the CaRGOS payload)
- All mandatory CaRGOS payload fields (stored as normalized columns):
  - `ContrattoId`, `ContrattoData`, `ContrattoTipoP`
  - `ContrattoCheckoutData`, `ContrattoCheckoutLuogoCod`, `ContrattoCheckoutIndirizzo`
  - `ContrattoCheckinData`, `ContrattoCheckinLuogoCod`, `ContrattoCheckinIndirizzo`
  - `OperatoreId`
  - `AgenziaId`, `AgenziaNome`, `AgenziaLuogoCod`, `AgenziaIndirizzo`, `AgenziaRecapitoTel`
  - `VeicoloTipo`, `VeicoloMarca`, `VeicoloModello`, `VeicoloTarga`
  - `ConducenteContraenteCognome`, `ConducenteContraenteNome`
  - `ConducenteContraenteNascitaData`, `ConducenteContraenteNascitaLuogoCod`
  - `ConducenteContraenteCittadinanzaCod`
  - `ConducenteContraenteDocideTipoCod`, `ConducenteContraenteDocideNumero`, `ConducenteContraenteDocideLuogorilCod`
  - `ConducenteContraentePatenteNumero`, `ConducenteContraentePatenteLuogorilCod`
- `DateFingerprint` (hash/string built from normalized checkin/checkout)
- `PayloadFingerprint` (hash/string built only from CaRGOS payload fields used by validation/record build; excludes internal metadata like `BranchId` / `BranchEmail`)
- overdue open-rental rule: if an extracted contract is still open and its planned `CONTRATTO_CHECKIN_DATA` is before today, the sync procedure normalizes the effective check-in date to today before hashing/enqueueing
- `LastQueuedFingerprint` (hash/string of last enqueued snapshot)
- `LastQueuedAt` (datetime)
- `LastSeenAt` (datetime)
- `CreatedAt`, `UpdatedAt`

### Suggested table: `Cargos_Contratti_Frontiera`
Fields:
- `Id` (PK)
- `ContractNo`
- `ContractLineNo`
- `CargosContractId` (value used in CaRGOS record, if different)
- `BranchId` (optional internal metadata)
- Same mandatory CaRGOS payload columns listed for `Cargos_Contratti` (snapshot at queue creation time)
- `Reason` (`INITIAL_SEND` | `DATE_CHANGE` | `DATA_FIX`)
- `SnapshotHash` (hash/string of snapshot used for this send item)
- `Status` (string/enum):
  - `PENDING`
  - `MISSING_DATA`
  - `READY_TO_SEND`
  - `CHECK_OK`
  - `SENT_OK`
  - `SENT_KO_RETRY`
  - `SENT_KO_DATA`
- `MissingFields` (text/json)
- `LastError` (text)
- `TransactionId` (text)
- `AttemptCount` (int)
- `LastAttemptAt` (datetime)
- `NextRetryAt` (datetime)
- `CreatedAt`, `UpdatedAt`

### Idempotency rule
- A queue item is eligible for processing only if status in `{PENDING, READY_TO_SEND, SENT_KO_RETRY}`.
- `CHECK_OK` is a parked state used when `CheckOnly=True`; it must not be rechecked in check-only mode, but it can be picked later for real send when check-only mode is disabled.
- Prevent duplicate queue creation for same contract snapshot using unique key `(ContractNo, ContractLineNo, SnapshotHash)`.

---

## 8. Validation Rules (Mandatory Fields)

### Source of truth
- The agent must parse the CaRGOS field list on the portal and build a list of mandatory fields (marked as required).

### Implementation approach
- Create a `ValidationResult` with:
  - `IsValid`
  - `MissingFields: List(Of String)` (CaRGOS field names)
  - `Errors: List(Of String)` (format issues)
- Validation steps:
  1) Check all mandatory fields are present and non-empty.
  2) Apply conditional rules (e.g., residence logic).
  3) Validate date formats and numeric/length constraints.
  4) For second driver: validate "all or nothing" rule.

### Output behavior
- If invalid: mark outbox `MISSING_DATA` and trigger branch email.

---

## 9. Record Builder (Fixed-width 1505 chars)

### Requirements
- Output is exactly **1505 characters**.
- Each field is written into its specified position range `DAL..AL` (1-based positions from CaRGOS docs).
- Fill default with spaces.
- Apply truncation if input exceeds field length.
- Current implementation follows the official `TRACCIATO RECORD` dimension set from the CaRGOS API manual.
- The current official optional tail includes `VEICOLO_COLORE`, `VEICOLO_GPS`, `VEICOLO_BLOCCOM`, and `CONDUCENTE2_*` fields.
- Fields not supplied by our source model are still written as blanks, but their official positions and lengths are reserved in the 1505-char line.
- Apply padding rules:
  - If docs do not specify numeric left-padding, default to right-pad spaces for strings.
  - Use CaRGOS `Check` endpoint to confirm formatting; adjust padding if required.

### Implementation outline
- Create a `FieldSpec` list:
  - `Name`, `StartPos`, `EndPos`, `Length`, `IsMandatory`, `Formatter`
- Build:
  - `Dim buffer As Char() = New String(" "c, 1505).ToCharArray()`
  - For each field:
    - value = format(value)
    - place into buffer indexes `(StartPos-1)..(EndPos-1)`
- Return `New String(buffer)`.

---

## 10. CaRGOS Client (Token, Check, Send)

### Endpoints (base URL from config)
- `GET /api/Token`
- `POST /api/Check`
- `POST /api/Send`
- (Optional) `GET /api/Tabella?TabellaIdentificativo=...`

### Token Provider
- Cache token + expiration.
- Refresh if missing or expiring soon (e.g., within 2 minutes).
- Store only in memory unless multi-instance requires shared cache.

### Encryption
- AES CBC, PKCS7 padding
- key/iv derived from APIKEY as described above
- output Base64 string

### Headers for Check/Send
- `Authorization: Bearer <encrypted_token>`
- `Organization: <username>`
- `Content-Type: application/json`

### Response parsing
- Response includes an array with per-line results:
  - success: includes `transactionid`
  - error: includes details (message/code)
- Map each response item to corresponding outbox record.

---

## 11. Email Notifications

### Trigger 1: Missing mandatory data
- Subject: `CARGOS - Missing mandatory data for contract <ContractNo>/<ContractLineNo>`
- Body includes:
  - Contract reference
  - Branch reference
  - Missing CaRGOS fields list
  - Next steps: complete missing fields on contract

### Trigger 2: CaRGOS data rejection
- Subject: `CARGOS - Rejected contract <ContractNo>/<ContractLineNo>`
- Body includes:
  - Contract reference
  - CaRGOS error message(s)
  - Suggested correction steps

### Anti-spam rule
- Do not send repeated emails for the same contract more than once in 24h unless the missing fields list changed.

---

## 12. Retry Policy (Technical errors only)

### Technical error examples
- Timeout
- Network failure
- 5xx responses
- Token endpoint transient failures

### Data errors (no retry)
- 4xx validation responses from CaRGOS where the payload is rejected for data reasons.

### Retry strategy
- Exponential backoff:
  - attempt 1: +5 min
  - attempt 2: +15 min
  - attempt 3: +60 min
  - cap attempts (e.g., 5) then mark `SENT_KO_RETRY` with long delay or manual review.

---

## 13. Configuration & Secrets

Store in `App.config` (`<connectionStrings>` + `<appSettings>`) + environment overrides:
- `ConnectionStrings:CargosDb` or fallback `Db.ConnectionString`: SQL Server connection string used by the app.
- `Db.ContractsViewName` (default `Cargos_Vista_Contratti`): logical source view name used by documentation and DB design.
- `Db.ContractsSyncProcedure` (default `Cargos_Sync_Contratti_Frontiera`): stored procedure executed at each cycle to sync and enqueue contracts.
- `Db.AgenciesSyncProcedure` (default `Cargos_Sync_Agenzie_Frontiera`): stored procedure executed at startup to sync and enqueue agency bootstrap rows.
- `Db.CommandTimeoutSeconds`: SQL command timeout for repositories and sync operations.
- `Worker.BatchSize`: max number of queue rows processed in one cycle and in one outbound batch.
- `Worker.SleepMilliseconds`: pause between two processing cycles in long-running mode.
- `Worker.CutoffHour`: local hour after which the process stops for the day.
- `Worker.ClaimTimeoutMinutes`: time after which a claimed row can be reclaimed if a worker died.
- `Worker.DryRun`: if `true`, app logs claimed work without calling CaRGOS or changing real send outcomes.
- `Diagnostics.RunSelfTests`: if `true`, run internal smoke tests and exit.
- `Cargos.BaseUrl`: base URL of the official CaRGOS API.
- `Cargos.TokenPath`: relative path of the token endpoint.
- `Cargos.CheckPath`: relative path of the check endpoint.
- `Cargos.TabellaPath`: relative path of the reference table endpoint.
- `Cargos.SendPath`: relative path of the send endpoint.
- `Cargos.Username`: CaRGOS username used for authentication.
- `Cargos.Password`: CaRGOS password used for authentication.
- `Cargos.ApiKey`: CaRGOS API key used to encrypt the access token.
- `Cargos.Organization`: value sent in `Organization` header, usually aligned with username/account.
- `Cargos.HttpTimeoutSeconds`: timeout for HTTP calls to CaRGOS.
- `Cargos.UseCheckEndpoint` (bool): if `true`, call `/api/Check` before `/api/Send`.
- `Cargos.CheckOnly` (bool): if `true`, call only `/api/Check` and never `/api/Send`.
- `Cargos.SyncTablesOnStartup` (bool): if `true`, sync `api/Tabella` caches once before entering the main loop.
- `Cargos.FailStartupIfTableSyncFails` (bool): if `true`, abort startup when reference table sync fails.
- `CargosWeb.BaseUrl`: base URL of the authenticated CaRGOS web portal.
- `CargosWeb.LoginPagePath`: relative path of the login page used to fetch anti-forgery token and hidden fields.
- `CargosWeb.LoginPath`: relative path of the credentials POST endpoint.
- `CargosWeb.LoginOtpPath`: relative path of the OTP confirmation POST endpoint.
- `CargosWeb.AgencyCreatePath`: relative path of the agency create endpoint/page.
- `CargosWeb.Username`: web-portal username used for login when cookie header is not supplied.
- `CargosWeb.Password`: web-portal password used for login when cookie header is not supplied.
- `CargosWeb.AuthCookieHeader`: optional pre-authenticated cookie header value used instead of login flow.
- `CargosWeb.VerifyTokenField`: anti-forgery field name expected by the web portal.
- `CargosWeb.LoginUsernameField`: form field name used for web login username.
- `CargosWeb.LoginPasswordField`: form field name used for web login password.
- `CargosWeb.LoginAccediField`: submit field name used by the login/OTP form.
- `CargosWeb.OtpCodeField`: field name containing the OTP code in the second login step.
- `CargosWeb.SyncAgenciesOnStartup` (bool): if `true`, run the initial agency bootstrap before contract processing.
- `CargosWeb.FailStartupIfAgencySyncFails` (bool): if `true`, abort startup when agency bootstrap fails.
- `Email.SmtpHost`: SMTP server host used for notifications.
- `Email.SmtpPort`: SMTP server port.
- `Email.User`: SMTP username, if authentication is required.
- `Email.Password`: SMTP password, if authentication is required.
- `Email.From`: sender email address for all notifications.
- `Email.EnableSsl`: if `true`, enable SSL/TLS for SMTP.
- `Email.CooldownHours`: anti-spam cooldown before resending the same notification with unchanged content.

Security:
- Treat Username/Password/ApiKey/SMTP password as secrets (use environment variables or secret store).

---

## 14. Logging & Observability
Log at minimum:
- Contract processing start/end
- Validation failures + missing fields
- CaRGOS request/response metadata (never log secrets; do not log full 1505-line content in production)
- Per-line result: transactionid or error
- Email notifications sent

Add correlation id per batch to link logs.

---

## 15. Test Plan

### Unit tests
- RecordBuilder:
  - Output length = 1505
  - Field placement correctness for sample specs
- CryptoService:
  - Deterministic encryption test vectors (if provided)
- Validation:
  - Missing mandatory fields detection
  - Conditional rule checks
- Response parsing:
  - Map response item -> correct contract/outbox record

### Integration tests (optional)
- Mock CaRGOS endpoints via local test server
- Verify token + encryption + Send format

---

## 16. Implementation Tasks (Codex TODO)

### Phase 1 → Skeleton
- [ ] Create solution + projects listed in Section 6
- [ ] Add dependency injection + logging in Worker project
- [ ] Define DTOs, `Cargos_Contratti` snapshot entity, and `Cargos_Contratti_Frontiera` outbox entity

### Phase 2 → Core integration
- [ ] Implement CryptoService (AES CBC PKCS7, Base64 output)
- [ ] Implement TokenProvider (Basic Auth GET /api/Token, caching)
- [ ] Implement CargosClient with Check/Send methods and required headers
- [ ] Implement RecordBuilder (fixed-width 1505)

### Phase 3 → Validation + Notifications
- [ ] Implement mandatory field list from CaRGOS docs (manual config or structured mapping file)
- [ ] Implement ValidationService producing MissingFields list
- [ ] Implement EmailService + templates
- [ ] Implement anti-spam logic (1 email per 24h per contract unless changes)

### Phase 4 → Orchestration
- [ ] Implement Worker loop:
  - execute `Cargos_Sync_Contratti_Frontiera`
  - fetch eligible outbox records
  - validate
  - build lines
  - batch to 100
  - optional Check then Send
  - update statuses
  - send emails on MISSING_DATA or SENT_KO_DATA

### Phase 5 → Hardening
- [ ] Retry policy for technical errors
- [ ] Robust error classification
- [ ] Add configuration validation on startup
- [ ] Add tests

### Tracked implemented updates (as of 2026-03-06)
- [x] Added SQL setup file `sql/Cargos_Setup.sql` as single executable DB script.
- [x] Renamed queue table from legacy `CargosOutbox` naming to `Cargos_Contratti_Frontiera`.
- [x] Renamed key field from `ContractId` to `ContractNo`.
- [x] Added `ContractLineNo` and changed uniqueness/idempotency to contract-line granularity.
- [x] Added sync procedure `dbo.Cargos_Sync_Contratti_Frontiera` (view extraction + snapshot upsert + outbox enqueue).
- [x] Added mandatory CaRGOS payload columns in both `Cargos_Contratti` and `Cargos_Contratti_Frontiera`.
- [x] Added real CaRGOS token flow and Send call pipeline with status transitions (`SENT_OK`, `SENT_KO_DATA`, `SENT_KO_RETRY`).
- [x] Added unique idempotency index `(ContractNo, ContractLineNo, SnapshotHash)` in `Cargos_Contratti_Frontiera`.
- [x] Added payload-fix reprocessing model (`DATA_FIX`) in analysis and implementation target.
- [x] Added long-running single-instance host loop model with sleep and cutoff hour.
- [x] Added app-side validation, record building, notification, and anti-spam implementation.
- [x] Added queue claim/reservation model to avoid overlapping processing of the same outbox row.
- [x] Added self-test execution mode for validation, record-builder, and crypto smoke checks.
- [x] Added startup sync service and SQL cache tables for `api/Tabella` reference data.
- [x] Corrected `api/Tabella` runtime handling to parse JSON envelope + Base64 `file` payload before reading `#`-separated rows.
- [x] Added lookup service on top of `Cargos_Tabella_Righe` to resolve business values to CaRGOS codes.
- [x] Added startup agency bootstrap pipeline for `CARGOS_WEB/Agenzia/Create`.
- [x] Updated agency bootstrap auth flow to handle `Login/Default` + interactive OTP on `Login/LoginAuth`.
- [x] Corrected agency web auth to separate login page GET (`Login/Login`) from credentials POST (`Login/Default`).
- [x] Added automatic one-time re-authentication and client recreation when `CARGOS_WEB` session expires mid-run.
- [x] Added SQL tracking tables `Cargos_Agenzie` and `Cargos_Agenzie_Frontiera`.
- [x] Added `CargosWeb.*` startup/auth settings for agency bootstrap.
- [x] Added structured agency luogo handling (`AgenziaCity`, `AgenziaCounty`, `AgenziaPostCode`) for `AGENZIA_LUOGO_COD` resolution.
- [x] Realigned `RecordBuilder` to the current official CaRGOS field dimensions (1505 total, current `TRACCIATO RECORD` layout).
- [x] Enlarged undersized SQL/app agency fields to avoid truncation before web/API submission (`AgenziaId` internal 50; API validation still 30; name/address/tel aligned to official sizes).
- [x] Corrected SQL `COL_LENGTH` migration checks for `NVARCHAR` columns so rerunning `Cargos_Setup.sql` applies length upgrades correctly.
- [x] Added daily overdue open-rental normalization: open extracted contracts with past `CONTRATTO_CHECKIN_DATA` are resent with today's effective check-in date once per day.

---

## 17. Acceptance Criteria

1) Given a contract missing mandatory fields:
   - No CaRGOS API call is performed
   - Outbox status becomes `MISSING_DATA`
   - Branch email is sent with the missing fields list

2) Given a valid contract:
   - A 1505-char record line is built
   - Contract is sent to CaRGOS (within a batch of max 100)
   - Success stores transactionid and marks `SENT_OK`

3) Given CaRGOS rejects due to data:
   - Outbox status becomes `SENT_KO_DATA`
   - Branch email is sent with CaRGOS error details

4) Given a transient technical error:
   - Outbox status becomes `SENT_KO_RETRY`
   - NextRetryAt is scheduled according to retry policy

5) No duplicate sending:
   - The same contract snapshot is never enqueued twice.

6) Given a contract with changed `CONTRATTO_CHECKIN_DATA` or `CONTRATTO_CHECKOUT_DATA`:
   - A new queue item is inserted in `Cargos_Contratti_Frontiera`
   - A new CaRGOS call is executed with updated data.

7) Given contracts not in signed status or without delivered line:
   - They are not extracted by `Cargos_Vista_Contratti`
   - No queue item is created.

---

## 18. Future Enhancements (Not required now)
- Support optional fields incrementally (step-by-step)
- Implement `/api/Tabella` downloads and local caching for code tables
- Add dashboard/report for failures and pending items
- Move from polling to event-driven trigger (if infrastructure allows)

---
END OF SPEC

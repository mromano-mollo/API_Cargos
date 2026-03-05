# CARGOS Integration Service (VB.NET) — Functional + Technical Specification (Codex Friendly)

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
- **Branch**: The contract’s branch (filiale) determined by location/ubication; used for notifications.
- **CaRGOS**: Police service receiving rental driver registrations.
- **Record line**: A single fixed-width string representing one contract, length **1505** chars.
- **Batch**: A single API call containing up to **100** record lines.

---

## 3. High-level Functional Requirements

### FR-01 — Process contracts
- Input source is DB view `Cargos_Vista_Contratti`.
- The view must contain only contracts in signed status and delivered line.
- The service receives a list of "contracts-to-send" with all necessary domain fields and the branch identifier.

### FR-02 — Validate mandatory fields (before calling CaRGOS)
- The service must validate that all CaRGOS fields marked **mandatory** are present.
- Conditional mandatory rules must be applied (examples from CaRGOS docs):
  - If "residence place" is provided, then "residence address" becomes required.
  - Second driver: either provide all required second-driver fields OR do not provide the second driver at all.

### FR-03 — Missing mandatory data → notify branch
- If validation fails:
  - Do not call CaRGOS.
  - Mark contract status as `MISSING_DATA`.
  - Send an email to the contract’s branch with:
    - Contract identifier(s)
    - List of missing fields (using CaRGOS field names)
    - Optional: suggested correction steps

### FR-04 — Build CaRGOS record lines
- For validated contracts, build a fixed-width record line of length **1505** using the CaRGOS field positions (DAL/AL).
- Initially send **mandatory fields only** (fields marked optional should be blank unless required by conditional rules).

### FR-05 — Send to CaRGOS
- Send record lines to CaRGOS in batches of max **100** lines per call.
- Parse response per record line and store result:
  - If success: store `transactionid` and mark `SENT_OK`.
  - If data error: mark `SENT_KO_DATA` and notify branch with CaRGOS error details.
  - If technical error: mark `SENT_KO_RETRY` (retry later).

### FR-06 — Idempotency / No duplicate sending
- The service must not send the same contract snapshot twice.
- Implement internal tracking tables where queue idempotency is keyed by `ContractNo + LineNo + SnapshotHash`.

### FR-07 - Re-send when contract date fields change
- If `CONTRATTO_CHECKIN_DATA` or `CONTRATTO_CHECKOUT_DATA` changes for an already processed contract, the service must send a new call to CaRGOS with updated data.
- The new call must be tracked as a new queue item, while preserving history of previous attempts/results.

### FR-08 - Extraction + snapshot + enqueue flow
- At each run:
  1) execute sync procedure `Cargos_Sync_Contratti_Frontiera`;
  2) procedure extracts current eligible contracts from `Cargos_Vista_Contratti`;
  3) procedure upserts contract snapshot state in internal table `Cargos_Contratti`;
  4) if a new contract is found or checkin/checkout values changed, procedure inserts a new pending item into `Cargos_Frontiera`.

---

## 4. Non-goals (initial phase)
- Full support of optional CaRGOS fields (only mandatory fields first).
- UI changes / user interface.
- Real-time sending from frontend; this is a backend worker.

---

## 5. Technical Requirements

### TR-01 — Platform & project type
- VB.NET solution
- Recommended: **.NET Worker Service** (Generic Host) deployable as Windows Service or scheduled console.

### TR-02 — HTTP client usage
- Use HttpClientFactory (or a single shared HttpClient instance) to avoid socket exhaustion.
- Apply sensible timeouts (e.g., 30–60s) and classify timeout as technical error.

### TR-03 — Authentication flow (CaRGOS)
The service must implement the official CaRGOS token flow:

1) **GET** `/api/Token` with **Basic Auth** using CaRGOS Username/Password  
2) Receive `access_token` + expiration  
3) Encrypt `access_token` using **AES CBC + PKCS7 padding** with key/iv derived from APIKEY:
   - key = first 32 characters of APIKEY
   - iv  = next 16 characters of APIKEY
   - output must be Base64
4) Use encrypted token as `Authorization: Bearer <encrypted_token>`
5) Add header: `Organization: <USERNAME>`

### TR-04 — Request payload format
For `Check` and `Send`, the body is:
- JSON array of strings: `["<recordLine1>", "<recordLine2>", ...]`

### TR-05 — Batch size limit
- Maximum 100 record lines per request.

### TR-06 — Optional pre-validation via CaRGOS Check endpoint
- Support calling `/api/Check` before `/api/Send`.
- Default behavior can be:
  - DEV/TEST: `Check` then `Send`
  - PROD: optional toggle, but recommended for troubleshooting

### TR-07 - Reuse `CommonLibrary` for shared concerns
- `CommonLibrary` can be used for common capabilities already available in solution (for example logging, email sending, DB access).
- Prefer using existing `CommonLibrary` modules instead of re-implementing shared behavior in CARGOS projects.
- Avoid modifying `CommonLibrary` unless strictly needed and only for features that are truly generic and reusable outside CARGOS.

### TR-08 - Change detection strategy for checkin/checkout
- Implement an internal snapshot table `Cargos_Contratti` to store the last known values of `CONTRATTO_CHECKIN_DATA` and `CONTRATTO_CHECKOUT_DATA` per contract.
- Compare current extracted values with stored snapshot using normalized datetime values (consistent timezone and precision).
- On change detection, enqueue a new item in `Cargos_Frontiera` with reason `DATE_CHANGE`.
- Preferred model: implement sync logic in SQL procedure `Cargos_Sync_Contratti_Frontiera` and execute it at the beginning of each app cycle.
- SQL Agent scheduling of the same procedure is optional fallback, not the primary orchestration model.

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

### Snapshot table: `Cargos_Contratti`
Purpose: keep one current state row per contract-line for change detection.
Fields:
- `Id` (PK)
- `ContractNo` (our contract number, e.g. `CTR26-xxxxxx`)
- `LineNo` (contract line number)
- `CargosContractId`
- `BranchId`
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
- `DataFingerprint` (hash/string built from normalized checkin/checkout)
- `LastQueuedFingerprint` (hash/string of last enqueued snapshot)
- `LastQueuedAt` (datetime)
- `LastSeenAt` (datetime)
- `CreatedAt`, `UpdatedAt`

### Suggested table: `Cargos_Frontiera`
Fields:
- `Id` (PK)
- `ContractNo`
- `LineNo`
- `CargosContractId` (value used in CaRGOS record, if different)
- `BranchId`
- Same mandatory CaRGOS payload columns listed for `Cargos_Contratti` (snapshot at queue creation time)
- `Reason` (`INITIAL_SEND` | `DATE_CHANGE`)
- `SnapshotHash` (hash/string of snapshot used for this send item)
- `Status` (string/enum):
  - `PENDING`
  - `MISSING_DATA`
  - `READY_TO_SEND`
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
- A queue item is eligible for sending only if status in `{PENDING, READY_TO_SEND, SENT_KO_RETRY}` AND that same queue item has not been `SENT_OK`.
- Prevent duplicate queue creation for same contract snapshot using unique key `(ContractNo, LineNo, SnapshotHash)`.

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
- Subject: `CARGOS - Missing mandatory data for contract <ContractNo>/<LineNo>`
- Body includes:
  - Contract reference
  - Branch reference
  - Missing CaRGOS fields list
  - Next steps: complete missing fields on contract

### Trigger 2: CaRGOS data rejection
- Subject: `CARGOS - Rejected contract <ContractNo>/<LineNo>`
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

Store in `appsettings.json` + environment overrides:
- `Cargos:BaseUrl`
- `Cargos:Username`
- `Cargos:Password`
- `Cargos:ApiKey`
- `Cargos:UseCheckEndpoint` (bool)
- `Worker:PollingIntervalSeconds`
- `Db:ContractsViewName` (default `Cargos_Vista_Contratti`)
- `Db:ContractsSyncProcedure` (default `Cargos_Sync_Contratti_Frontiera`)
- `Email:SmtpHost`, `Email:SmtpPort`, `Email:User`, `Email:Password`, `Email:From`
- Branch email lookup settings (mapping strategy)

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

### Phase 1 — Skeleton
- [ ] Create solution + projects listed in Section 6
- [ ] Add dependency injection + logging in Worker project
- [ ] Define DTOs, `Cargos_Contratti` snapshot entity, and `Cargos_Frontiera` outbox entity

### Phase 2 — Core integration
- [ ] Implement CryptoService (AES CBC PKCS7, Base64 output)
- [ ] Implement TokenProvider (Basic Auth GET /api/Token, caching)
- [ ] Implement CargosClient with Check/Send methods and required headers
- [ ] Implement RecordBuilder (fixed-width 1505)

### Phase 3 — Validation + Notifications
- [ ] Implement mandatory field list from CaRGOS docs (manual config or structured mapping file)
- [ ] Implement ValidationService producing MissingFields list
- [ ] Implement EmailService + templates
- [ ] Implement anti-spam logic (1 email per 24h per contract unless changes)

### Phase 4 — Orchestration
- [ ] Implement Worker loop:
  - execute `Cargos_Sync_Contratti_Frontiera`
  - fetch eligible outbox records
  - validate
  - build lines
  - batch to 100
  - optional Check then Send
  - update statuses
  - send emails on MISSING_DATA or SENT_KO_DATA

### Phase 5 — Hardening
- [ ] Retry policy for technical errors
- [ ] Robust error classification
- [ ] Add configuration validation on startup
- [ ] Add tests

### Tracked implemented updates (as of 2026-03-05)
- [x] Added SQL setup file `sql/Cargos_Setup.sql` as single executable DB script.
- [x] Renamed queue table from legacy `CargosOutbox` naming to `Cargos_Frontiera`.
- [x] Renamed key field from `ContractId` to `ContractNo`.
- [x] Added `LineNo` and changed uniqueness/idempotency to contract-line granularity.
- [x] Added sync procedure `dbo.Cargos_Sync_Contratti_Frontiera` (view extraction + snapshot upsert + outbox enqueue).
- [x] Added mandatory CaRGOS payload columns in both `Cargos_Contratti` and `Cargos_Frontiera`.
- [x] Added real CaRGOS token flow and Send call pipeline with status transitions (`SENT_OK`, `SENT_KO_DATA`, `SENT_KO_RETRY`).
- [x] Added unique idempotency index `(ContractNo, LineNo, SnapshotHash)` in `Cargos_Frontiera`.

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
   - A new queue item is inserted in `Cargos_Frontiera`
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

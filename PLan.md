# Rafiq AI — MVP Implementation Phases (spec-kit / SDD)

## How to Use This Document with spec-kit

Each phase below maps to one spec-kit cycle:
1. Run `/speckit.spec` → generate the spec for that phase
2. Run `/speckit.plan` → generate the technical plan
3. Run `/speckit.tasks` → break into small, testable tasks
4. Run `/speckit.implement` → agent implements task by task

Complete one phase fully before starting the next. The output of each phase (working, tested code) becomes the foundation the next phase builds on.

---

## Stack (from project docs — bake into constitution.md)

| Layer | Technology |
|---|---|
| Frontend Web | Angular |
| Mobile | Flutter |
| Backend | ASP.NET Core REST APIs |
| Database | SQL Server |
| File Storage | Azure Blob Storage / AWS S3 |
| AI | OCR + LLM + RAG |
| Maps | Google Maps API |
| Notifications | Firebase Cloud Messaging (FCM) + SMS/Email |

---

## constitution.md Principles (set before Phase 1)

Before running any spec, initialize your constitution with these non-negotiables:

- **API-first**: every feature is exposed as a REST endpoint; frontend/mobile are pure consumers.
- **Security baseline**: JWT authentication on every protected endpoint; no patient data endpoint is ever unauthenticated.
- **Soft delete only**: never hard-delete medical records; always set `IsDeleted = true` + `DeletedAt`.
- **UTC timestamps**: all `CreatedAt`, `UpdatedAt`, `DeletedAt` stored in UTC.
- **Nullable by design**: `PatientProfile.UserId` is nullable (dependents); `LabResult.DocumentId` is nullable (manual entry).
- **Audit every write**: every POST/PUT/PATCH/DELETE on patient data must write an `AuditLog` row.
- **Error contracts**: all API errors return a standard `{ statusCode, message, errors[] }` shape.
- **Test coverage gate**: no phase is done until unit tests pass for all service-layer logic.

---

## Phase Overview

| Phase | Name | What it Delivers | Depends On |
|---|---|---|---|
| 1 | Foundation & Auth | Database schema, User auth, PatientProfile CRUD | — |
| 2 | Medical Documents & OCR | Document upload, blob storage, OCR extraction pipeline | Phase 1 |
| 3 | Medications & Reminders | Medication list, schedules, adherence tracking, notifications | Phase 2 |
| 4 | Lab Results & Appointments | Lab result entry (manual + document), appointments, reminders | Phase 2 |
| 5 | Family & Caregiver Access | CaregiverLink with approval workflow, cross-patient views | Phase 1 |
| 6 | AI Medical Assistant (RAG) | Chat interface, LLM integration, per-claim RAG citations | Phase 2 |
| 7 | Healthcare Provider Discovery | Provider database, map search, link to documents/appointments | Phase 1 |
| 8 | Consent, Audit & Compliance | Consent management, AuditLog, soft delete, notification log | All prior phases |

---

## Phase 1 — Foundation & Auth

**Goal:** A running, deployable backend with the full database schema, user registration/login, and patient profile management. Everything else builds on top of this.

### Spec Inputs (paste into `/speckit.spec`):

> Build the foundation layer for Rafiq AI, a personal healthcare companion platform.
> This phase covers:
> 1. SQL Server database schema for all MVP entities: User, PatientProfile, CaregiverLink, Document, ExtractedEntity, Medication, MedicationSchedule, Appointment, LabResult, HealthcareProvider, ChatSession, ChatMessage, KnowledgeSource, ChatMessageCitation, Notification, Consent, AuditLog. All entities holding personal/medical data must include CreatedAt, UpdatedAt, IsDeleted, DeletedAt fields.
> 2. User registration (email + phone, hashed password, role: Patient/Caregiver/Admin).
> 3. User login returning a JWT access token and refresh token.
> 4. PatientProfile CRUD (create, get, update, soft delete) — UserId is nullable to support dependents with no login.
> 5. Middleware: JWT validation on protected routes, global error handler returning standard error shape.
>
> Stack: ASP.NET Core Web API, SQL Server (Entity Framework Core), JWT.
> Non-goals for this phase: file upload, OCR, notifications, AI, maps.

### Key Entities Introduced:
- `User`, `PatientProfile`

### Acceptance Criteria:
- [ ] All 17 tables created with correct FK relationships and audit fields
- [ ] `POST /auth/register` and `POST /auth/login` return valid JWTs
- [ ] `GET/POST/PUT/DELETE /patients/{id}` endpoints work, with soft delete
- [ ] Unauthorized requests to protected routes return 401
- [ ] Unit tests cover AuthService, PatientService, token generation/validation

---

## Phase 2 — Medical Documents & OCR

**Goal:** Users can upload medical documents; the system stores metadata in SQL and the file in blob storage, then runs OCR/AI extraction to detect medications, diagnoses, and lab values from the document.

### Spec Inputs (paste into `/speckit.spec`):

> Build the document management and OCR extraction pipeline for Rafiq AI.
> This phase covers:
> 1. Document upload endpoint: accepts PDF/JPG/PNG, stores file in Azure Blob Storage / S3, persists Document metadata in SQL (DocumentType, FileUrl, OcrStatus, PatientId, ProviderId nullable, audit fields).
> 2. OCR/AI extraction pipeline: triggered after upload, extracts structured entities (Medication, Diagnosis, LabValue) from the document and stores them in ExtractedEntity (RawValue, ConfirmedValue, ConfidenceScore, IsUserConfirmed).
> 3. User confirmation endpoint: allows the patient/caregiver to review and confirm/correct AI-extracted data (sets IsUserConfirmed = true, updates ConfirmedValue).
> 4. Document list and detail endpoints: list all documents for a patient, get document with its extracted entities.
> 5. OcrStatus lifecycle: Pending → Processing → Completed / Failed.
>
> Stack: ASP.NET Core, Azure Blob Storage SDK (or AWS S3), OCR service integration (Azure AI Document Intelligence or equivalent).
> Non-goals: medication scheduling, lab trend analysis, chat assistant.

### Key Entities Introduced:
- `Document`, `ExtractedEntity`

### Acceptance Criteria:
- [ ] `POST /documents/upload` stores file in blob and inserts Document row with `OcrStatus = Pending`
- [ ] OCR pipeline runs asynchronously, populates ExtractedEntity rows, sets `OcrStatus = Completed`
- [ ] `PUT /documents/{id}/entities/{entityId}/confirm` updates ConfirmedValue and IsUserConfirmed
- [ ] `GET /patients/{id}/documents` returns documents with their extraction status
- [ ] Unit tests cover upload service, extraction parser, confirmation logic
- [ ] OCR failures set `OcrStatus = Failed` without crashing the pipeline

---

## Phase 3 — Medications & Reminders

**Goal:** Patients have a medication list derived from confirmed extractions or manual entry, with auto-generated schedules and notifications for adherence tracking.

### Spec Inputs (paste into `/speckit.spec`):

> Build the medication management and reminder system for Rafiq AI.
> This phase covers:
> 1. Medication CRUD: create a medication record for a patient (Name, Dosage, Frequency, Route, StartDate, EndDate, SourceDocumentId nullable). Support both manual creation and auto-creation from a confirmed ExtractedEntity.
> 2. MedicationSchedule generation: when a Medication is created, auto-generate MedicationSchedule rows based on Frequency (e.g., "twice daily" → two schedule entries per day) with Status = Pending.
> 3. Adherence tracking: endpoint to mark a scheduled dose as Taken, Missed, or Skipped.
> 4. Notification dispatch: when a MedicationSchedule entry is upcoming (within reminder window), send a push notification via Firebase Cloud Messaging (FCM) to the patient and/or linked caregivers. Log the notification in the Notification table.
> 5. Medication list endpoint: list active medications for a patient with today's schedule summary.
>
> Stack: ASP.NET Core, SQL Server, FCM (Firebase Admin SDK), background scheduler (e.g., Hangfire or hosted service).
> Non-goals: lab results, appointments, AI chat.

### Key Entities Introduced:
- `Medication`, `MedicationSchedule`, `Notification`

### Acceptance Criteria:
- [ ] `POST /patients/{id}/medications` creates medication + auto-generates schedule rows
- [ ] `PUT /schedules/{id}/status` updates dose status (Taken/Missed/Skipped)
- [ ] FCM push fires before each scheduled dose; Notification row logged with SentAt + Status
- [ ] `GET /patients/{id}/medications` returns active medications with schedule summary
- [ ] Unit tests cover schedule generation logic, adherence update, notification trigger

---

## Phase 4 — Lab Results & Appointments

**Goal:** Patients can record lab results (from documents or manually) and track appointments with reminders.

### Spec Inputs (paste into `/speckit.spec`):

> Build lab result tracking and appointment management for Rafiq AI.
> This phase covers:
> 1. Lab Result entry: create a LabResult record (TestName, Value, Unit, ReferenceRange, TestDate). Support two entry paths: (a) linked to an uploaded Document (DocumentId FK set), (b) manual entry by patient/caregiver (DocumentId = null, EnteredBy = UserId). EntrySource field distinguishes the two.
> 2. Lab Result trend: endpoint returning all values for a given TestName over time for a patient, sorted by TestDate — suitable for charting a trend line.
> 3. Appointment CRUD: create/edit/cancel appointments (Type: Visit/Test/Scan/FollowUp, DateTime, ProviderId nullable, Location, ReminderLeadTime, Status: Scheduled/Completed/Cancelled).
> 4. Appointment reminder: dispatch FCM/email reminder at (DateTime − ReminderLeadTime); log to Notification table.
> 5. List endpoints: upcoming appointments and recent lab results per patient.
>
> Stack: ASP.NET Core, SQL Server, FCM, background scheduler.
> Non-goals: AI chat, provider map search, caregiver access (handled in Phase 5).

### Key Entities Introduced:
- `LabResult`, `Appointment`

### Acceptance Criteria:
- [ ] `POST /patients/{id}/lab-results` supports both document-linked and manual entry
- [ ] `GET /patients/{id}/lab-results/trend?test=HbA1c` returns time-ordered values
- [ ] `POST /patients/{id}/appointments` creates appointment and schedules reminder job
- [ ] Reminder notification fires at correct lead time; Notification row logged
- [ ] Unit tests cover dual-entry lab logic, trend query, reminder scheduling

---

## Phase 5 — Family & Caregiver Access

**Goal:** A user can invite another user as a caregiver for a patient, with an approval workflow controlling access. Caregivers see patient data within their granted permission level.

### Spec Inputs (paste into `/speckit.spec`):

> Build the family and caregiver access system for Rafiq AI.
> This phase covers:
> 1. CaregiverLink invitation: a user sends an invite to another user (by email/phone) to become a caregiver for a PatientProfile. CaregiverLink is created with Status = Pending, RequestedAt = now.
> 2. Approval workflow: the invitee can Accept or Reject. On Accept: Status = Accepted, AcceptedAt = now, AcceptedBy = invitee UserId. On Reject: Status = Rejected. Either party can later Revoke (Status = Revoked).
> 3. Access enforcement: all patient data endpoints (documents, medications, lab results, appointments) must check that the requesting user is either (a) the patient themselves, or (b) a caregiver with Status = Accepted and PermissionLevel covering the requested action (view/edit/reminder-only).
> 4. Caregiver dashboard: endpoint returning a list of all PatientProfiles a caregiver has accepted access to, with a summary (name, next appointment, pending medication doses).
> 5. Dependent patient creation: allow a caregiver to create a PatientProfile with UserId = null (a dependent with no login), automatically creating an Accepted CaregiverLink for themselves.
> 6. Notifications: notify caregiver when their linked patient misses a medication or appointment.
>
> Stack: ASP.NET Core, SQL Server, FCM/email.
> Non-goals: AI chat, provider discovery.

### Key Entities Introduced:
- `CaregiverLink` (approval workflow in full)

### Acceptance Criteria:
- [ ] `POST /caregiver-links/invite` creates Pending link and notifies invitee
- [ ] `PUT /caregiver-links/{id}/accept` and `/reject` transitions work correctly
- [ ] Patient endpoints return 403 for callers without an Accepted CaregiverLink
- [ ] `GET /caregiver/dashboard` returns all linked patients with summary data
- [ ] Dependent patient creation works with `UserId = null`
- [ ] Unit tests cover approval state machine, access-enforcement middleware

---

## Phase 6 — AI Medical Assistant (RAG)

**Goal:** Patients can chat with an AI assistant that explains their reports, answers healthcare questions, and cites per-claim the specific medical source (WHO guideline, drug database, literature) it used.

### Spec Inputs (paste into `/speckit.spec`):

> Build the AI medical assistant with RAG for Rafiq AI.
> This phase covers:
> 1. Chat session management: create a ChatSession for a patient; persist ChatMessages (Sender: User/AI, Content, Timestamp).
> 2. LLM integration: on user message, call the LLM (GPT-4 / Claude / equivalent) with the patient's message plus relevant context retrieved from the medical knowledge base.
> 3. RAG retrieval: query the KnowledgeSource knowledge base (WHO Guidelines, Drug Databases, Clinical Literature, National Health Recommendations) using vector similarity to retrieve the most relevant chunks for the patient's question.
> 4. Per-claim citation: the LLM response is structured so each individual claim references its specific KnowledgeSource (SourceId) and a Locator (page/paragraph/chunk). Store these as ChatMessageCitation rows (ClaimText, SourceId, Locator, ConfidenceScore) linked to the ChatMessage.
> 5. Patient context injection: before calling the LLM, inject a summary of the patient's active medications and recent diagnoses from their profile to personalize the response.
> 6. Medical specialty suggestion: if the patient describes symptoms, the assistant should suggest an appropriate medical specialty and the confidence score should be stored in the relevant ChatMessageCitation.
> 7. Safety guardrail: the assistant must include a disclaimer on every response that it is not a substitute for professional medical advice.
>
> Stack: ASP.NET Core, LLM API (OpenAI / Anthropic), vector search (Azure AI Search / pgvector / Qdrant), KnowledgeSource seed data.
> Non-goals: voice, real-time streaming (can be added post-MVP), provider map.

### Key Entities Introduced:
- `ChatSession`, `ChatMessage`, `KnowledgeSource`, `ChatMessageCitation`

### Acceptance Criteria:
- [ ] `POST /patients/{id}/chat/sessions` creates a ChatSession
- [ ] `POST /chat/sessions/{id}/messages` stores user message, retrieves RAG context, calls LLM, stores AI response
- [ ] AI response is split into claims; each claim has a ChatMessageCitation row with SourceId + Locator
- [ ] `GET /chat/sessions/{id}/messages` returns full conversation with citations
- [ ] Patient's medications and diagnoses are injected into every LLM call
- [ ] Disclaimer text is present in every AI message
- [ ] Unit tests cover RAG retrieval, citation parsing, patient context injection

---

## Phase 7 — Healthcare Provider Discovery

**Goal:** Patients can search for nearby hospitals, clinics, labs, and pharmacies, filter by type/specialty, and link providers to their documents and appointments.

### Spec Inputs (paste into `/speckit.spec`):

> Build healthcare provider discovery for Rafiq AI.
> This phase covers:
> 1. HealthcareProvider data model: seed a provider database with Name, Type (Hospital/Clinic/Lab/Pharmacy), Address, Latitude, Longitude, Phone, Services (comma-separated or JSON array).
> 2. Proximity search: endpoint accepting (latitude, longitude, radius, type filter) returning providers sorted by distance. Use Google Maps API or Haversine formula for distance calculation.
> 3. Provider detail: endpoint returning full provider details including address, phone, services offered.
> 4. Link provider to document: allow updating Document.ProviderId to associate a document with the provider it came from.
> 5. Link provider to appointment: allow setting Appointment.ProviderId when creating/editing an appointment; provider detail should show automatically on the appointment.
> 6. Provider search from appointment/document creation: when a patient creates an appointment or uploads a document, surface a provider search inline so they can tag the source provider.
>
> Stack: ASP.NET Core, SQL Server, Google Maps API (or Haversine for distance), provider seed data.
> Non-goals: provider ratings/reviews, real-time availability, booking integration.

### Key Entities Introduced:
- `HealthcareProvider`

### Acceptance Criteria:
- [ ] `GET /providers/search?lat=&lng=&radius=&type=` returns sorted, filtered provider list
- [ ] `GET /providers/{id}` returns full provider detail
- [ ] `PUT /documents/{id}/provider` links a provider to a document
- [ ] `POST/PUT /appointments` accepts ProviderId and returns provider name in appointment detail
- [ ] Distance calculation is accurate within acceptable margin
- [ ] Unit tests cover proximity sort, type filter, provider link

---

## Phase 8 — Consent, Audit & Compliance

**Goal:** Formal consent tracking for what patients have authorized, a complete audit log of all data access and modification, and a notification log audit view. This phase hardens the platform for production readiness.

### Spec Inputs (paste into `/speckit.spec`):

> Build the consent, audit, and compliance layer for Rafiq AI.
> This phase covers:
> 1. Consent management: create Consent records per patient for each ConsentType (DataSharing, AIProcessing, CaregiverAccess, Marketing). Endpoints to grant and revoke consent. If a patient has not granted AIProcessing consent, the AI chat endpoint must return 403 with a prompt to provide consent.
> 2. Consent enforcement middleware: check Consent.Status = Granted before executing any feature that requires it (e.g., AI chat requires AIProcessing consent; caregiver invite requires CaregiverAccess consent).
> 3. AuditLog completeness: verify and close any gaps — every POST/PUT/PATCH/DELETE on patient data entities (Document, Medication, LabResult, Appointment, CaregiverLink, Consent) must write an AuditLog row (ActorUserId, PatientId, Action, EntityType, EntityId, Timestamp, IpAddress).
> 4. Read access audit: log GET requests to patient records in AuditLog when the actor is a caregiver (not the patient themselves), for access tracking.
> 5. Notification log: expose a `GET /patients/{id}/notifications` endpoint returning the full Notification history (type, channel, status, timestamp) for patient and caregiver visibility.
> 6. Soft delete verification: run a check across all entities to confirm no hard-delete endpoints exist; all deletes must set IsDeleted = true, DeletedAt = now, and write an AuditLog row.
> 7. Data export: endpoint allowing a patient to export their full medical profile (documents metadata, medications, lab results, appointments, consents) as a structured JSON payload — supports right-to-portability.
>
> Stack: ASP.NET Core, SQL Server, middleware pipeline.
> Non-goals: formal HIPAA/GDPR certification (baseline compliance only at MVP level).

### Key Entities Introduced / Hardened:
- `Consent`, `AuditLog`, `Notification` (log view)

### Acceptance Criteria:
- [ ] `POST /patients/{id}/consents` and `PUT /consents/{id}/revoke` work correctly
- [ ] AI chat returns 403 if AIProcessing consent is not granted
- [ ] Every write operation on a patient entity creates an AuditLog row
- [ ] Caregiver read access is logged in AuditLog
- [ ] `GET /patients/{id}/notifications` returns full notification history
- [ ] No endpoint in the codebase calls SQL DELETE on a patient data table (verified by test)
- [ ] `GET /patients/{id}/export` returns complete profile JSON

---

## Summary: Phase-to-Entity Mapping

| Entity | Introduced in Phase |
|---|---|
| User | 1 |
| PatientProfile | 1 |
| CaregiverLink (full approval) | 5 |
| Document | 2 |
| ExtractedEntity | 2 |
| Medication | 3 |
| MedicationSchedule | 3 |
| Notification | 3 (extended in 4, 8) |
| LabResult | 4 |
| Appointment | 4 |
| ChatSession | 6 |
| ChatMessage | 6 |
| KnowledgeSource | 6 |
| ChatMessageCitation | 6 |
| HealthcareProvider | 7 |
| Consent | 8 |
| AuditLog | 1 (hardened in 8) |

---

## Post-MVP Backlog (out of scope for now)

- Real-time chat streaming (WebSockets / SignalR)
- Voice input for AI assistant
- DICOM radiology file support
- Provider ratings and booking integration
- Multi-language support
- Formal HIPAA / local regulatory compliance audit
- Mobile (Flutter) frontend — all above phases are backend-first; mobile consumes the APIs
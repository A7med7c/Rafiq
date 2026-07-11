Repository Map

Root: C:\Users\Ahmed Ragab\source\repos\Rafiq
Backend solution: src/src.sln:5
Backend projects:
src/Rafiq.API/Rafiq.API.csproj:1
src/Rafiq.Application/Rafiq.Application.csproj:1
src/Rafiq.Domain/Rafiq.Domain.csproj:1
src/Rafiq.Infrastructure/Rafiq.Infrastructure.csproj:1
Backend tests:
tests/Rafiq.API.Tests/Rafiq.API.Tests.csproj:1
tests/Rafiq.Application.Tests/Rafiq.Application.Tests.csproj:1
tests/Rafiq.Domain.Tests/Rafiq.Domain.Tests.csproj:1
Angular workspace: RafiqAngular/angular.json:8
Angular package manifest: RafiqAngular/package.json:1
Backend config:
src/Rafiq.API/appsettings.json:1
src/Rafiq.API/appsettings.Development.json:1
src/Rafiq.API/Properties/launchSettings.json
Angular config:
RafiqAngular/angular.json:1
RafiqAngular/tsconfig.json
RafiqAngular/tsconfig.app.json
RafiqAngular/tsconfig.spec.json
RafiqAngular/src/app/Environments/Environment.ts:1
Documentation:
RafiqAngular/README.md:1
PLan.md
Infrastructure/tooling:
.kilo/agent-manager.json
.kilo/package.json
RafiqAngular/.vscode/tasks.json
RafiqAngular/.vscode/launch.json
RafiqAngular/.vscode/extensions.json
RafiqAngular/.vscode/mcp.json
Architecture Overview

The backend follows a Clean Architecture-style split: API -> Application -> Domain, with Infrastructure implementing persistence/external services and referenced by API.
Rafiq.API is the HTTP host and composition root. It registers controllers, Swagger, Application, Infrastructure, JWT auth, authorization, health checks, CORS, static files, and exception middleware in src/Rafiq.API/Program.cs:18.
Rafiq.Application owns CQRS handlers, DTOs, validators, mappings, response wrappers, interfaces, and feature orchestration. MediatR, FluentValidation, and Mapster are registered in src/Rafiq.Application/DependencyInjection.cs:16.
Rafiq.Domain owns entities, enums, repository contracts, and domain exceptions. It has no package references in src/Rafiq.Domain/Rafiq.Domain.csproj:1.
Rafiq.Infrastructure owns EF Core, Identity, repositories, JWT/token services, Google validation, Twilio notification service, Bedrock AI integration, local file storage, and background jobs. Its registrations are in src/Rafiq.Infrastructure/DependencyInjection.cs:21.
The Angular app is a standalone Angular application, not NgModule-based. It bootstraps with bootstrapApplication(App, appConfig) and uses router, HTTP interceptors, functional guards, signals, services, and plain CSS.
Technology Stack

Backend:
.NET net10.0 across API/Application/Domain/Infrastructure at src/Rafiq.API/Rafiq.API.csproj:3
ASP.NET Core Web API
EF Core SQL Server 10._
ASP.NET Core Identity with GUID users/roles
JWT Bearer auth
MediatR 12._
FluentValidation 11._
Mapster 7._
Swagger/Swashbuckle 10.2.3
Serilog packages referenced, but no active UseSerilog bootstrap found
Twilio
Google.Apis.Auth
BCrypt.Net
Frontend:
Angular ^21.2.0 at RafiqAngular/package.json:14
Angular CLI/build ^21.2.1
TypeScript ~5.9.2
RxJS ~7.8.0
Vitest via Angular test builder
jspdf and jspdf-autotable for client PDF generation
Plain component CSS and global CSS, no Tailwind/Bootstrap package detected
Backend Projects

Rafiq.API: ASP.NET Core Web API host. Entry point is src/Rafiq.API/Program.cs:12. Controllers live under src/Rafiq.API/Controllers. Middleware lives under src/Rafiq.API/Middleware.
Rafiq.Application: application layer. Feature folders include Auth, PatientProfiles, LabReports, ImagingReports, Prescriptions, UserMedicines, MedicineReminders, Appointments, and GeneralDocuments.
Rafiq.Domain: domain model. Contains Common/BaseEntity, Entities, Enums, Exceptions, and Repositories.
Rafiq.Infrastructure: persistence and integrations. Contains Persistence/RafiqDbContext.cs, Persistence/Configurations, Persistence/Repositories, Persistence/Identity, Services/auth, Services/Notifications, Services/BackgroundJobs, migrations, Bedrock, and file storage.
Backend Request Pipeline

Controllers are registered with JSON enum string serialization in src/Rafiq.API/Program.cs:18.
Swagger is configured in src/Rafiq.API/Extensions/ServiceCollectionExtensions.cs:11.
CORS allows only http://localhost:4200 in src/Rafiq.API/Program.cs:34.
Global exception middleware runs early in src/Rafiq.API/Program.cs:47.
Dev-only Swagger UI is exposed at /swagger in src/Rafiq.API/Program.cs:49.
HTTPS redirection, CORS, authentication, authorization, static files, controllers, and health checks are mapped in src/Rafiq.API/Program.cs:59.
Health endpoint: /health in src/Rafiq.API/Program.cs:65.
Backend API Surface

Auth: api/auth in src/Rafiq.API/Controllers/AuthController.cs:20
Register, login, logout, Google login, me, update me, change password, forgot password, verify reset OTP, reset password, verify phone, resend phone code, refresh token, revoke token.
Patient profiles: api/patient-profiles in src/Rafiq.API/Controllers/PatientProfilesController.cs:15
Create, get me, get by id, update, delete.
Documents: api/documents in src/Rafiq.API/Controllers/DocumentsController.cs:24
Lab upload/save/list/get, imaging upload/save/list/get, general document upload.
Prescriptions: api/prescriptions in src/Rafiq.API/Controllers/PrescriptionsController.cs:21
Upload, save, list, get, update, delete.
User medicines: api/user-medicines in src/Rafiq.API/Controllers/UserMedicinesController.cs:16
Scan medicine box, add from prescription, add, list, get, update, delete.
Medicine reminders: mixed under api/user-medicines/{medicineId}/reminders and api/medicine-reminders in src/Rafiq.API/Controllers/MedicineRemindersController.cs:16.
Appointments: api/appointments in src/Rafiq.API/Controllers/AppointmentsController.cs:19
Create, list, upcoming, today, get, update, delete, complete, cancel.
Clean Architecture / CQRS

MediatR is registered for all Application handlers in src/Rafiq.Application/DependencyInjection.cs:16.
Validation is wired as a MediatR pipeline behavior in src/Rafiq.Application/DependencyInjection.cs:19.
Commands/queries return ApiResponse<T> wrappers, for example Application files matched throughout src/Rafiq.Application/Features.
Feature folders use command/query/DTO/validator structure.
Most controllers delegate to MediatR, but some API controllers directly construct domain entities and call repositories, especially DocumentsController and PrescriptionsController, which weakens the clean layering.
Authentication Flow

Backend uses IdentityCore with roles, SignInManager, EF stores, and default token providers in src/Rafiq.Infrastructure/DependencyInjection.cs:26.
JWT auth is configured in src/Rafiq.API/Extensions/ServiceCollectionExtensions.cs:40.
JWT secret is read from JWT_SECRET_KEY first, then Jwt:SecretKey in src/Rafiq.API/Extensions/ServiceCollectionExtensions.cs:42.
JWT validates issuer, audience, lifetime, signing key, and uses zero clock skew in src/Rafiq.API/Extensions/ServiceCollectionExtensions.cs:65.
Custom JSON 401/403 responses are returned from JWT events in src/Rafiq.API/Extensions/ServiceCollectionExtensions.cs:77.
Frontend login posts to ${environment.apiUrl}/auth/login in RafiqAngular/src/app/Services/auth-service.ts:57.
Successful login stores access/refresh tokens, then loads /auth/me in RafiqAngular/src/app/Services/auth-service.ts:149.
Tokens and current user are stored in localStorage under accessToken, refreshToken, and currentUser in RafiqAngular/src/app/Services/token-storage-service.ts:10.
Refresh token flow posts to /auth/refresh-token in RafiqAngular/src/app/Services/auth-service.ts:103.
The auth interceptor retries protected 401 responses after refresh in RafiqAngular/src/app/Interceptors/auth.interceptor.ts:54.
Google login uses frontend Google Identity Services and posts ID token to /auth/google in RafiqAngular/src/app/Services/auth-service.ts:94.
Authorization

Backend registers authorization in src/Rafiq.API/Program.cs:28.
Most resource controllers are protected with [Authorize], such as PatientProfilesController, DocumentsController, PrescriptionsController, UserMedicinesController, and AppointmentsController.
No role-based policies or custom authorization policies were found.
Auth endpoints are mostly public by absence of controller-level [Authorize]; selected endpoints use [Authorize] explicitly.
Frontend Summary

Angular app name: RafiqAngular in RafiqAngular/angular.json:9.
Browser entry: RafiqAngular/src/main.ts.
App providers are centralized in RafiqAngular/src/app/app.config.ts:13.
Root routes live in RafiqAngular/src/app/app.routes.ts:16.
Routes:
/ landing
/login, /register
/dashboard
/medical-records
/onboarding/welcome
/onboarding/step1 through /onboarding/step4
/onboarding/ai-upload
Guards:
authGuard protects app routes.
guestGuard prevents logged-in users from visiting login/register.
HTTP:
Base URL is hardcoded to https://localhost:7082/api in RafiqAngular/src/app/Environments/Environment.ts:3.
Auth interceptor attaches bearer tokens only to URLs starting with that API URL in RafiqAngular/src/app/Interceptors/auth.interceptor.ts:15.
State management:
No NgRx/store library.
Auth uses BehaviorSubject.
UI-heavy pages use Angular signals and computed state.
Styling:
Plain global CSS and component CSS.
Global stylesheet is RafiqAngular/src/styles.css.
Fonts and Font Awesome load from CDN in RafiqAngular/src/index.html.
Angular Features

Landing page with navbar, hero, features, about, how-it-works, stats, testimonials, contact, and footer.
Login/register pages with reusable auth hero and form components.
Dashboard aggregates recent labs/imaging and medicine reminder-like cards.
Medical records page handles labs, imaging, prescriptions, medicines, general documents, upload/review modals, AI review confirmation, medicine-box scanning, deletion, filtering, pagination, and PDF generation.
Onboarding wizard collects demographic/health data in sessionStorage and submits patient profile through HealthProfileService.
PDF generation is client-side via PdfService.
Frontend-Backend Communication

Angular project communicates with the local ASP.NET Core API at https://localhost:7082/api.
Auth endpoints:
POST /auth/login
POST /auth/register
POST /auth/google
GET /auth/me
POST /auth/refresh-token
POST /auth/logout
POST /auth/verify-phone
POST /auth/resend-phone-code
Patient profile endpoints:
POST /patient-profiles
GET /patient-profiles/me
Medical records endpoints:
GET /documents/labs
GET /documents/imaging
GET /prescriptions
GET /user-medicines
GET /documents/general
Upload endpoints:
POST /documents/upload/lab
POST /documents/upload/imaging
POST /prescriptions/upload
POST /documents/general/upload
POST /user-medicines/scan-box
The upload flow uses FormData with image, then opens client-side review/confirmation before saving normalized records.
No SignalR client or server registration was found.
No Hangfire usage was found.
Database Summary

Main DbContext: src/Rafiq.Infrastructure/Persistence/RafiqDbContext.cs:13.
It inherits IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid, ...> and implements IUnitOfWork.
DbSets include health profiles, allergies, chronic diseases, OTPs, phone verifications, prescriptions, prescription medicines, user medicines, medicine reminders, appointments, lab reports/results, imaging reports, general documents, and refresh tokens in src/Rafiq.Infrastructure/Persistence/RafiqDbContext.cs:31.
Configurations are scanned from the Infrastructure assembly in src/Rafiq.Infrastructure/Persistence/RafiqDbContext.cs:72.
Soft-delete is applied to BaseEntity deletes in src/Rafiq.Infrastructure/Persistence/RafiqDbContext.cs:81.
Global query filters exclude soft-deleted base entities in src/Rafiq.Infrastructure/Persistence/RafiqDbContext.cs:103.
All DateTime columns are normalized to datetime2(7) in src/Rafiq.Infrastructure/Persistence/RafiqDbContext.cs:127.
Migrations are under src/Rafiq.Infrastructure/Migrations.
No seed data was found.
Database Entities

Identity:
ApplicationUser extends Identity user with profile and medical document navigations.
Identity roles/users/claims/logins/tokens are standard ASP.NET Identity tables.
Health profile:
UserHealthProfiles
Allergies
ChronicDiseases
Auth/session:
RefreshTokens
PhoneVerifications
Otps
Medical records:
Prescriptions
PrescriptionMedicines
UserMedicines
MedicineReminders
LabReports
LabResults
ImagingReports
GeneralDocuments
Appointments
Database Relationships

User -> HealthProfile: one-to-one, unique user profile.
HealthProfile -> Allergies: one-to-many cascade.
HealthProfile -> ChronicDiseases: one-to-many cascade.
User -> Prescriptions: one-to-many cascade.
Prescription -> PrescriptionMedicines: one-to-many cascade.
User -> UserMedicines: one-to-many cascade.
UserMedicine -> MedicineReminders: one-to-many cascade.
User -> Appointments: one-to-many cascade.
User -> LabReports: one-to-many cascade.
LabReport -> LabResults: one-to-many cascade.
User -> ImagingReports: one-to-many cascade.
User -> GeneralDocuments: one-to-many cascade.
User -> RefreshTokens: one-to-many cascade.
Otp.UserId appears to have no configured FK.
Dependency Graph

RafiqAngular
-> Rafiq.API
-> Rafiq.Application
-> Rafiq.Domain
-> Rafiq.Infrastructure
-> Rafiq.Application
-> Rafiq.Domain
-> SQL Server database
-> Local file storage
-> Bedrock-compatible AI gateway
-> Twilio
-> Google token validation
Request Lifecycle

User interacts with Angular component or page.
Angular service builds request from environment.apiUrl.
authInterceptor attaches JWT for protected API URLs.
ASP.NET Core receives request through global exception middleware, CORS, authentication, and authorization.
Controller action receives DTO/command/form file.
Preferred path: controller sends command/query through MediatR.
ValidationBehavior runs FluentValidation validators.
Handler uses current user service, domain repositories, external services, and IUnitOfWork.
Repository queries/mutates EF Core DbContext.
DbContext applies soft-delete/audit behavior and saves to SQL Server.
Handler returns ApiResponse<T>.
Controller returns status code and response body.
Angular service maps response into page/component state.
Important Services

Backend:
IdentityService: registration, login, account, password, role/user orchestration.
TokenService: JWT creation.
TokenIssuingService: access/refresh token issuance.
RefreshTokenRepository: refresh-token persistence.
CurrentUserService: resolves current user from claims.
OtpService, OtpGenerator, BCryptOtpHasher: OTP flow.
GoogleTokenValidator: external Google token validation.
BedrockService: AI multimodal image analysis.
LocalFileStorageService: saves uploads under wwwroot/uploads.
MissedAppointmentsBackgroundService: marks expired appointments missed every 15 minutes.
Frontend:
AuthService: login/register/session/current user/refresh/logout.
TokenStorageService: localStorage tokens/user/onboarding flags.
HealthProfileService: patient profile create/read.
DashboardService: dashboard aggregation.
MedicalRecordsService: unified record fetching/mapping/deletion.
PdfService: record PDF export.
GoogleService: Google login button integration.
Implemented Features

Authentication: email/phone login, registration, JWT, refresh tokens, logout, revoke token, Google login.
Authorization: protected API controllers and Angular auth/guest guards.
User account: /auth/me, update account, change password.
Phone verification: verify/resend phone code.
Password reset: forgot password, verify reset OTP, reset password.
Patient profile/onboarding: demographics, blood type, height/weight, allergies, chronic diseases.
Medical records: labs, imaging, prescriptions, medicines, general documents.
File uploads: image-based uploads saved locally and reviewed client-side before final save.
AI/OCR-like extraction: Bedrock service analyzes lab reports, imaging reports, prescriptions, medicine boxes, and general documents.
Dashboard: recent medical records and inferred medicine reminder cards.
Medicine management: manual/add from prescription/scan box/update/delete.
Medicine reminders: CRUD and toggle endpoints exist.
Appointments: create/list/today/upcoming/update/delete/complete/cancel plus background missed status processing.
Notifications/SMS: Twilio service exists for OTP-like notification flows.
PDF exports: Angular generates PDFs for records.
SignalR: not implemented.
Payments: not found.
Emails: not found.
Coding Style Learned

C#:
File-scoped namespaces.
Nullable enabled.
Primary constructors are used for controllers/services/handlers.
sealed records/classes are common.
Commands/queries are MediatR IRequest<ApiResponse<T>>.
Validators use FluentValidation, often internal sealed validators.
Domain exceptions are thrown and mapped globally by middleware.
Repositories are interfaces in Domain and implementations in Infrastructure.
CancellationToken is used on most async APIs.
DTOs generally live under feature-specific DTOs folders.
API responses use ApiResponse<T>.SuccessResponse and FailureResponse.
Angular:
Standalone components and functional guards/interceptors.
inject() preferred over constructor injection.
Services are providedIn: 'root'.
State uses RxJS for auth and signals for UI-heavy pages.
Folders use PascalCase names: Pages, Services, Guards, Interceptors.
Models folder is misspelled as Modles and existing code follows that spelling.
Environment import path uses custom casing: ../Environments/Environment.
Potential Issues

src/Rafiq.API/appsettings.json:20 contains Twilio credentials and src/Rafiq.API/appsettings.json:12 contains a JWT secret; these should not be committed as plaintext secrets.
src/Rafiq.API/appsettings.json:3 contains a machine-specific SQL Server connection string.
RafiqAngular/src/app/Environments/Environment.ts:3 hardcodes the API URL and does not use Angular file replacements.
RafiqAngular/src/app/Environments/Environment.ts:5 hardcodes Google client ID.
CORS allows http://localhost:4200, while frontend API URL targets HTTPS localhost API; production environment strategy is absent.
Serilog packages are referenced, but no active Serilog host configuration was found.
No API versioning is implemented.
No SignalR or Hangfire despite possible realtime/background needs.
Some controllers bypass Application layer by constructing entities and calling repositories directly.
Duplicate IGoogleTokenValidator registration exists in API and Infrastructure.
GeneralDocument appears to lack a namespace, unlike other domain entities.
ApplicationUser/health profile relationship may create an unintended extra nullable FK/index because navigation configuration is incomplete.
Otp.UserId appears to lack a database FK.
AuthController.Logout is not protected by [Authorize], though it operates by refresh token body.
localStorage token storage exposes tokens to XSS risk.
Medical records Angular page is very large and mixes UI state, upload orchestration, API calls, and transformations.
DashboardService infers reminder times from medicine frequency instead of using the reminders API.
Some naming/casing inconsistencies exist: Services/auth, Modles, trailing spaces in filenames, and class/file mismatch for patient profile repository.
No wildcard Angular route exists.
Onboarding AI upload captures files but does not upload them before profile completion.
Suggested Improvements

Move secrets and connection strings to user secrets, environment variables, or a secret manager.
Add proper Angular environment configurations and file replacements for development/production.
Keep API controllers thin by moving save/update orchestration fully into Application commands.
Add role/policy-based authorization where business rules require it.
Fix EF relationship anomalies for health profile and OTP user FK.
Add API versioning if external/mobile clients are expected.
Add structured Serilog host setup or remove unused Serilog packages.
Split large Angular smart components into smaller services/components, especially medical records upload/review flows.
Replace dashboard inferred reminder times with real reminder endpoint data.
Consider safer token storage strategy if threat model includes XSS-sensitive data.
Add production CORS configuration.
Add missing wildcard route and not-found UI.
Expand tests beyond auth/profile into documents, medicines, reminders, appointments, uploads, and exception behavior.

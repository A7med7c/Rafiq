# Product

<!-- impeccable:product-schema 1 -->

## Platform

android

## Users

Primary users are families managing healthcare together through Rafiq: an adult "family manager" (often an adult child or spouse) who tracks appointments, medications, and medical records for themselves and dependents (children, elderly parents, spouse), plus elderly or less tech-fluent family members who may use the app directly with simplified, large-touch-target, low-cognitive-load screens. [Inferred from the app's existing feature set — appointments, medications, medical records, family profiles with managed/dependent profiles — and the explicit design brief's "Easy for elderly users" and "Family-oriented" goals.]

## Product Purpose

Rafiq ("companion" in Arabic) is an AI-assisted healthcare companion app that helps a family manage healthcare in one place: appointments, medication reminders, medical records, and a shared view of each family member's health status. Success means a family member (including elderly or dependent members without their own account) can be added once and have their care coordinated — reminders, records, appointments — without duplicate data entry or confusion about who is managing whom.

## Positioning

Family-centric health coordination with AI assistance (AI health summaries, an AI chat/assistant panel) layered on top of standard health-record/appointment/medication tracking — positioned as a companion for the whole family's care, not a single-user quantified-self tracker.

## Operating Context

Native mobile app (Angular 21 + Capacitor, targeting Android and iOS) with a fixed bottom-tab shell (Home / Records / Appointments / Medications / Profile) and a sticky top header on most pages. Bilingual: English and Egyptian-dialect-flavored Arabic via a centralized `LocalizationService`/`t()` signal, with `en.ts`/`ar.ts` kept in parallel — any new UI copy needs both. Auth-gated routes sit behind `authGuard`. A sibling web app, `RafiqAngular`, is the read-only canonical source of the original design/brand values; `RafiqMobile` (this repo) is the actively developed native-wrapped app.

## Capabilities and Constraints

- Family accounts model: a user can own/manage "Managed" dependent profiles (no linked login, e.g. a child or elderly relative without their own account) and can also be invited to "supervise" (Viewer or Manager access role) profiles owned by other users, and vice versa — invitations, access roles, and a two-way supervision relationship already exist in the current implementation (`AccessibleProfileDto`, `SentInvitationDto`, `ReceivedInvitationDto`, supervision CRUD).
- Existing family-profile data fields: name, relationship (fixed enum-like list), date of birth, gender, blood type, height, weight, allergies (name + severity), chronic diseases (name + status + diagnosed date), profile photo.
- No Emergency Contacts data model exists yet (no backend fields for a contact name/phone/relationship list). Decision for this round of work: ship Emergency Contacts as a UI-only placeholder/empty state, not backed by persistence, until a real data model exists.
- No granular per-member "permissions" data model exists yet beyond the existing Viewer/Manager access role. Decision for this round: new permission checkboxes (e.g. "can manage medications," "receive reminders," "emergency contact enabled") are new local UI state only, not wired to a real backend permissions system, unless one is discovered to already exist.
- Medical Records, Appointments, and Medications are existing top-level routes that already accept a `profileId` query param to show another family member's data (with a "viewing X's records" context banner pattern) — the Family Member Details page's navigation cards to these three should reuse this existing pattern rather than duplicating those pages.
- Health Information, Emergency Contacts, and Permissions do not exist as pages/routes yet. Decision for this round: build them as real new Angular routes/components (not modals), matching the brief's explicit "each card opens its own page" requirement.
- Family Profiles is currently a single flat route (`/family-profiles`) with everything — list, selected-member detail with tabs, and every modal (add, edit, invitations, supervision, remove) — handled in one large component (`family-profiles.ts`/`.html`/`.css`, over 1000 lines each). Decision for this round: split into real routes (list page + `/family-profiles/:id` detail + nav-card sub-pages) rather than keep it monolithic.
- Design tokens are centralized in `src/theme/tokens.css` (imported once via `src/styles.css`); some pages (e.g. `dashboard.css`) additionally redeclare the same tokens locally in their own `:host` block with minor drift (e.g. `--bg`/`--r`) — new work should use the global `tokens.css` values as the source of truth going forward rather than perpetuating per-page drift.
- Icon system app-wide is FontAwesome (`fa-solid`/`fa-regular`), not a custom SVG set.

## Brand Commitments

- Name: Rafiq. Brand gradient reserved for primary/AI actions: `linear-gradient(135deg, #0EAFD7 0%, #0891B2 100%)`.
- Established visual language across already-redesigned pages (Dashboard, Appointments, Medications, My Profile): white cards on a soft blue-tinted background, 16–20px corner rounding, soft multi-layer shadows, `'Outfit'` for display/headings and `'Inter'` for body text, FontAwesome icons, a shared `app-mobile-header` + `app-bottom-nav` shell.
- No new accent colors or additional pastel colors beyond the existing token palette (blue, green, red, orange, purple, teal, yellow, each with a light tint) may be introduced.

## Evidence on Hand

- `src/theme/tokens.css`, `src/app/Pages/dashboard/dashboard.css` — canonical color/spacing/radius/shadow/typography tokens and shared reusable classes (`.m-page`, `.m-body`, `.m-card`, `.m-section*`, `.pill*`, `.btn-primary`, skeleton loaders, bottom-sheet modal classes).
- `src/app/shared/mobile-header/*`, `src/app/shared/bottom-nav/*` — shared shell components and their actual Input/Output API (mobile-header has no AI-button slot despite being referenced informally elsewhere — only `title`, `showNotifications`, `showBack` inputs and `notificationsClicked`/`backClicked` outputs).
- `src/app/Pages/family-profiles/*` (current implementation, ~1000+ lines each across .ts/.html/.css) — full existing data model, signals, methods, and all current modals; treated as evidence of current behavior to preserve (API calls, validation, field lists), not as the visual target.
- `src/app/i18n/en.ts` / `ar.ts`, `family` key block (~150 keys) — existing bilingual copy for family-profiles; new copy must be added to both files in parallel, matching the existing key-naming and tone conventions.
- `SESSION_HANDOFF.md` at repo root — prior session notes; establishes the mobile-shell pattern (`.m-page`/`.m-body`/`app-bottom-nav`, modals rendered outside `.m-page` as overlay siblings) and the `notifSvc` (family-profiles/appointments/medications) vs `notifService` (my-profile) naming inconsistency to watch for.

## Product Principles

1. Never duplicate medical data across screens — each fact (allergies, blood type, medications) lives in exactly one place; other screens link to it rather than re-displaying it.
2. Navigation over exhaustive display: the family list and member hero surface only identity + status; depth lives one tap away in dedicated pages.
3. Elderly-accessible by default: large touch targets, generous whitespace, minimal simultaneous choices, no dense multi-column data dumps.
4. Reuse the established design system exactly (tokens, shared shell, FontAwesome icons) — this is a redesign of information architecture and visual polish, not a rebrand.
5. Bilingual parity is non-negotiable: every new string ships in both `en.ts` and `ar.ts` before the feature is considered done.

## Accessibility & Inclusion

Explicit design goal from the brief: interfaces must be comfortable for elderly users specifically — large touch targets, high legibility, low visual density, minimal cognitive load per screen (one clear action/decision at a time rather than dense dashboards).

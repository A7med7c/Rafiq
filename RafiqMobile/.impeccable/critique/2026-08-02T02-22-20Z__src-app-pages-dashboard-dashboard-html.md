---
target: RafiqMobile Dashboard (dashboard.html/ts/css + mobile-header + bottom-nav)
total_score: 22
max_score: 36
na_heuristics: 10
p0_count: 1
p1_count: 3
timestamp: 2026-08-02T02-22-20Z
slug: src-app-pages-dashboard-dashboard-html
---
Method: dual-agent (A: a12b15e89056327e9 · B: a0e00a361ca2ce54b)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Skeleton loaders everywhere (good); today-deck carousel gives no scroll-position feedback beyond 6px dots |
| 2 | Match System / Real World | 3 | Domain language correct (dosage, lab abnormal counts); "AI Active" pill is mildly techy for a caregiver audience |
| 3 | User Control and Freedom | 2 | Modals close only via 32×32px X or backdrop tap; report modal blocks close mid-generation unless "cancel" is found |
| 4 | Consistency and Standards | 2 | ~2300 lines of dead desktop-sidebar CSS (`.dsh`, `.mid-row`, `.bot-row`, `.sys-*`) coexist with the live `.m-*` mobile system; centered desktop-style modals contradict the native bottom-nav/swipe-deck patterns elsewhere on the same screen |
| 5 | Error Prevention | 3 | Report type selection shown with clear descriptions before commit; no destructive actions on this screen |
| 6 | Recognition Rather Than Recall | 3 | Icon+label pairing throughout; swipe-only carousel relies on users remembering the gesture exists |
| 7 | Flexibility and Efficiency | 2 | No reordering/pinning of the fixed 3-slide deck; no shortcuts |
| 8 | Aesthetic and Minimalist Design | 2 | AI Health Summary deck slide crams 6+ sub-sections (status, conditions, allergies, meds, labs, insights, recommendations) into one 168px-min-height swipe panel |
| 9 | Error Recovery | 2 | `hasLoadError` signal is tracked in `dashboard.ts` but no visible error banner renders in `dashboard.html` — failures look identical to empty states |
| 10 | Help and Documentation | n/a | Not applicable to this operate-surface screen (onboarding tour is a separate concern) |
| **Total** | | **22/36** | **Acceptable (61%)** |

## Design Specificity Verdict

**LLM assessment**: Content is genuinely health-domain-specific — conditions, allergy severity, lab abnormal counts, medication dosage/frequency, relationship labels. But the visual system is generic SaaS-dashboard chrome underneath: the same `linear-gradient(135deg, #0EAFD7 0%, #0891B2 100%)` powers the report button, primary modal button, and header AI button interchangeably; modals are centered desktop dialogs shrunk to fit rather than authored mobile-native. `dashboard.css:1-3` literally states "Pixel-perfect CSS / Matches uploaded reference screenshot exactly" — confirming this was built to replicate a reference mock, not designed mobile-first from the ground up. Nothing about warmth or reassurance appropriate to a family-health context is expressed structurally beyond copy strings.

**Deterministic scan**: `detect.mjs` returned exit code 0 / `[]` findings on `dashboard.html`, `mobile-header.html`, and `bottom-nav.html` — the static-HTML engine found no markup-level anti-patterns. Note this engine only parses markup, not computed/cascaded CSS, so it can't catch contrast ratios or effective rendered sizes; those came from the manual CSS grep instead (see Priority Issues below). No false positives to flag since there were zero findings.

**Visual overlays**: Not available. Live browser visualization was skipped — the target is an Angular app requiring dev-server compilation, which was out of scope for this pass. No user-visible overlay exists to point you to in a browser tab.

## Overall Impression

The underlying screen is more mobile-native than it first appears — mobile header, bottom tab bar, a swipeable "today" deck, skeleton loaders, and a family-member grid are all genuinely mobile UX patterns, not a shrunk desktop page. The gap is in execution polish and leftover cruft: roughly 2300 lines of unreferenced desktop-sidebar CSS still live in the same file as the mobile styles, several interactive touch targets fall under the 44×44pt minimum, the swipe carousel has zero accessible navigation, and the modals are still centered desktop dialogs instead of native-feeling bottom sheets. The single biggest opportunity: strip the dead CSS and convert the two remaining "shrunk desktop" patterns (modals, and the swipe-only carousel) into properly native-feeling ones — that alone would close most of the gap between "functional mobile page" and "feels like Apple Health."

## What's Working

1. **Skeleton-loading discipline** is consistent across every async section (appointments, family, records, reminders) — reduces layout shift, communicates "still loading" clearly.
2. **Today-deck's 86%-width slides** intentionally leave a sliver of the next card visible (`flex: 0 0 86%`, `dashboard.css:3102`) — legitimate swipe-discoverability affordance, even if not sufficient alone.
3. **Bottom nav's AI button** is a genuinely good native pattern: 52×52px circular FAB-style tab, clearly exceeds the 44pt minimum and sits in the natural thumb zone.

## Priority Issues

**[P0] Today-deck carousel has zero accessible/keyboard navigation.**
Why it matters: `.today-deck__dot` elements (`dashboard.html:246-250`) are bare non-interactive `<span>`s with no click handlers, ARIA roles, or labels. The only way to move between AI summary / appointment / tip slides is raw touch-scroll. A screen-reader or keyboard user (Sam) cannot reach the appointment or tip slides at all; a first-timer (Jordan) may never discover the deck is swipeable beyond the small peek of the next card.
Fix: Make the dots real `<button>` elements wired to `(click)="scrollToSlide(i)"` with `aria-label="Slide {{i+1}} of {{deckOrder().length}}"`; wrap the scroller in `role="region" aria-roledescription="carousel"`.
Suggested command: `/impeccable adapt` (or `/impeccable harden` for the a11y angle)

**[P1] Multiple interactive controls fall under the 44×44pt touch-target minimum.**
Why it matters: `.mobile-header__btn`/`.mobile-header__back` (36×36px), `.modal-close-btn` (32×32px), `.hdr-bell`/legacy toggles (30-38px), `.appt-add-btn` (~22-24px effective height), `.fam-summary-btn` (~30px effective height), `.report-option-radio` (18×18px) all fail Apple/Google's minimum — exactly the controls a distracted one-handed user (Casey) or motor-impaired user (Sam) is most likely to mis-tap.
Fix: Set `min-width: 44px; min-height: 44px` as the hit-area on every one of these buttons, keeping the visual glyph smaller via internal padding if the current visual size should stay.
Suggested command: `/impeccable adapt`

**[P1] Low-contrast text fails WCAG AA.**
Why it matters: `--text-4: #9CA3AF` on white (`dashboard.css:26`) is ~2.5:1 contrast, well under the 4.5:1 minimum for body text — used on `.rec-meta`, `.m-empty__sub`, and empty-state icon colors. This text is functionally unreadable for low-vision users.
Fix: Reserve `--text-4` for large decorative icons only; use a darker existing token (`--text-3` or darker) for any text ≤14px.
Suggested command: `/impeccable audit` then `/impeccable adapt`

**[P1] Modals are centered desktop dialogs, not native bottom sheets.**
Why it matters: `.modal-backdrop`/`.modal-box` (`dashboard.css:2534-2577`) center a 520px-max-width box with all-corner border-radius and a token 20px slide animation — a shrunk desktop pattern that contradicts the native feel the rest of the screen already establishes (bottom nav, swipe deck). This is also the mechanism behind heuristic #3's score (no swipe-to-dismiss, X button is the only exit and it's undersized).
Fix: Anchor `.modal-box` to `bottom: 0`, animate `translateY(100%) → 0`, round only the top corners, support swipe-down-to-dismiss.
Suggested command: `/impeccable adapt`

**[P2] Dead desktop-sidebar CSS (~2300 lines) coexists with the live mobile system, and one style override secretly depends on it.**
Why it matters: `.dsh`, `.dsh-sb`, `.sb-*`, `.mid-row`, `.bot-row`, `.sys-*` and their three `@media` breakpoints (`dashboard.css:2264-2529`) are entirely unreferenced by the current `dashboard.html` template — confirmed by both the detector pass finding zero markup references and the manual review. Worse, `.family-grid`'s mobile 1-column override only exists inside that dead `@media (max-width:768px)` block; on any viewport ≥768px (tablet, phablet landscape, foldable) the base `repeat(4, 1fr)` rule (line ~1505) reasserts itself with no sidebar to compensate, producing an illegible squeezed 4-column grid. There are also 7 duplicate-selector definitions with conflicting values across the live/dead boundary (`.tip-ico-wrap`, `.tip-ico`, `.tip-text`, `.tip-link` each defined twice — once in the dead desktop tip-bar, once in the live mobile tip-card, with different literal-hex vs. `var()` colors), plus 6 hardcoded `#0EAFD7` literals that should be `var(--blue)`, and 4 `!important` uses on `.fam-card--empty`.
Fix: Delete the entire dead block; hardcode `.family-grid { grid-template-columns: 1fr; }` (or `repeat(2,1fr)`) directly with no media-query dependency, since this is a phone-only shell with no desktop variant to differentiate from.
Suggested command: `/impeccable distill`

**[P2] AI Health Summary deck slide is overloaded for a single swipe panel.**
Why it matters: Overall status, conditions, allergy pills, medication count/issues, lab status, insights, and recommendations are all crammed into one scrollable slide with `min-height: 168px` — high intrinsic + extraneous cognitive load right after a warm greeting, creating tonal whiplash from reassurance to clinical density in one screen-height.
Fix: Split into a compact "status glance" (overall status + top 1 flag) on the card face, with the rest reachable via the existing "View Full Summary" tag rather than pre-rendered inline.
Suggested command: `/impeccable distill`

**[P3] RTL scroll-index calculation likely desyncs for Arabic users.**
Why it matters: `onDeckScroll` (`dashboard.ts:181-186`) reads raw `el.scrollLeft`, whose sign/range convention differs across browsers under `dir="rtl"`; only a handful of `[dir="rtl"]` CSS overrides exist elsewhere in the file, none addressing this JS calculation. The active-dot indicator will likely invert or desync for Arabic-language users, who are an explicit target audience given the app's RTL support elsewhere.
Fix: Normalize `scrollLeft` against computed `direction` before computing the active index; test explicitly in `dir="rtl"`.
Suggested command: `/impeccable harden`

## Persona Red Flags

**Casey (distracted, one-handed mobile user)**: Primary "Medical Report" CTA sits top-right of the greeting card — upper-screen, requiring a reach in one-handed use; nothing critical besides the bottom nav itself sits in the natural thumb arc. Multiple tap targets Casey would hit quickly (`.appt-add-btn`, header icon buttons) are under 44px, raising mis-tap risk while walking/distracted.

**Sam (accessibility-dependent — screen reader / motor / low vision)**: Cannot operate the today-deck carousel at all via keyboard or screen reader (no roles, no focusable controls). `--text-4` body text is ~2.5:1 contrast, functionally invisible. The only modal-dismiss control is a 32×32px close button.

**Jordan (confused first-timer)**: No visible affordance signals the deck is swipeable beyond a small peek of the next card and two 6px dots — easy to miss, especially since a fade/slide-in animation plays on load and can distract from noticing the peek. The family grid always renders exactly 4 slots — with only themselves in the family, Jordan sees their own card, an "Add Family Member" card, and then a **third, fully blank spacer div** with no border, background, or explanation (still carrying the card's 18px padding as dead whitespace). Once a family reaches exactly 4 members, the "Add Family Member" card silently disappears from the dashboard with no indication a 5th member can still be added elsewhere.

## Minor Observations

- The wand-sparkles icon (`fa-wand-magic-sparkles`) is reused for four different destinations (header AI button, bottom-nav AI tab, "AI Health Summary" label, family-member summary button) with no visual differentiation besides label text.
- `dashboard.ts:424-431` defines `getTruncatedSummary`/`isSummaryTruncatable` (a "read more" truncation helper) that isn't called anywhere in the current template — dead code left over from an earlier iteration of the AI summary card.
- Failed data loads (`hasLoadError` signal) are tracked but never surface a visible error banner/toast — errors are visually indistinguishable from legitimate empty states.

## Questions to Consider

- What would it take for the greeting-to-deck transition to feel like reassurance first, dense data second, rather than the reverse?
- If the modals became true bottom sheets, would the medical-report flow (picker → type selection → generating → done) feel more like a single continuous native flow instead of three stacked dialogs?
- Does the today-deck need to be swipe-first at all, or would a simple vertically-stacked "AI summary card, then appointment card, then tip card" (no carousel) actually serve Casey and Jordan better than a carousel that hides two of three cards by default?

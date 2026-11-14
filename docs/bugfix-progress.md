# Review fixes

Fixes follow the numbered findings in `code-review-2026-11-14.md`. Each completed finding receives its own commit. Existing unrelated local files are excluded.

| Finding | Status | Validation |
| --- | --- | --- |
| 1 — editor-only import in player builds | Fixed | Player-source compilation check |
| 2 — locale-dependent model numbers | Fixed | Variable round trips under en-US, fr-FR, de-DE; player-source compilation |
| 3 — unsafe Lua string serialization | Fixed | Exact string round trips including nested comments, long strings, control characters and Unicode |
| 4 — regex rewriting of valid Lua | Fixed | Production Lua environment tests for comparisons, inline statements, locals, strings, comments and table fields |
| 5 — sensor/script initialization failures | Fixed | Single initialization after all sensor bindings; syntax/runtime error recovery checks; player-source compilation |
| 6 — unbounded Lua/model execution | Fixed | Instruction budgets on vehicle/challenge callbacks, no executable model parsing, source/object limits; infinite-loop and bundled-model regressions |
| 7 — malformed models and missing cores | Fixed | Pre-publication schema/tree validation; empty, duplicate, missing-parent, cyclic and invalid-option cases; bundled model compatibility |
| 8 — shipped Up Arrow velocity boost | Fixed | Removed debug force path; player-source compilation |
| 9 — remote disconnect navigates everyone to the menu | Fixed | Navigation moved to Mirror's local-player-only teardown hook; player-source compilation (multi-instance runtime check pending) |
| 10 — render-dependent vehicle controls | Fixed | Fixed 50 Hz control clock/backstep across 30–1000 Hz samples; buffered input-edge checks; Lua DeltaTime exposed |
| 11 — zero accelerometer readings | Fixed | Cached physics-tick velocity differences; Unity regression added; player/editor-source compilation |
| 12 — inconsistent multiplayer worlds | Fixed | Host map announcements/content checks and spawn resets; remote map selection blocked; scripted challenges restricted to solo sessions; compile checks (multi-instance runtime pending) |
| 13 — remote variable/effect presentation | Fixed | Observer values applied before presentation; server-authoritative shot events with harmless client bullets; observer/bullet regressions added and compiled |
| 14 — host-resolution mouse normalization | Fixed | Normalize at the sender, consume normalized coordinates on the host; resolution/neutral/nonfinite checks |
| 15 — redo branches and remote undo | Fixed | Server-owned undo/redo commands, serialized pending edits, bounded history; empty/undo/redo/new-branch regressions |
| 16 — cubic virtual model linking | Fixed | One parent lookup and ID dictionary; iterative virtual subtree deletion; tree regression added and compiled |
| 17 — sound sources retained across rebuilds | Fixed | Owned sources stopped/released and runtime callbacks removed; repeated lifecycle regression added and compiled |
| 18 — retained runtime child/model trees | Fixed | Clear both attachment directions and variable subscriptions before rebuild/destruction; reparent/clear regression added and compiled |
| 19 — detached drives retain commands | Fixed | Stop drive commands on the detached branch and replicate death state; wheels can coast; detachment regression added and compiled |
| 20 — unbounded/zero-power gun allocation | Fixed | Lazy pool capped at 256 bullets, dynamic finite power, max 50 shots/sec, physics-tick charge; rate/zero/tiny/nonfinite power regressions |
| 21 — inconsistent impact/recoil mass | Fixed | One finite positive projectile mass shared by recoil and reused bullet bodies; mass regression added and compiled |
| 22 — missing wheel support reaction | Fixed | Normal and tangential forces share equal/opposite contact distribution; dynamic-platform momentum regression added and compiled |
| 23 — fresh-install volume/particle defaults | Fixed | All preference reads supply their declared defaults; volume/particle setters apply runtime values; player-source compilation |
| 24 — clearing numeric settings throws | Fixed | Single parse with invalid-input feedback/restoration; clamp valid integers to the documented range; player-source compilation |
| 25 — vehicles saved into installed content | Fixed | User-data model store, non-overwriting legacy migration, bundled fallback; migration/user override/new-example filesystem regressions |
| 26 — direct overwrite and missing confirmation | Fixed | Atomic replacement/backups, explicit second-step overwrite, errors keep the save dialog open; injected partial-write regression |
| 27 — editor help cannot receive dismiss input | Fixed | F1 transfers input focus, including from editor side panels; player-source compilation |
| 28 — independent messages overwrite each other | Fixed | Unique transient IDs with expiry/cap, explicit reusable Lua print slots, canvas reacquisition; message lifecycle regression added and compiled |
| 29 — best times ignore changed maps | Fixed | Run identity includes loaded map content, run stores map identity, browser verifies installed geometry, legacy times remain separate; map-change regressions |
| 30 — fragile Lua key discovery | Fixed | Transmit supported held/edge states without scanning source; uppercase, digits/keypad, space/control keys and malformed packets handled; key regressions |
| 31 — variable initialization and validation | Fixed | Initialize validated bounds before value; reject invalid names, nonfinite settings, inverted/default-outside ranges and negative backstep before mutation; variable regressions |
| 32 — save/load filenames and premature selection | Fixed | One safe filename/display rule, optional .txt, case-compatible lookup, load error recovery, selected name updated only after server acceptance; filename regressions |
| Additional — repeated menu entry/exit side effects | Fixed | Idempotent selection transitions; lifecycle regression added and compiled |
| Additional — stale/empty keyboard focus | Fixed | Start first receiver, safely remove last receiver and discard inactive/destroyed receivers; focus regression added and compiled |
| Additional — legacy audio singleton | Fixed | Cache/discover the instance, clear destroyed instances and initialize empty sound lists safely; Unity lifecycle regression added |

Unity Play-mode and multiplayer checks require separate runtime validation; a C# compilation check does not exercise Mirror weaving or physics.

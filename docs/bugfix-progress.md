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

Unity Play-mode and multiplayer checks require separate runtime validation; a C# compilation check does not exercise Mirror weaving or physics.

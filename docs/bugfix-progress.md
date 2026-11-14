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

Unity Play-mode and multiplayer checks require separate runtime validation; a C# compilation check does not exercise Mirror weaving or physics.

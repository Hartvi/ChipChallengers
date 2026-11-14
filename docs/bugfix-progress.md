# Review fixes

Follow-up redo 1: unfinished Lua is accepted as editable model data and can be saved/reloaded. Syntax errors disable the runtime program with feedback instead of rejecting and replacing the draft. Structural validation and execution budgets remain in force. Native draft save/load and safe initialization regression passed.

Follow-up redo 2: sensor pastes allocate unique names across existing sensors, variables and the entire pasted batch. Mirrored/repeated pastes and legacy unnamed sensors are supported. Type conversion to Sensor also allocates a free name; variable/sensor conflicts are rejected before mutation. Three native paste regressions passed.

Follow-up redo 3: chip type changes preserve the selected option when the destination supports the same named variant. Incompatible variants fall back to Basic/the first option. Eight native tests cover material preservation, incompatible options and exactly one model-change notification.

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
| 9 — remote disconnect navigates everyone to the menu | Fixed | Navigation moved to Mirror's local-player-only teardown hook; host and remaining client stayed in sandbox after another client disconnected |
| 10 — render-dependent vehicle controls | Fixed | Fixed 50 Hz control clock/backstep across 30–1000 Hz samples; buffered input-edge checks; Lua DeltaTime exposed |
| 11 — zero accelerometer readings | Fixed | Cached physics-tick velocity differences; Unity regression added; player/editor-source compilation |
| 12 — inconsistent multiplayer worlds | Fixed | Host map announcements/content checks and spawn resets; remote map selection blocked; scripted challenges restricted to solo sessions; host/two-client map change and solo restriction passed |
| 13 — remote variable/effect presentation | Fixed | Observer values applied before presentation; server-authoritative shot events with harmless client bullets; observer/bullet regressions added and compiled |
| 14 — host-resolution mouse normalization | Fixed | Normalize at the sender, consume normalized coordinates on the host; resolution/neutral/nonfinite checks |
| 15 — redo branches and remote undo | Fixed | Server-owned undo/redo commands, serialized pending edits, bounded history; empty/undo/redo/new-branch regressions |
| 16 — cubic virtual model linking | Fixed | One parent lookup and ID dictionary; iterative virtual subtree deletion; tree regression added and compiled |
| 17 — sound sources retained across rebuilds | Fixed | Owned sources stopped/released and runtime callbacks removed; repeated lifecycle regression added and compiled |
| 18 — retained runtime child/model trees | Fixed | Clear both attachment directions and variable subscriptions before rebuild/destruction; reparent/clear regression added and compiled |
| 19 — detached drives retain commands | Fixed | Stop drive commands on the detached branch and replicate death state; wheels can coast; detachment regression added and compiled |
| 20 — unbounded/zero-power gun allocation | Fixed | Lazy pool capped at 256 bullets, dynamic finite power, max 50 shots/sec, physics-tick charge; rate/zero/tiny/nonfinite power regressions |
| 21 — inconsistent impact/recoil mass | Fixed | Explicit projectile mass and recoil attenuation; the follow-up below restores the original standard-shot knockback while preserving recoil tuning |
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
| Additional — item sibling navigation | Fixed | Traverse the parent's actual items, handle inactive gaps/empty lists and ignore stale cached indices; Unity regression added |
| Additional — UI item pool growth after hiding rows | Fixed | Grow by actual missing objects instead of reactivating existing rows; empty hide is safe; Unity regression added |
| Additional — standalone test README | Fixed | Corrected the runnable project path to Tests/ScenarioRecording |
| Additional — misleading sensor option labels | Fixed | Labels now describe angular velocity, forward direction and velocity without changing saved indices or sensor outputs |
| Additional — legacy sound reservations | Fixed | Per-sound bounded pools, positive limits, safe missing sounds and owned-source teardown; Unity regression added |
| Additional — self-closing input receivers | Fixed | Remove obsolete bookkeeping that indexed an empty stack after a receiver closed itself during Update; regression expanded |
| Additional — detachment after the joint is already gone | Fixed | Avoid requesting destruction of a missing joint; native detachment regression |
| Multiplayer validation — client physics warning flood | Fixed | Use Mirror's silent ServerCallback guard for Unity's automatic FixedUpdate callback |
| Multiplayer validation — map files opened exclusively for writing | Fixed | Minimal OBJ importer patch opens geometry/materials read-only with shared readers, supporting protected installs and simultaneous local instances; native regression added |

## Initial review verification

- Standalone production-code checks: **145 vehicle/runtime**, **301 challenge/save**, and **15 bundled challenge** checks passed.
- Unity **2021.3.16f1**: **31/31 EditMode tests passed**, including native physics, observer variable updates, model trees, aspect/audio ownership, menus, focus and shared read-only map access.
- Full **Windows x64 player build succeeded with zero errors**, including Mirror's actual IL postprocessing/weaving.
- Three separate headless Windows players passed the host/two-client check: joins, host map change, remote undo/redo on both clients, three further resets per client, solo challenge restriction, clean disconnect, and preservation of sandbox on the host and remaining client.
- After initial setup settled, both clients retained **75 AudioSources and 60 aspect registrations** through all three resets.
- Native validation ran in a detached temporary worktree. Its validation-only build uses a separate company/product name and save directory. The open editor and ordinary player saves were left intact.

The reproducible build/probe sources and instructions are in `Tests/UnityValidation`. The probe remains outside `Assets` in the normal project. An early native run exposed incomplete Mirror lifecycle setup in some test fixtures; those fixtures were corrected before the passing run.

The headless checks do not assess visual design, audible mixing, long-session handling or internet latency. An editor tutorial and menu redesign remain feature work, as described in the original review.

## Bullet knockback follow-up

The user reported that a free rolling ball no longer flew away when shot. Commit `be9f580` had reduced the default 100-power projectile from the prefab's original 1 kg to 0.01 kg while unifying mass calculations. That reduced impact momentum from 333 to 3.33 N·s. The earlier impact test manually overrode bullet mass, so it did not catch the gameplay regression.

The projectile now weighs 1 kg at the standard power, with mass proportional to power. An explicit 1% recoil factor preserves the previous gun kick. This attenuation is intentional gameplay tuning; the target receives the full projectile momentum. Authority checks, exclusion of the firing vehicle, damage and shot rate are preserved.

A native regression fires a normal 333 m/s shot, without overriding its mass, at a rolling 10 kg, five-unit-diameter ball on a floor. Before the fix, both 500 and 1000 Hz cases failed with only about 0.24 m/s remaining velocity. Both now pass the launch checks: more than 20 m/s and over one metre of movement within 0.1 seconds.

Follow-up verification: **33/33 native Unity tests** and **146 vehicle/runtime checks** passed. The Windows build and multiplayer results above refer to the initial review validation.

## Variable panel Enter follow-up

The focus guard introduced in `3617dbd` could skip Enter after TextMeshPro had already consumed the key and cleared input focus. Variable fields now use TMP's own submit event for Return and keypad Enter, with no duplicate global Enter polling. Submission retains default filling and definition validation, and typing focus returns to the name field after TMP finishes deactivating the submitted input.

Native regressions submit each of the five fields without relying on polled focus, verify adding and updating exactly once, and verify that a hidden panel ignores submissions. All five cases failed before the fix. **38/38 native Unity tests passed** after the fix.

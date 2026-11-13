# Driving introduction

Presentation uses `IntroductionOverlay : BasePanel`, declared with `VirtualProp` and the shared `BaseImage`, `BaseText`, and `BaseButton` prefabs. Both the offer menu and sandbox declare the overlay as a child in their existing UI layout. Tutorial flow and vehicle holds remain in `DrivingIntroduction`.

**Learn to play** in the main menu loads the built-in `Resources/aguncar` car on `01_industrial_test_track_race`. The first visit to **Challenges** offers the lesson with **Start introduction**, **Browse challenges**, and **Back**. Browsing or starting saves the offer choice; backing out leaves it available. Completing the lap saves completion separately. Learn to play remains available for replay.

The welcome overlay blocks camera/menu shortcuts and holds all vehicle bodies and model logic. Escape still returns to the menu. **Let's drive** releases the introduction hold so the player can drive into the starting gate. Crossing that gate starts the normal race countdown. The countdown has its own hold, so releasing either hold cannot release the other.

During the lap, the top-left prompt teaches W/S, A/D, and C. The acceleration prompt guides the player to the starting gate; steering is introduced on the opening straight after the countdown, before the first checkpoint/bend. Braking is introduced after steering and the first checkpoint, before the second checkpoint. Actual deceleration under braking advances to the final lap objective. A stalled player gets a Ctrl+R reminder; restarting reloads the guided lap and its welcome card. The finish offers a normal race, more challenges, or the editor. Skipping starts a fresh normal race without marking the introduction complete.

Leaving the introduction restores the previously loaded model and model name. **Try building** deliberately keeps the default car for editing. Normal challenges do not enable lesson prompts or holds, and the existing challenge Lua, track, and scoring are unchanged.

Preferences: `Introduction.OfferDismissed.v1` and `Introduction.Completed.v1`. Set both to zero to see the first-time offer again during development.

Validation: run `dotnet run --project Tests/ScenarioRecording/ScenarioRecording.Tests.csproj`. In Unity Play mode, check first-time offer/browse/replay, welcome control blocking, countdown release, Ctrl+R, skipping, exit/model restoration, lesson hints and finish navigation. Also check the overlay at the smallest supported game resolution.

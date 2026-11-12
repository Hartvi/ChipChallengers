# Driving introduction

**Learn to play** in the main menu loads the built-in `Resources/aguncar` car on `01_industrial_test_track_race`. The first visit to **Challenges** offers the lesson with **Start introduction**, **Browse challenges**, and **Back**. Browsing or starting saves the offer choice; backing out leaves it available. Completing the lap saves completion separately. Learn to play remains available for replay.

The welcome overlay blocks camera/menu shortcuts and holds all vehicle bodies and model logic. Escape still returns to the menu. **Let's drive** releases the introduction hold and starts the existing race countdown. The countdown has its own hold, so releasing either hold cannot release the other.

During the lap, the top-left prompt teaches W/S, A/D, and C. It advances using movement, checkpoint progress, steering input, and actual deceleration under braking. X is mentioned as the gun control but is not a lesson objective. A stalled player gets a Ctrl+R reminder; restarting reloads the guided lap and its welcome card. The finish offers a normal race, more challenges, or the editor. Skipping starts a fresh normal race without marking the introduction complete.

Leaving the introduction restores the previously loaded model and model name. **Try building** deliberately keeps the default car for editing. Normal challenges do not enable lesson prompts or holds, and the existing challenge Lua, track, and scoring are unchanged.

Preferences: `Introduction.OfferDismissed.v1` and `Introduction.Completed.v1`. Set both to zero to see the first-time offer again during development.

Validation: run `dotnet run --project Tests/ScenarioRecording/ScenarioRecording.Tests.csproj`. In Unity Play mode, check first-time offer/browse/replay, welcome control blocking, countdown release, Ctrl+R, skipping, exit/model restoration, lesson hints and finish navigation. Also check the overlay at the smallest supported game resolution.

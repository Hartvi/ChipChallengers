using System.IO;
using Mirror;
using UnityEngine;

public class DrivingIntroduction : MonoBehaviour
{
    public const string ChallengeName = "01_industrial_test_track_race";
    public const string OfferDismissedKey = "Introduction.OfferDismissed.v1";
    public const string CompletedKey = "Introduction.Completed.v1";
    public static bool ShouldOffer => PlayerPrefs.GetInt(OfferDismissedKey, 0) == 0 &&
        PlayerPrefs.GetInt(CompletedKey, 0) == 0;

    IntroductionOverlay overlay;
    CoreChip core;
    GameScript race;
    DrivingIntroductionProgress progress;
    string previousModel;
    string previousModelName;
    string promptText;
    bool active;
    bool welcomed;
    bool complete;
    bool keepModel;
    float lastProgressAt;
    int lastCheckpoint;
    DrivingIntroductionProgress.Lesson lastLesson;
    public bool BlocksControls { get; private set; }

    public static void DismissOffer()
    {
        PlayerPrefs.SetInt(OfferDismissedKey, 1);
        PlayerPrefs.Save();
    }

    public static void Launch()
    {
        if (!File.Exists(Path.Combine(IOHelpers.GetChallengesDirectory(), ChallengeName + UIStrings.ChallengeExtension)))
        {
            Debug.LogWarning("The introduction track is missing.");
            DisplaySingleton.Instance.DisplayText(text => {
                DisplaySingleton.ErrorMsgModification(text);
                text.SetText("The introduction track is missing. Choose another challenge.");
            }, 4f);
            GoToChallenges.Browse();
            return;
        }
        DismissOffer();
        GameManager.PlayContext.SelectIntroduction(ChallengeName);
        GoToSandbox.Function();
    }

    public void Begin(CoreChip player)
    {
        this.core = player;
        this.previousModel = player.modelString;
        this.previousModelName = GameManager.Instance.GetModel();
        this.keepModel = false;
        this.active = true;
        this.progress = new DrivingIntroductionProgress();
        this.overlay = this.overlay != null ? this.overlay : IntroductionOverlay.Create(this.transform);
        this.SetHeld(true);
        this.overlay.ShowModal("Your first drive", "Preparing your car and the practice lap...",
            null, null, "Back to menu", () => GoToMainMenu.Function(false));
        var model = Resources.Load<TextAsset>("aguncar");
        if (model == null) { this.ShowError("The default car could not be loaded."); return; }
        GameManager.Instance.SetModel("Default car");
        player.CmdLoadString(model.text);
        this.welcomed = false;
        this.complete = false;
        this.race = null;
        this.promptText = null;
        this.lastCheckpoint = 0;
        this.lastLesson = DrivingIntroductionProgress.Lesson.Accelerate;
        this.lastProgressAt = Time.unscaledTime;
    }

    // A retry reloads the same lesson and shows its opening card after the new race is ready.
    public void PrepareRetry()
    {
        if (!this.active) { return; }
        this.SetHeld(true);
        this.race = null;
        this.welcomed = false;
        this.complete = false;
        this.progress = new DrivingIntroductionProgress();
        this.promptText = null;
        this.lastCheckpoint = 0;
        this.lastLesson = DrivingIntroductionProgress.Lesson.Accelerate;
        this.lastProgressAt = Time.unscaledTime;
        this.overlay.ShowModal("Your first drive", "Resetting the practice lap...", null, null,
            "Back to menu", () => GoToMainMenu.Function(false));
    }

    void Update()
    {
        if (!this.active) { return; }
        if (this.core == null || NetworkClient.localPlayer == null)
        {
            this.End();
            return;
        }
        if (!GameManager.PlayContext.IsIntroduction) { this.End(); return; }
        this.race = SandboxMenu.myVChallenge.Runtime;
        if (!this.welcomed)
        {
            if (this.race != null && this.race.RaceReady && !this.core.IsRebuilding && !this.core.HasPendingPoseReset)
            {
                this.welcomed = true;
                this.lastProgressAt = Time.unscaledTime;
                this.overlay.ShowModal("Your first drive",
                    "Drive through the gates in order to complete a lap.\n\n" +
                    "W / S  Accelerate / reverse     A / D  Steer\nC  Brake\n\n" +
                    "We'll guide you as you go. X fires the gun; you won't need it on this lap.",
                    "Let's drive", this.StartDriving, "Skip introduction", this.Skip);
            }
            else if (Time.unscaledTime - this.lastProgressAt > 30f)
            {
                this.ShowError("The practice lap could not start. Return to Challenges and try again.");
            }
            return;
        }
        if (this.BlocksControls || this.complete || this.race == null) { return; }
        this.progress.Observe(this.race.RaceHasStarted, this.race.RaceHasFinished, this.race.PassedCheckpoints,
            this.core.rb.velocity.magnitude, Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.C));
        if (this.progress.Current == DrivingIntroductionProgress.Lesson.Complete)
        {
            this.Complete();
            return;
        }
        if (this.race.PassedCheckpoints != this.lastCheckpoint || this.progress.Current != this.lastLesson)
        {
            this.lastCheckpoint = this.race.PassedCheckpoints;
            this.lastLesson = this.progress.Current;
            this.lastProgressAt = Time.unscaledTime;
        }
        string message = this.Message();
        if (Time.unscaledTime - this.lastProgressAt > 25f && !this.race.RaceCountingDown)
        {
            message += "\nStuck? Ctrl+R restarts the lap. Esc returns to the menu.";
        }
        if (message != this.promptText)
        {
            this.promptText = message;
            this.overlay.ShowPrompt(message, this.Skip);
        }
    }

    string Message()
    {
        if (this.race.RaceCountingDown) { return "Get ready!\nYour car will be released after the countdown."; }
        if (!this.race.RaceReady) { return "Lap interrupted.\nPress Ctrl+R to restart, or Esc to leave."; }
        switch (this.progress.Current)
        {
            case DrivingIntroductionProgress.Lesson.Accelerate:
                return "1 / 4   Get moving\nHold W and drive through the first gate. S reverses.";
            case DrivingIntroductionProgress.Lesson.Steer:
                return "2 / 4   Follow the bend\nUse A / D to steer through the next gate.";
            case DrivingIntroductionProgress.Lesson.Brake:
                return "3 / 4   Control your speed\nHold C to slow down for a corner.";
            default:
                return "4 / 4   Finish your lap\nFollow the gates. Ctrl+R restarts; F1 shows controls.";
        }
    }

    void StartDriving()
    {
        if (this.race == null || !this.race.RaceReady) { return; }
        this.SetHeld(false);
        this.lastProgressAt = Time.unscaledTime;
        this.race.StartIntroductionRace();
        this.overlay.ShowPrompt(this.Message(), this.Skip);
    }

    void Complete()
    {
        this.complete = true;
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
        this.SetHeld(true);
        this.overlay.ShowModal("First lap complete!",
            "You can drive, steer and brake. Try beating your time, explore another challenge, or build your own vehicle.",
            "Race again without hints", this.Skip, "More challenges", GoToChallenges.Browse,
            "Try building", this.TryBuilding);
    }

    void Skip()
    {
        // Start a fresh normal race so skipping never resumes halfway through a guided attempt.
        this.End();
        SandboxMenu.PlayChallenge(ChallengeName);
    }

    void TryBuilding()
    {
        this.keepModel = true;
        GameManager.PlayContext.SelectSandbox();
        GoToEditor.Function();
    }

    void ShowError(string message)
    {
        this.welcomed = true;
        this.overlay.ShowModal("Introduction unavailable", message, "Browse challenges", GoToChallenges.Browse,
            "Back to menu", () => GoToMainMenu.Function(false));
    }

    void SetHeld(bool held)
    {
        this.BlocksControls = held;
        if (this.core != null && NetworkClient.active) { this.core.CmdSetIntroductionHeld(held); }
    }

    public void End()
    {
        if (!this.active) { return; }
        this.active = false;
        this.SetHeld(false);
        if (this.overlay != null) { this.overlay.Hide(); }
        if (!this.keepModel && this.core != null && NetworkClient.active && this.previousModel != null)
        {
            this.core.CmdLoadString(this.previousModel);
            GameManager.Instance.SetModel(this.previousModelName);
        }
        if (GameManager.PlayContext.IsIntroduction) { GameManager.PlayContext.SelectSandbox(); }
        this.core = null;
        this.race = null;
    }

    void OnDisable() => this.End();
}

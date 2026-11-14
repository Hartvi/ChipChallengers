using System;
using Mirror;

public static class WorldSession
{
    public static string LastError;
    public static bool IsRemoteClient => NetworkClient.active && !NetworkServer.active;
    public static bool IsMultiplayer => IsRemoteClient || (NetworkServer.active && NetworkServer.connections.Count > 1);

    public static bool RequireSoloChallenge()
    {
        if (!IsMultiplayer) { return true; }
        Report("Challenges require a solo session. Leave multiplayer before starting one.");
        return false;
    }

    public static void Report(string message)
    {
        DisplaySingleton.Instance.DisplayText(text => {
            DisplaySingleton.ErrorMsgModification(text);
            text.SetText(message);
        }, 6f, "world-session-error");
    }
}

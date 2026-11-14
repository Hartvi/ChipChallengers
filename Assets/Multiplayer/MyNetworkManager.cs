using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.IO;

public class MyNetworkManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        SandboxMenu.myVMap.Loaded += this.OnMapLoaded;
    }

    public override void OnStopServer()
    {
        SandboxMenu.myVMap.Loaded -= this.OnMapLoaded;
        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<WorldStateMessage>(this.ApplyWorld);
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<WorldStateMessage>();
        base.OnStopClient();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient connection)
    {
        if (!GameManager.PlayContext.IsChallenge || connection is LocalConnectionToClient)
        {
            base.OnServerAddPlayer(connection);
            connection.identity.GetComponent<CoreChip>()?.ServerResetToMapSpawn();
        }
        // A solo challenge's local scripted objects are not a shared multiplayer world.
        connection.Send(this.CaptureWorld());
    }

    WorldStateMessage CaptureWorld() => new WorldStateMessage {
        mapFile = SandboxMenu.myVMap.FileName + UIStrings.MapExtension,
        mapHash = SandboxMenu.myVMap.ContentHash,
        challengeActive = GameManager.PlayContext.IsChallenge
    };

    void OnMapLoaded()
    {
        foreach (var identity in NetworkServer.spawned.Values)
        {
            var core = identity.GetComponent<CoreChip>();
            if (core != null) { core.ServerResetToMapSpawn(); }
        }
        NetworkServer.SendToAll(this.CaptureWorld());
    }

    void ApplyWorld(WorldStateMessage state)
    {
        if (NetworkServer.active) { return; }
        try
        {
            if (state.challengeActive) { throw new InvalidOperationException("This host is playing a solo challenge. Join after it returns to sandbox."); }
            if (string.IsNullOrEmpty(state.mapFile) || Path.GetFileName(state.mapFile) != state.mapFile ||
                !state.mapFile.EndsWith(UIStrings.MapExtension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The host selected an invalid map name.");
            string path = Path.Combine(IOHelpers.GetMapsDirectory(), state.mapFile);
            if (!File.Exists(path) || ChallengeContentHash.Compute(Array.Empty<byte>(), File.ReadAllBytes(path)) != state.mapHash)
                throw new InvalidDataException("Your map does not match the host's " + state.mapFile + ". Install the matching map before joining.");
            if ((!SandboxMenu.myVMap.IsLoaded || SandboxMenu.myVMap.FileName + UIStrings.MapExtension != state.mapFile || SandboxMenu.myVMap.ContentHash != state.mapHash) &&
                !SandboxMenu.myVMap.LoadNewMap(state.mapFile, fromHost: true))
                throw new InvalidDataException("The host's map could not be loaded.");
        }
        catch (Exception error)
        {
            WorldSession.LastError = error.Message;
            WorldSession.Report(error.Message);
            this.StopClient();
        }
    }
}

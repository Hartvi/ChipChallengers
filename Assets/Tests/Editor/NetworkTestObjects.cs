using System;
using System.Reflection;
using Mirror;
using UnityEngine;

// Edit-mode fixtures must reproduce the identity setup Mirror performs in Awake.
public static class NetworkTestObjects
{
    public static T Add<T>(GameObject root) where T : NetworkBehaviour
    {
        var identity = root.GetComponent<NetworkIdentity>();
        if (identity == null) identity = root.AddComponent<NetworkIdentity>();
        var component = root.AddComponent<T>();
        typeof(NetworkIdentity).GetMethod("InitializeNetworkBehaviours", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(identity, null);
        return component;
    }

    public static IDisposable ClientScope() => new ClientState();

    sealed class ClientState : IDisposable
    {
        readonly FieldInfo state = typeof(NetworkClient).GetField("connectState", BindingFlags.Static | BindingFlags.NonPublic);
        readonly object previous;
        public ClientState()
        {
            this.previous = this.state.GetValue(null);
            this.state.SetValue(null, ConnectState.Connected);
        }
        public void Dispose() => this.state.SetValue(null, this.previous);
    }
}

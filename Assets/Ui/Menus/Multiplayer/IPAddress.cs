using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class IPAddress : BaseInput
{
    NetworkManager manager;
    // Start is called before the first frame update
    void Start()
    {
        manager = NetworkManager.singleton;

        this.input.SetTextWithoutNotify(manager.networkAddress);
        this.input.onEndEdit.AddListener(x => { manager.networkAddress = x; });
        this.placeholder.SetText("Enter IP address");
    }

}

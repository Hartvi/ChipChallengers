using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoreInit : NetworkBehaviour
{
    [Server]
    public override void OnStartServer()
    {
        print($"srv: CoreInit");
        this.gameObject.layer = 6;
        this.name = UIStrings.Core;
        this.gameObject.GetComponent<CoreChip>();
    }

}

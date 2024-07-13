using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreInit : MonoBehaviour
{
    void Awake()
    {
        this.gameObject.layer = 6;
        this.name = UIStrings.Core;
        this.gameObject.AddComponent<CoreChip>();
    }
}

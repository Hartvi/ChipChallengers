using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using System.Linq;

public interface ReadonlyProp
{
    public VirtualProp propTree { get
        {
            return this.GetPropTree();
        }
    }
    protected abstract VirtualProp GetPropTree();


}

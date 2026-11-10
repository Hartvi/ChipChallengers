using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseSidePanel : BasePanel, IPointerEnterHandler, IPointerExitHandler
{
    protected bool insideChipPanel = false;

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        this.insideChipPanel = false;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        this.insideChipPanel = true;
    }

}

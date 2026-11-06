using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionChip : GeometricChip
{
    GameObject[] _OptionObjects;
    Transform selectedOptionTransform;

    // SelectedOptionTransform identifies the visible option that visual animations should update.
    public Transform SelectedOptionTransform
    {
        get
        {
            if (this.selectedOptionTransform == null || !this.selectedOptionTransform.gameObject.activeSelf)
            {
                this.selectedOptionTransform = this.OptionObjects.FirstOrDefault(x => x.activeSelf)?.transform;
            }
            return this.selectedOptionTransform;
        }
    }
    protected GameObject[] OptionObjects
    {
        get
        {
            if (this._OptionObjects is null)
            {
                List<GameObject> optObjs = new List<GameObject>();
                Transform[] childTs = this.gameObject.GetComponentsInChildren<Transform>(true);
                this._OptionObjects = childTs.Where(x => x.name.Substring(0, Math.Min("Option".Length, x.name.Length)) == "Option").Select(x=>x.gameObject).ToArray();
            }
            return this._OptionObjects;
        }
    }

    // SelectOption activates the requested appearance and updates the visual animation target.
    protected void SelectOption(int o)
    {
        char oChar = o.ToString()[0];
        if (!(o < OptionObjects.Length && o > -1))
        {
            //Debug.LogWarning($"Option is invalid: `{o}` for chip `{this.equivalentVirtualChip.ChipType}`");
            oChar = '0';
        }
        this.selectedOptionTransform = null;
        for (int i = 0; i < this.OptionObjects.Length; ++i)
        {
            GameObject oo = this.OptionObjects[i];
            oo.SetActive(oChar == oo.name[oo.name.Length - 1]);
            if (oo.activeSelf) { this.selectedOptionTransform = oo.transform; }
        }
    }

}

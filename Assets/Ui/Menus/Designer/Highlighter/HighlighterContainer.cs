using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlighterContainer : MonoBehaviour
{
    private GameObject _ParentHighlighter;
    public GameObject ParentHighlighter { get { return this._ParentHighlighter; } }
    private GameObject HighlightingChip, NorthChip, SouthChip, EastChip, WestChip;
    private Action<VChip>[] HighlightCallbacks = new Action<VChip>[] { };

    //void OnDisable()
    //{
    //    this.ParentHighlighter?.SetActive(false);
    //}

    //void OnEnable()
    //{
    //    this.ParentHighlighter?.SetActive(true);
    //}

    public void InstantiateHighlighters()
    {
        this._ParentHighlighter = new GameObject("Highlighter");
        this.ParentHighlighter.transform.rotation = Quaternion.identity;
        this.ParentHighlighter.transform.position = Vector3.zero;
        this.ParentHighlighter.transform.localScale = Vector3.one;

        this.HighlightingChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.HighlightingChip.layer = 6;
        Renderer renderer = this.HighlightingChip.GetComponent<MeshRenderer>();
        Material m = renderer.material;
        m.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        m.SetTransparent();

        this.HighlightingChip.SetActive(false);

        GameObject.Destroy(this.HighlightingChip.GetComponent<Collider>());

        this.NorthChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.NorthChip.layer = 6;

        ColourHighlighter ch = this.NorthChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.North;

        renderer = this.NorthChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.0f, 0.0f, 0.8f, 0.5f);
        m.SetTransparent();

        this.NorthChip.SetActive(false);

        this.SouthChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.SouthChip.layer = 6;

        ch = this.SouthChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.South;

        renderer = this.SouthChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.8f, 0.0f, 0.0f, 0.5f);
        m.SetTransparent();

        this.SouthChip.SetActive(false);

        this.EastChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.EastChip.layer = 6;

        ch = this.EastChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.East;

        renderer = this.EastChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.0f, 0.8f, 0.0f, 0.5f);
        m.SetTransparent();

        this.EastChip.SetActive(false);

        this.WestChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.WestChip.layer = 6;

        ch = this.WestChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.West;

        renderer = this.WestChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.8f, 0.8f, 0.0f, 0.5f);
        m.SetTransparent();

        this.WestChip.SetActive(false);

        this.HighlightingChip.transform.SetParent(this.ParentHighlighter.transform);
        this.NorthChip.transform.SetParent(this.ParentHighlighter.transform);
        this.WestChip.transform.SetParent(this.ParentHighlighter.transform);
        this.SouthChip.transform.SetParent(this.ParentHighlighter.transform);
        this.EastChip.transform.SetParent(this.ParentHighlighter.transform);
    }

    public void SetHighlightCallbacks(Action<VChip>[] vcs) {
        this.HighlightCallbacks = vcs;
    }

    public CommonChip SelectVChip(string chipId)
    {
        // TODO make the assignment from this function more secure, maybe `out CommonChip selectedChip`? and `bool` return

        CommonChip cc = CommonChip.ClientCore.AllChips.FirstOrDefault(x => x.equivalentVirtualChip.id == chipId) as CommonChip;

        if(cc is null)
        {
            //UnityEngine.Debug.LogWarning($"Clicked on a null object. ID: {chipId}, perhaps fix it to the second to last object");
            string parentId = chipId.Substring(0, chipId.Length - 1);
            cc = CommonChip.ClientCore.AllChips.FirstOrDefault(x => x.equivalentVirtualChip.id == parentId) as CommonChip;
            if (cc is null)
            {
                UnityEngine.Debug.LogError($"Not even the parent of id {chipId}, {parentId} exists");
                cc = CommonChip.ClientCore;
            }
            //throw new NullReferenceException($"Chip with id {chipId} does not exist.");
        }
        //print($"Selected chip type: {cc.equivalentVirtualChip.ChipType}.");
        //print(cc.transform.rotation.eulerAngles);

        Vector3 scalingVector = Vector3.one + Vector3.up * 0.1f;

        var hc = this.HighlightingChip;
        hc.SetActive(true);
        hc.transform.position = cc.transform.position;
        hc.transform.rotation = cc.transform.rotation;
        hc.transform.localScale = cc.transform.localScale.Multiply(scalingVector);

        hc = this.NorthChip;
        hc.SetActive(true);
        hc.transform.position = cc.transform.position + GeometricChip.ChipSide*cc.transform.forward;
        hc.transform.rotation = cc.transform.rotation;
        hc.transform.localScale = cc.transform.localScale.Multiply(scalingVector);

        hc = this.SouthChip;
        hc.SetActive(true);
        hc.transform.position = cc.transform.position - GeometricChip.ChipSide*cc.transform.forward;
        hc.transform.rotation = cc.transform.rotation;
        hc.transform.localScale = cc.transform.localScale.Multiply(scalingVector);

        hc = this.EastChip;
        hc.SetActive(true);
        hc.transform.position = cc.transform.position + GeometricChip.ChipSide*cc.transform.right;
        hc.transform.rotation = cc.transform.rotation;
        hc.transform.localScale = cc.transform.localScale.Multiply(scalingVector);

        hc = this.WestChip;
        hc.SetActive(true);
        hc.transform.position = cc.transform.position - GeometricChip.ChipSide*cc.transform.right;
        hc.transform.rotation = cc.transform.rotation;
        hc.transform.localScale = cc.transform.localScale.Multiply(scalingVector);

        foreach (var c in this.HighlightCallbacks)
        {
            c(cc.equivalentVirtualChip);
        }
        return cc;
    }

    public bool HighlightChip(CommonChip clickedObject, out CommonChip selectedChip)
    {
        //CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();

        if (clickedObject is not null)
        {
            selectedChip = this.SelectVChip(clickedObject.equivalentVirtualChip.id);
            return true;
        }
        else
        {
            this.HighlightingChip.SetActive(false);
            this.NorthChip.SetActive(false);
            this.SouthChip.SetActive(false);
            this.EastChip.SetActive(false);
            this.WestChip.SetActive(false);
        }
        selectedChip = null;
        return false;
    }

}


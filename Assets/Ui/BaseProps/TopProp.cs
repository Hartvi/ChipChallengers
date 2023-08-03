using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopProp : TopUI
{
    private VirtualProp _vProp = new VirtualProp();
    //public VirtualProp vProp { get { return this.GetVProp(); } set { this._vProp = value; } }
    public VirtualProp vProp
    {
        get
        {
            return this._vProp;
        }
        set
        {
            this._vProp = value;
            this._vProp.rProp = this;
        }
    }

    public VirtualProp vParent;
    public TopProp rParent;
    public List<VirtualProp> vChildren = new List<VirtualProp>();
    public List<TopProp> rChildren = new List<TopProp>();

    protected virtual void Setup() {}

    public void Link(VirtualProp vProp)
    {
        // sets all the other contingent variables
        this.vProp = vProp;
        vProp.rProp = this;
        //print($"Link parent: {this.vProp.Parent} of {this.vProp} is null: {this.vProp.Parent == null}");
        var rParent = this.vProp.Parent.rProp;
        this.transform.SetParent(rParent.transform);
        this.Setup();
        this.vParent = rParent.vProp;
        this.vChildren = vProp.Children;
        this.rParent = rParent;
        this.rParent.rChildren.Add(this);
    }

    public void AddChildren()
    {
        // if it's going down => start at maximum to go to zero
        Vector2 stackDir = this.vProp.StackDir;
        Vector2 edge = stackDir * -0.5f;
        // [0, 1] or [1, 0] the object dimension is Size*stackAxis
        Vector2 stackAxis = new Vector2(Mathf.Abs(this.vProp.StackDir.x), Mathf.Abs(this.vProp.StackDir.y));
        // the width dimension is inherited from Parent
        Vector2 fullDimension = Vector2.one - stackAxis;
        if (this.vParent == null)
        {
            this.RT.anchorMin = new Vector2(0.5f, 0.5f);
            this.RT.anchorMax = new Vector2(0.5f, 0.5f);
            this.RT.sizeDelta = new Vector2(Screen.width, Screen.height);
            //print($"this: {this} sizedelta: {this.RT.sizeDelta} anchor: {this.RT.anchorMin} {this.RT.anchorMax}");
        }
        Vector2 sizeDelta = this.RT.sizeDelta;

        //foreach (VirtualProp childProp in this.vProp.Children)
        var _children = this.vProp.Children;
        int _childCount = _children.Count;
        float cumulativeSize = 0f;
        for (int i = 0; i<_childCount; ++i)
        {
            var childProp = _children[i];
            //print("parent prop: "+this.vProp+" rprop is null: "+(this.vProp.rProp==null));
            //print("child prop: "+childProp);
            TopProp newChild = childProp.Spawn();
            // SETUP TRANSFORM
            RectTransform childRT = newChild.RT;
            // fix any top/down/left/right assumptions, since we are using only size and position
            childRT.anchorMin = new Vector2(0.5f, 0.5f);
            childRT.anchorMax = new Vector2(0.5f, 0.5f);

            float childPropSize = childProp.Size;
            if(childProp.Size < 0)
            {
                if(i == _childCount - 1)
                {
                    childPropSize = 1f - cumulativeSize;
                }
                else
                {
                    throw new ArgumentException($"Size can only be -1 in the last element of a VirtualProp list, index: {i}!={_childCount-1}");
                }
                cumulativeSize += childPropSize;
                if(cumulativeSize < 0.999f)
                {
                    throw new Exception($"Cumulative size was calculated wrong: {cumulativeSize}.");
                }
            }
            cumulativeSize += childPropSize;
            print($"prop: {childProp} edge: {edge} stackDir: {stackDir}");
            childRT.anchoredPosition = sizeDelta * (edge + 0.5f * childPropSize * stackDir);
            childRT.rotation = TopProp.Rot2Rot(childProp.Rotation);
            // retain dimension orthogonal to stacking & scale by Size in stacking axis
            childRT.sizeDelta = TopProp.Rotate2D(sizeDelta * (fullDimension + childPropSize * stackAxis), childProp.Rotation);

            edge += childPropSize * stackDir;
            newChild.AddChildren();
            //newChild.gameObject.SetActive(true);
        }
    }

    public void StackFrom<T>(List<T> props) where T : TopProp
    {
        
        Vector2 childPropSize = props[0].RT.sizeDelta;

        // if it's going down => start at maximum to go to zero
        Vector2 stackDir = props[0].rParent.vProp.StackDir;
        Vector2 deltaPosition = stackDir * childPropSize;

        Vector2 currentPosition = props[0].RT.anchoredPosition;

        var _children = props;
        int _childCount = _children.Count;

        for (int i = 0; i < _childCount; ++i)
        {
            var childProp = _children[i];
            // SETUP TRANSFORM
            RectTransform childRT = childProp.RT;
            // fix any top/down/left/right assumptions, since we are using only size and position
            childRT.anchorMin = new Vector2(0.5f, 0.5f);
            childRT.anchorMax = new Vector2(0.5f, 0.5f);

            childRT.anchoredPosition = currentPosition;
            currentPosition += deltaPosition;
        }
    }

    public static Quaternion Rot2Rot(Vector2Int rotation)
    {
        if (rotation.y > 0)
            return Quaternion.Euler(0f, 0f, 0f);
        if (rotation.y < 0)
            return Quaternion.Euler(0f, 0f, 180f);
        if (rotation.x > 0)
            return Quaternion.Euler(0f, 0f, -90f);
        if (rotation.x < 0)
            return Quaternion.Euler(0f, 0f, 90f);
        throw new ArgumentException("Zero vector not a Rotation!");
    }

    public static Vector2 Rotate2D(Vector2 v, Vector2Int rotation)
    {
        if (rotation.y > 0)
            return v;
        if (rotation.y < 0)
            return v;
        if (rotation.x > 0)
            return new Vector2(v[1], v[0]);
        if (rotation.x < 0)
            return new Vector2(v[1], v[0]);
        throw new ArgumentException("Zero vector not a Rotation!");
    }

}

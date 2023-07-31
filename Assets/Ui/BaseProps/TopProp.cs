using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopProp : TopUI
{
    //private VirtualProp _vProp = new VirtualProp();
    //public VirtualProp vProp { get { return this.GetVProp(); } set { this._vProp = value; } }
    public VirtualProp vProp = new VirtualProp();
    //{ 
    //    get
    //    {
    //        return this._vProp;
    //    }
    //    set 
    //    {
    //        this._vProp = value;
    //        value.rProp = this;
    //        var rParent = this._vProp.Parent.rProp;
    //        this.vParent = rParent.vProp;
    //        this.vChildren = this._vProp.Children;
    //        this.rParent = rParent;
    //        this.rParent.rChildren.Add(this);
    //    }
    //}
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
        print($"Link parent: {this.vProp.Parent} of {this.vProp} is null: {this.vProp.Parent == null}");
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
            this.RT.sizeDelta = new Vector2(Screen.width, Screen.height);
        }
        Vector2 sizeDelta = this.RT.sizeDelta;

        foreach (VirtualProp childProp in this.vProp.Children)
        {
            print("parent prop: "+this.vProp+" rprop is null: "+(this.vProp.rProp==null));
            print("child prop: "+childProp);
            TopProp newChild = childProp.Spawn();
            // SETUP TRANSFORM
            RectTransform childRT = newChild.RT;
            childRT.anchoredPosition = sizeDelta * (edge + 0.5f * childProp.Size * stackDir);
            childRT.rotation = TopProp.Rot2Rot(childProp.Rotation);
            // retain dimension orthogonal to stacking & scale by Size in stacking axis
            childRT.sizeDelta = TopProp.Rotate2D(sizeDelta * (fullDimension + childProp.Size * stackAxis), childProp.Rotation);

            edge += childProp.Size * stackDir;
            newChild.AddChildren();
            //newChild.gameObject.SetActive(true);
        }
    }

    public T GetSibling<T>()
    {
        IEnumerable<TopProp> siblings = from x in this.vProp.Parent.Children select x.rProp;
        foreach (var o in siblings)
        {
            if (o is T result) return result;
        }
        return default(T);
    }

    public List<T> GetSiblings<T>()
    {
        List<T> ret = new List<T>();
        IEnumerable<TopProp> siblings = from x in this.vProp.Parent.Children select x.rProp;
        foreach (var o in siblings)
        {
            if (o is T result) ret.Add(result);
        }
        return ret;
    }

    public T GetChild<T>()
    {
        IEnumerable<TopProp> siblings = this.rChildren;
        foreach (var o in siblings)
        {
            if (o is T result) return result;
        }
        return default(T);
    }
    public List<T> GetChildren<T>()
    {
        List<T> ret = new List<T>();
        IEnumerable<TopProp> siblings = this.rChildren;
        foreach (var o in siblings)
        {
            if (o is T result) ret.Add(result);
        }
        return ret;
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

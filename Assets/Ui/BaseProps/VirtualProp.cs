using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public enum PropType
{
    Panel, Image, RawImage, Button, Toggle, Input, Text, Slider, Scrollbar, Dropdown, ScrollView
}


//[Serializable]
public class VirtualProp
{
    public PropType MyType = PropType.Panel;
    public Vector2 PropSize = new Vector2(1f, 1f);
    public Vector2 InheritedPropSize = new Vector2(1f, 1f);
    public float Size = 1f;
    public Vector2 StackDir = Vector2.down;  // use zero for z-stacking
    public Vector2Int Rotation = Vector2Int.up;

    public VirtualProp Parent;
    public List<VirtualProp> Children;
    public TopProp rProp;

    private Type _myBehaviour;

    public Type MyBehaviour
    {
        get
        {
            return _myBehaviour;
        }
        set
        {
            if (typeof(TopProp).IsAssignableFrom(value))
            {
                _myBehaviour = value;
            }
            else
            {
                throw new ArgumentException("Type must be TopProp or a derived class");
            }
        }
    }
    
    public TopProp Spawn()
    {
        GameObject uiGameObject;

        switch (this.MyType)
        {
            case PropType.Panel:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Panel);
                break;
            case PropType.Button:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Button);
                break;
            case PropType.Toggle:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Toggle);
                break;
            case PropType.Image:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Image);
                break;
            case PropType.Slider:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Slider);
                break;
            case PropType.Text:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Text);
                break;
            case PropType.Dropdown:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Dropdown);
                break;
            case PropType.Input:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Input);
                break;
            case PropType.RawImage:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.RawImage);
                break;
            case PropType.ScrollView:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.ScrollView);
                break;
            case PropType.Scrollbar:
                uiGameObject = UnityEngine.Object.Instantiate(UIUtils.Scrollbar);
                break;
            default:
                throw new ArgumentException($"Invalid PropType: {this.MyType}");
        }
        TopProp newChild = uiGameObject.AddComponent(this.MyBehaviour) as TopProp;
        //PRINT.print($"newChild: {newChild} and its vProp: {this}");
        newChild.Link(this);
        return newChild;
    }

    public VirtualProp(params VirtualProp[] childProps)  // special node
    {
        this.Children = new List<VirtualProp>(childProps);
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
    }
    public VirtualProp(PropType propType, params VirtualProp[] childProps)  // special node
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
    }
    public VirtualProp(Type behaviour, params VirtualProp[] childProps)
    {
        this.Children = new List<VirtualProp>(childProps);
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
        this.MyBehaviour = behaviour;
    }
    public VirtualProp(PropType propType, float size, Vector2Int stackDir, Vector2Int rotation, Type behaviour, params VirtualProp[] childProps)
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.StackDir = VirtualProp.SignV2I(stackDir);
        this.Size = size;
        this.Rotation = rotation;
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
        this.MyBehaviour = behaviour;
    }
    public VirtualProp(PropType propType, float size, Vector2Int stackDir, Type behaviour, params VirtualProp[] childProps)
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.StackDir = VirtualProp.SignV2I(stackDir);
        this.Size = size;
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
        this.MyBehaviour = behaviour;
    }
    public VirtualProp(PropType propType, float size, Vector2Int stackDir, Vector2Int rotation, params VirtualProp[] childProps)
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.StackDir = VirtualProp.SignV2I(stackDir);
        this.Size = size;
        this.Rotation = rotation;
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
    }
    public VirtualProp(PropType propType, float size, Vector2Int stackDir, params VirtualProp[] childProps)
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.StackDir = VirtualProp.SignV2I(stackDir);
        this.Size = size;
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
    }
    public VirtualProp(PropType propType, float size, Type behaviour, params VirtualProp[] childProps)
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.Size = size;
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
        this.MyBehaviour = behaviour;
    }
    public VirtualProp(PropType propType, float size, params VirtualProp[] childProps)
    {
        this.MyType = propType;
        this.Children = new List<VirtualProp>(childProps);
        this.Size = size;
        this.SetParent(this.Children);
        this.SetPropBehaviourType(this.MyType);
    }
    void SetParent(List<VirtualProp> children)
    {
        foreach (var child in children)
        {
            //PRINT.print($"setting parent {this} of {child}");
            child.Parent = this;
        }
    }
    void SetPropBehaviourType(PropType propType)
    {
        switch (propType)
        {
            case PropType.Button:
                this.MyBehaviour = typeof(BaseButton);
                break;
            case PropType.Input:
                this.MyBehaviour = typeof(BaseInput);
                break;
            case PropType.Text:
                this.MyBehaviour = typeof(BaseText);
                break;
            case PropType.Slider:
                this.MyBehaviour = typeof(BaseSlider);
                break;
            case PropType.Image:
                this.MyBehaviour = typeof(BaseImage);
                break;
            case PropType.RawImage:
                this.MyBehaviour = typeof(BaseRawImage);
                break;
            case PropType.Panel:
                this.MyBehaviour = typeof(BasePanel);
                break;
            case PropType.Scrollbar:
                this.MyBehaviour = typeof(BaseScrollbar);
                break;
            default:
                throw new ArgumentException($"Invalid propType: {propType}");
        }
    }
    public static Vector2Int SignV2I(Vector2Int v2)
    {
        return new Vector2Int(Math.Sign(v2.x), Math.Sign(v2.y));
    }
    public override string ToString()
    {
        return this.MyType.ToString()+","+this.GetHashCode().ToString();
    }
}


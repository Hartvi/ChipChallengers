using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BaseItemScroll : BasePanel
{
    protected TopProp displayItem;

    protected float realSize;
    protected Vector2 realEdge;

    private float _realItemSize;
    protected float realItemSize
    {
        get { return this._realItemSize; }
        set
        {
            foreach (var item in this.items)
            {
                // keep the size orthogonal to the stackdirection, otherwise set the element size
                item.RT.sizeDelta = new Vector2(Mathf.Abs(StackDirection.y) * item.RT.sizeDelta.x + Mathf.Abs(StackDirection.x) * value, Mathf.Abs(StackDirection.x) * item.RT.sizeDelta.y + Mathf.Abs(StackDirection.y) * value);
            }
            this._realItemSize = value;
        }
    }
    protected Action<string> action;
    public VirtualContainer virtualContainer;

    protected TopProp[] items;

    bool hasSetDisplayItems = false;
    int _NumberOfDisplayItems;
    int _NumberOfSpawnedItems;

    public int NumberOfDisplayItems
    {
        get { return this._NumberOfDisplayItems; }
        set
        {
            if (this.hasSetDisplayItems)
            {
                throw new FieldAccessException($"Cannot set number of items more than once: UI structure is defined at compile time.");
            }

            if(this.displayItem == null)
            {
                throw new FieldAccessException($"Must set display element before initializing list.");
            }

            this._NumberOfSpawnedItems = value + 2;
            this.items = new TopProp[_NumberOfSpawnedItems];
            this.items[0] = this.displayItem;
            for(int i = 1; i < _NumberOfSpawnedItems; ++i)
            {
                TopProp newItem = Instantiate(this.displayItem);
                newItem.transform.SetParent(this.displayItem.transform.parent);
                newItem.transform.position = this.displayItem.transform.position;
                newItem.transform.rotation = this.displayItem.transform.rotation;
                this.items[i] = newItem;
            }
            this._NumberOfDisplayItems = value;
        }
    }

    protected VirtualItem[] virtualItems;

    public void SetupItemList(Action<string> action, int numberOfDisplayItems, string[] labels)
    {
        //print(this.transform.childCount);
        TopProp child = this.GetComponentsInChildren<TopProp>()[1];
        //UnityEngine.Debug.Log($"{this}: Just set {child} as displayItem.");

        this.displayItem = child;
        this.action = action;
        this.virtualContainer = new VirtualContainer(numberOfDisplayItems, labels);
        this.NumberOfDisplayItems = numberOfDisplayItems;

        // rotation points up if it's normal and down if it's upside down
        // for stack direction up or down then it follows that the abs(dot product) of these two shall be greater than 0.5
        //print($"size: {this.RT.sizeDelta} stack direction: {StackDirection} number of display items: {this.NumberOfDisplayItems}");
        this.realSize = Mathf.Max(Mathf.Abs(this.RT.sizeDelta.x * StackDirection.x), Mathf.Abs(this.RT.sizeDelta.y * StackDirection.y));
        this.realItemSize = this.realSize / NumberOfDisplayItems;
        this.realEdge = -0.5f * this.realSize * StackDirection;
        //print($"realsize: {this.realSize}, real edge: {this.realEdge}, real item size: {this.realItemSize}");

        for(int i = 0; i < this.items.Length; ++i)
        {
            //print($"Item: {i}: {this.items[i]}");
            if(this.items[i] == null)
            {
                throw new NullReferenceException($"TopProp items[{i}] out of {this._NumberOfSpawnedItems} is null.");
            }
        }
        Scroll(0f);
        //var positions = realPositions.Zip(this.items, (x, y) => y.transform.position = x);
        //print($"positions: {positions.Count()}");
        //PRINT.print(positions);
    }

    protected Vector2 RealPosition(float relativePosition)
    {
        // real position (wrt this panel): (panel position - 0.5*panelsize) + realsize * relative position
        //print($"relative position: {relativePosition} edge: {this.realEdge} shift: {relativePosition * StackDirection * this.realSize}");
        return this.realEdge + relativePosition * StackDirection * this.realSize;
    }

    protected override void Setup()
    {
        base.Setup();
        //this._DisplayItem
    }
    
    public void Scroll(float x)
    {
        //print($"SCROLLING TO {x}");
        // contains names and relative positions
        VirtualItem[] visibleItems = this.virtualContainer.MoveItems(x);
        //print($"visible items: {visibleItems.Length}");
        
        // real position: real position * realSize
        // real position (wrt this panel): (panel position - 0.5*panelsize*stackDirection) + realsize * relative position
        Vector2[] realPositions = visibleItems.Select(x => this.RealPosition(x.relativePosition)).ToArray();
        //print($"realpositions: {realPositions.Length}");
        if(realPositions.Length > this.items.Length)
        {
            throw new ArgumentException($"Number of positions: {realPositions.Length} does not match number of UI elements: {this.items.Length}.");
        }
        for(int i = 0; i < this.items.Length; ++i) {
            if (i >= realPositions.Length)
            {
                this.items[i].gameObject.SetActive(false);
                continue;
            }
            // set label of visible item
            var btn = (this.items[i] as BaseButton);
            btn.text.text = visibleItems[i].label;
            btn.btn.onClick.RemoveAllListeners();
            btn.btn.onClick.AddListener(() => action(btn.text.text));

            this.items[i].gameObject.SetActive(true);
            //print($"i: {i} pos: {realPositions[i]}");
            this.items[i].RT.anchoredPosition = realPositions[i];
        }
    }
}

public class VirtualItem
{
    public float defaultPosition;
    public float relativePosition;
    public string label;
    

    public VirtualItem(string label, float defaultPosition)
    {
        this.label = label;
        if(defaultPosition < 0f)
        {
            throw new ArgumentOutOfRangeException($"Default position of item must be non-negative: {defaultPosition}.");
        }
        //UnityEngine.Debug.Log($"default position: {defaultPosition}");
        this.defaultPosition = defaultPosition;
    }

}
public class VirtualContainer
{
    float _Position;
    float Position
    {
        get
        {
            return this._Position;
        }
        set
        {
            this._Position = value;
        }
    }
    bool useDiscreteSteps = false;

    int NumberToDisplay, NumberOfSpawned;
    VirtualItem[] virtualItems;
    int NumberOfItems { get { return this.virtualItems.Length; } }
    float MaxMoveDelta;

    float DefaultInterval, DefaultOffset;

    public VirtualContainer(int numberToDisplay, string[] labels)
    {
        // display size is normalized to one
        this.NumberToDisplay = numberToDisplay;
        //UnityEngine.Debug.Log($"number to display: {numberToDisplay}");

        this.NumberOfSpawned = numberToDisplay + 2;

        // this way the sum of item sizes sums up to one
        float defaultInterval = 1f / numberToDisplay;
        this.DefaultInterval = defaultInterval;

        float defaultOffset = 0.5f * defaultInterval;
        this.DefaultOffset = defaultOffset;
        /* 2 items:
         ___
         ___ -- 0.5 * defaultInterval
         ___ -- 1.5 * defaultInterval
        */
        this.UpdateLabels(labels);
    }

    public void UpdateLabels(string[] labels)
    {
        VirtualItem[] virtualItems = labels.Select((x, i) => new VirtualItem(x, this.DefaultOffset + i * this.DefaultInterval)).ToArray();
        
        this.virtualItems = virtualItems;

        // labels.Length = T = M+N elements. M displayed. N not displayed.
        // Maximum position shift is the last not-displayed element to the last displayed position.
        // Total*interval - displayed*interval
        this.MaxMoveDelta = (this.NumberOfItems - this.NumberToDisplay) * this.DefaultInterval;
    }

    public VirtualItem[] MoveItems(float x)
    {
        if(x < 0f || x > 1f)
        {
            throw new ArgumentOutOfRangeException($"Can only scroll in interval [0, 1], currently attempting {x}.");
        }

        for(int i = 0; i < this.virtualItems.Length; ++i)
        {
            var vItem = this.virtualItems[i];
            vItem.relativePosition = vItem.defaultPosition - this.MaxMoveDelta * x;
            // offset = 0.5*item height
            // top item: offset, bottom item: number of display items - offset
            // visible items: +-1 the edges
            //bool itemViyysible = vItem.relativePosition < -this.DefaultOffset || vItem.relativePosition > 1f - this.DefaultOffset;
            
        }
        float maxOffset = 1f + this.DefaultOffset;
        return this.virtualItems.Where(vItem => vItem.relativePosition >= -this.DefaultOffset && vItem.relativePosition <= maxOffset).ToArray();
    }
}


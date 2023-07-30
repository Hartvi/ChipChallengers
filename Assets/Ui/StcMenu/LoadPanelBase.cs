using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadPanelBase : BaseMenu
{
    string[] items;
    Type itemSelector = typeof(MenuItemButton);
    int displayNum = 10;
    protected override void Setup()
    {
        VirtualProp[] itemSelects = new VirtualProp[displayNum+2];
        float itemHeight = 1f / displayNum;
        for(int i = 0; i < displayNum+2; ++i)
        {
            itemSelects[i] = new VirtualProp(PropType.Button, itemHeight, itemSelector);
        }
        vProp = new VirtualProp(
            new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
                new VirtualProp(PropType.Image, 0.1f, Vector2Int.right,
                    new VirtualProp(PropType.Text, 1f)
                ),
                new VirtualProp(PropType.Panel, 0.8f, Vector2Int.right,
                    new VirtualProp(PropType.Image, 0.98f, Vector2Int.down,
                        itemSelects  // model name etc, there's gonna be many buttons
                    ),
                    new VirtualProp(PropType.Scrollbar, 0.02f, Vector2Int.right, Vector2Int.right)  // scrollbar
                ),
                new VirtualProp(PropType.Image, 0.1f, Vector2Int.right,
                    //new VirtualProp(PropType.Panel, 0.1f),
                    new VirtualProp(PropType.Input, 0.6f),
                    new VirtualProp(PropType.Button, 0.2f),  // ok
                    new VirtualProp(PropType.Button, 0.2f)  // cancel
                )
            )
        );
    }

    protected override void Start()
    {
        base.Start();
    }

    public virtual void LoadItems()
    {
        items = "a b c d e f g h i j k l m n o p q r s t u v w x y z".Split(' ');
    }
    class LoadSlider : BaseSlider
    {
        protected override void Execute(float x)
        {
            
        }
    }
    protected class MenuItemButton : TopProp
    {
        public static Vector2 boundaries;
        protected override void Setup()
        {
            // yo soy lazy
            float myY = gameObject.RT().anchoredPosition.y;
            if (myY < boundaries[0])
                boundaries[0] = myY;
            else if (myY > boundaries[1])
                boundaries[1] = myY;
        }
    }
    class VirtualItem
    {
        public float defaultPosition;
        public string label;
        public bool visible;
        
        public VirtualItem(string label)
        {
            this.label = label;
        }
    }
    class VirtualContainer
    {
        float size;
        float itemSize;
        VirtualItem[] items;
        float invisibilityLower, invisibilityHigher;
        public VirtualContainer(float size, float itemSize, params VirtualItem[] items)
        {
            this.size = size;
            this.itemSize = itemSize;
            this.items = items;
            for(int i = 0; i < items.Length; ++i)
            {
                items[i].defaultPosition = 0.5f * itemSize + i * itemSize;
            }
            invisibilityLower = -0.5f * itemSize;
            invisibilityHigher = items.Length * itemSize - 0.5f * itemSize;
        }
        //public VirtualItem[] Move(float pos)
        //{
        //    float moveFactor = Mathf.Max(0, Size - items.Length*itemSize);
        //}
    }
}

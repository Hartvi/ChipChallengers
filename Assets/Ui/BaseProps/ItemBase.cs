using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemBase : TopProp
{
    public int MyIndex = 0;

    public ItemBase Next(bool takeInactive = false)
    {
        List<ItemBase> siblings = new List<ItemBase>();
        bool hasBeenInactive = false;
        int numberOfChildren = 0;
        for (int i = 0; i < this.transform.childCount; ++i)
        {
            var newSibling = this.transform.parent.GetChild(i).GetComponent<ItemBase>();
            if (newSibling == null) continue;

            ++numberOfChildren;
            if (newSibling.isActiveAndEnabled)
            {
                if(hasBeenInactive)
                {
                    throw new ArgumentNullException($"Items are not continuously inactive. Active: {i} after inactive {i-1}.");
                }
                siblings.Add(newSibling);
            }
            else
            {
                hasBeenInactive = true;
            }
        }
        int CurrentIndex = (MyIndex + 1) % (takeInactive ? numberOfChildren : siblings.Count);

        // pass information to the next guy that he is selected
        var nextGuy = siblings[CurrentIndex];
        nextGuy.MyIndex = CurrentIndex;

        return nextGuy;
    }
    public ItemBase AddItem()
    {
        var siblings = this.Siblings<ItemBase>(takeInactive: true);
        for(int i = 0; i < siblings.Length; ++i)
        {
            if(!siblings[i].gameObject.activeSelf)
            {
                siblings[i].gameObject.SetActive(true);
                return siblings[i];
            }
        }
        var newSibling = Instantiate<ItemBase>(this);
        newSibling.transform.SetParent(this.transform.parent);
        return newSibling;
    }
    public void HideItem()
    {
        var siblings = this.Siblings<ItemBase>(takeInactive: false);
        siblings[siblings.Length -1].gameObject.SetActive(false);
    }
    public ItemBase[] DisplayNItems(int n)
    {
        var siblings = this.Siblings<ItemBase>(takeInactive: true);
        if (n > siblings.Length)
        {
            for(int i = 0; i < n - siblings.Length; ++i)
            {
                AddItem();
            }
            siblings = this.Siblings<ItemBase>(takeInactive: true);
        }
        for(int i = 0; i < siblings.Length; ++i)
        {
            siblings[i].gameObject.SetActive(i < n);
        }
        return this.Siblings<ItemBase>(takeInactive: false);
    }
}

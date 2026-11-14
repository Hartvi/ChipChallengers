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
        var parent = this.transform.parent;
        if (parent == null) return takeInactive || this.isActiveAndEnabled ? this : null;
        var siblings = this.Siblings<ItemBase>(takeInactive)
            .Where(item => takeInactive || item.isActiveAndEnabled).ToArray();
        if (siblings.Length == 0) return null;
        int current = Array.IndexOf(siblings, this);
        int next = (current + 1) % siblings.Length;
        if (current < 0)
        {
            // A hidden item can still advance to the next visible sibling.
            int siblingIndex = this.transform.GetSiblingIndex();
            for (int i = 0; i < siblings.Length; ++i)
            {
                if (siblings[i].transform.GetSiblingIndex() > siblingIndex)
                {
                    next = i;
                    break;
                }
            }
        }
        siblings[next].MyIndex = next;
        return siblings[next];
    }
    public ItemBase AddItem()
    {
        var siblings = this.Siblings<ItemBase>(takeInactive: true);
        for (int i = 0; i < siblings.Length; ++i)
        {
            if (!siblings[i].gameObject.activeSelf)
            {
                siblings[i].gameObject.SetActive(true);
                return siblings[i];
            }
        }
        var newSibling = GameObject.Instantiate<ItemBase>(this, this.transform.parent, false);
        return newSibling;
    }
    public void HideItem()
    {
        var siblings = this.Siblings<ItemBase>(takeInactive: false);
        if (siblings.Length > 0) siblings[siblings.Length - 1].gameObject.SetActive(false);
    }
    public T[] DisplayNItems<T>(int n) where T : ItemBase
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        var siblings = this.Siblings<ItemBase>(takeInactive: true);
        if (n > siblings.Length)
        {
            for (int i = 0; i < n - siblings.Length; ++i)
            {
                GameObject.Instantiate<ItemBase>(this, this.transform.parent, false);
            }
            siblings = this.Siblings<ItemBase>(takeInactive: true);
        }
        for (int i = 0; i < siblings.Length; ++i)
        {
            siblings[i].gameObject.SetActive(i < n);
        }
        return this.Siblings<T>(takeInactive: false);
    }
}

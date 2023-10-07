using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragButton : BaseButton, IPointerDownHandler, IPointerUpHandler
{
    Action[] btnDownActions = { };
    Action[] btnUpActions = { };
    Action[] btnHoldActions = { };

    bool btnDown = false;

    public void AddBtnDownActions(Action[] actions)
    {
        this.btnDownActions = actions;
    }

    public void AddBtnUpActions(Action[] actions)
    {
        this.btnUpActions = actions;
    }
    
    public void AddBtnHoldActions(Action[] actions)
    {
        this.btnHoldActions= actions;
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        foreach(Action a in this.btnDownActions)
        {
            a();
        }
        this.btnDown = true;
    }


    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        foreach(Action a in this.btnUpActions)
        {
            a();
        }
        this.btnDown = false;
    }

    void Update()
    {
        if (this.btnDown)
        {
            foreach (Action a in this.btnHoldActions)
            {
                a();
            }
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistoryStack
{
    Stack<string> pastEdits = new Stack<string>();
    Stack<string> futureEdits = new Stack<string>();

    public void SaveState(string state)
    {
        //PRINT.IPrint($"Pushing new state");
        if (this.pastEdits.Count > 0 && this.pastEdits.Peek() != state)
        {
            this.pastEdits.Push(state);
        }
        else if (this.pastEdits.Count == 0)
        {
            this.pastEdits.Push(state);
        }
    }

    public string Undo()
    {
        if (this.pastEdits.Count == 1)
            return this.pastEdits.Peek();

        if (this.pastEdits.Count == 0)
            return CoreChip.ClientCoreChip.VirtualModel.ToLuaString();

        this.futureEdits.Push(this.pastEdits.Pop());
        return this.pastEdits.Peek();
    }

    public string Redo()
    {
        if (this.futureEdits.Count == 0)
        {
            if (this.pastEdits.Count == 0)
            {
                return CoreChip.ClientCoreChip.VirtualModel.ToLuaString();
            }
            return this.pastEdits.Peek();
        }
        var state = this.futureEdits.Pop();
        this.pastEdits.Push(state);
        return state;
    }
}


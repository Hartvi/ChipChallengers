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
        this.pastEdits.Push(state);
        this.futureEdits.Clear();  // clear future edits once a new state is saved
    }

    public string Undo()
    {
        if (this.pastEdits.Count == 1)
            return this.pastEdits.Peek();
        //throw new InvalidOperationException("No more states to undo.");

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
        //throw new InvalidOperationException("No more states to redo.");

        var state = this.futureEdits.Pop();
        this.pastEdits.Push(state);
        return state;
    }
}


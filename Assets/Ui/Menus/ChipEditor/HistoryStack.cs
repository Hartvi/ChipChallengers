using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HistoryStack
{
    Stack<string> pastEdits = new Stack<string>();
    Stack<string> futureEdits = new Stack<string>();

    public void SaveState(string state)
    {
        //PRINT.IPrint($"Pushing new state");
        if (this.pastEdits.Count > 0 && this.pastEdits.Peek() != state)
        {
            this.futureEdits.Clear();
            this.pastEdits.Push(state);
        }
        else if (this.pastEdits.Count == 0)
        {
            this.pastEdits.Push(state);
        }
        // Retain useful undo history without retaining unbounded full-model snapshots.
        var retained = this.pastEdits.ToArray();
        int count = Math.Min(128, retained.Length);
        long bytes = retained.Take(count).Sum(value => (long)value.Length * 2);
        while (count > 1 && bytes > 8 * 1024 * 1024) { bytes -= retained[--count].Length * 2L; }
        if (count != retained.Length) { this.pastEdits = new Stack<string>(retained.Take(count).Reverse()); }
    }

    public string Undo()
    {
        if (this.pastEdits.Count == 1)
            return this.pastEdits.Peek();

        if (this.pastEdits.Count == 0)
            return null;

        this.futureEdits.Push(this.pastEdits.Pop());
        return this.pastEdits.Peek();
    }

    public string Redo()
    {
        if (this.futureEdits.Count == 0)
        {
            if (this.pastEdits.Count == 0)
            {
                return null;
            }
            return this.pastEdits.Peek();
        }
        var state = this.futureEdits.Pop();
        this.pastEdits.Push(state);
        return state;
    }
}


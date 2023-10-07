using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HistoryStack
{
    static Stack<string> pastEdits = new Stack<string>();
    static Stack<string> futureEdits = new Stack<string>();

    public static void SaveState(string state)
    {
        PRINT.IPrint($"Pushing new state");
        pastEdits.Push(state);
        futureEdits.Clear();  // clear future edits once a new state is saved
    }

    public static string Undo()
    {
        if (pastEdits.Count <= 1)
            throw new InvalidOperationException("No more states to undo.");

        futureEdits.Push(pastEdits.Pop());
        return pastEdits.Peek();
    }

    public static string Redo()
    {
        if (futureEdits.Count == 0)
            throw new InvalidOperationException("No more states to redo.");

        var state = futureEdits.Pop();
        pastEdits.Push(state);
        return state;
    }
}


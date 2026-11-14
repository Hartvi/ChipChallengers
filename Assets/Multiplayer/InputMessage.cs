using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class InputMessage
{
    public char[] KeysDown;
    public char[] KeysUp;
    public char[] Keys;

    public float[] MousePos;
    public bool[] MouseDown;
    public bool[] MouseUp;
    public bool[] MouseClicked;

    public InputMessage()
    {
        this.KeysDown = new char[0];
        this.KeysUp = new char[0];
        this.Keys = new char[0];
        this.MousePos = new float[2] { 0.5f, 0.5f };
        this.MouseDown = new bool[3] { false, false, false };
        this.MouseUp = new bool[3] { false, false, false };
        this.MouseClicked = new bool[3] { false, false, false };
    }

    public void Accumulate(char[] keys, char[] down, char[] up, bool[] clicked, bool[] mouseDown, bool[] mouseUp, float[] mouse)
    {
        this.Keys = keys;
        this.KeysDown = this.KeysDown.Union(down).ToArray();
        this.KeysUp = this.KeysUp.Union(up).ToArray();
        this.MouseClicked = clicked;
        for (int i = 0; i < 3; i++)
        {
            this.MouseDown[i] |= mouseDown[i];
            this.MouseUp[i] |= mouseUp[i];
        }
        this.MousePos = mouse;
    }

    public void ConsumeEdges()
    {
        this.KeysDown = Array.Empty<char>();
        this.KeysUp = Array.Empty<char>();
        Array.Clear(this.MouseDown, 0, this.MouseDown.Length);
        Array.Clear(this.MouseUp, 0, this.MouseUp.Length);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}

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
        this.MousePos = new float[2];
        this.MouseDown = new bool[3] { false, false, false };
        this.MouseUp = new bool[3] { false, false, false };
        this.MouseClicked = new bool[3] { false, false, false };
    }

    public void Accumulate(char[] keys, char[] down, char[] up, bool[] clicked, bool[] mouseDown, bool[] mouseUp, float[] mouse)
    {
        if (keys == null || down == null || up == null || clicked == null || mouseDown == null || mouseUp == null || mouse == null ||
            keys.Length > ControlKeys.Supported.Length || down.Length > ControlKeys.Supported.Length || up.Length > ControlKeys.Supported.Length ||
            clicked.Length != 3 || mouseDown.Length != 3 || mouseUp.Length != 3 || mouse.Length != 2)
        {
            this.Keys = Array.Empty<char>();
            this.ConsumeEdges();
            Array.Clear(this.MouseClicked, 0, this.MouseClicked.Length);
            return;
        }
        this.Keys = keys.Where(ControlKeys.IsSupported).Select(ControlKeys.Normalize).Distinct().ToArray();
        this.KeysDown = this.KeysDown.Union(down.Where(ControlKeys.IsSupported).Select(ControlKeys.Normalize)).ToArray();
        this.KeysUp = this.KeysUp.Union(up.Where(ControlKeys.IsSupported).Select(ControlKeys.Normalize)).ToArray();
        this.MouseClicked = clicked;
        for (int i = 0; i < 3; i++)
        {
            this.MouseDown[i] |= mouseDown[i];
            this.MouseUp[i] |= mouseUp[i];
        }
        this.MousePos = new[] { InputCoordinates.Clamp(mouse[0]), InputCoordinates.Clamp(mouse[1]) };
    }

    public bool Held(char key) => this.Keys.Contains(ControlKeys.Normalize(key));
    public bool Down(char key) => this.KeysDown.Contains(ControlKeys.Normalize(key));
    public bool Up(char key) => this.KeysUp.Contains(ControlKeys.Normalize(key));

    public void ConsumeEdges()
    {
        this.KeysDown = Array.Empty<char>();
        this.KeysUp = Array.Empty<char>();
        Array.Clear(this.MouseDown, 0, this.MouseDown.Length);
        Array.Clear(this.MouseUp, 0, this.MouseUp.Length);
    }
}

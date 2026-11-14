using System.Collections.Generic;
using UnityEngine;

class InputHelper
{
    public static IEnumerable<char> SupportedKeys => ControlKeys.Supported;
    public static bool Held(char key) => Read(key, Input.GetKey);
    public static bool Down(char key) => Read(key, Input.GetKeyDown);
    public static bool Up(char key) => Read(key, Input.GetKeyUp);

    static bool Read(char key, System.Func<KeyCode, bool> read)
    {
        key = ControlKeys.Normalize(key);
        if (!chartoKeycode.TryGetValue(key, out var code)) { return false; }
        if (read(code)) { return true; }
        if (key >= '0' && key <= '9') { return read((KeyCode)((int)KeyCode.Keypad0 + key - '0')); }
        return key == '\n' && read(KeyCode.KeypadEnter);
    }
    public static Dictionary<char, KeyCode> chartoKeycode = new Dictionary<char, KeyCode>()
    {
        //-------------------------LOGICAL mappings-------------------------
        
        //Lower Case Letters
        {'a', KeyCode.A},
        {'b', KeyCode.B},
        {'c', KeyCode.C},
        {'d', KeyCode.D},
        {'e', KeyCode.E},
        {'f', KeyCode.F},
        {'g', KeyCode.G},
        {'h', KeyCode.H},
        {'i', KeyCode.I},
        {'j', KeyCode.J},
        {'k', KeyCode.K},
        {'l', KeyCode.L},
        {'m', KeyCode.M},
        {'n', KeyCode.N},
        {'o', KeyCode.O},
        {'p', KeyCode.P},
        {'q', KeyCode.Q},
        {'r', KeyCode.R},
        {'s', KeyCode.S},
        {'t', KeyCode.T},
        {'u', KeyCode.U},
        {'v', KeyCode.V},
        {'w', KeyCode.W},
        {'x', KeyCode.X},
        {'y', KeyCode.Y},
        {'z', KeyCode.Z},
        
        //KeyPad Numbers
        {'1', KeyCode.Alpha1},
        {'2', KeyCode.Alpha2},
        {'3', KeyCode.Alpha3},
        {'4', KeyCode.Alpha4},
        {'5', KeyCode.Alpha5},
        {'6', KeyCode.Alpha6},
        {'7', KeyCode.Alpha7},
        {'8', KeyCode.Alpha8},
        {'9', KeyCode.Alpha9},
        {'0', KeyCode.Alpha0},
        {' ', KeyCode.Space},
        {'\t', KeyCode.Tab},
        {'\b', KeyCode.Backspace},
        {'\n', KeyCode.Return},
        {'\u001b', KeyCode.Escape},
        
        //Other Symbols
        {'!', KeyCode.Exclaim}, //1
        {'"', KeyCode.DoubleQuote},
        {'#', KeyCode.Hash}, //3
        {'$', KeyCode.Dollar}, //4
        {'&', KeyCode.Ampersand}, //7
        {'\'', KeyCode.Quote}, //remember the special forward slash rule... this isnt wrong
        {'(', KeyCode.LeftParen}, //9
        {')', KeyCode.RightParen}, //0
        {'*', KeyCode.Asterisk}, //8
        {'+', KeyCode.Plus},
        {',', KeyCode.Comma},
        {'-', KeyCode.Minus},
        {'.', KeyCode.Period},
        {'/', KeyCode.Slash},
        {':', KeyCode.Colon},
        {';', KeyCode.Semicolon},
        {'<', KeyCode.Less},
        {'=', KeyCode.Equals},
        {'>', KeyCode.Greater},
        {'?', KeyCode.Question},
        {'@', KeyCode.At}, //2
        {'[', KeyCode.LeftBracket},
        {'\\', KeyCode.Backslash}, //remember the special forward slash rule... this isnt wrong
        {']', KeyCode.RightBracket},
        {'^', KeyCode.Caret}, //6
        {'_', KeyCode.Underscore},
        {'`', KeyCode.BackQuote},
    };
}


using System.Collections.Generic;
using UnityEngine;

class InputHelper
{
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
        {'1', KeyCode.Keypad1},
        {'2', KeyCode.Keypad2},
        {'3', KeyCode.Keypad3},
        {'4', KeyCode.Keypad4},
        {'5', KeyCode.Keypad5},
        {'6', KeyCode.Keypad6},
        {'7', KeyCode.Keypad7},
        {'8', KeyCode.Keypad8},
        {'9', KeyCode.Keypad9},
        {'0', KeyCode.Keypad0},
        
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


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StringHelpers {
    public static bool IsFirstLetterAlphabeticOrUnderscore(this string input) {
        if (string.IsNullOrEmpty(input)) {
            throw new ArgumentException("Input string cannot be null or empty.");
        }

        char firstChar = input[0];

        return char.IsLetter(firstChar) || firstChar == '_';
    }

    public static bool IsAlphanumericOrContainsUnderscore(this string input) {
        if (string.IsNullOrEmpty(input)) {
            throw new ArgumentException("Input string cannot be null or empty.");
        }

        foreach (char c in input) {
            if (!char.IsLetterOrDigit(c) && c != '_') {
                return false;
            }
        }

        return true;
    }
    
    public static bool IsVariableName(this string input)
    {
        if (string.IsNullOrEmpty(input)) { return false; }
        char firstChar = input[0];
        return IsAlphanumericOrContainsUnderscore(input) && (char.IsLetter(firstChar) || firstChar == '_');
    }

    public static bool IsColourString(this string input)
    {
        return ColorUtility.TryParseHtmlString(input, out Color _) || uint.TryParse(input, out uint _);
    }

    public static bool IsFloat(this string input)
    {
        return float.TryParse(input, out float _);
    }

    public static bool IsUInt(this string input)
    {
        return uint.TryParse(input, out uint _);
    }
}


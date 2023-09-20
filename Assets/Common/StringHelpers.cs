using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StringHelpers
{
    public static string GetLargest(string[] input)
    {
        return input.OrderBy(s => s.Last()).Last();
    }

    public static int GetIndexOfLargest(string[] input)
    {
        var sorted = input
            .Select((value, index) => new { value, index })
            .OrderBy(item => item.value.Last())
            .Last();

        return sorted.index;
    }

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
        return ColorUtility.TryParseHtmlString(input, out Color _) || int.TryParse(input, out int _);
    }
    public static bool ParseColorOrInt(string input, out Color color)
    {
        if (ColorUtility.TryParseHtmlString(input, out color)) {
            return true;
        } else if (int.TryParse(input, out int i)) {
            color = i.ToColor();
            return true;
        }
        return false;
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


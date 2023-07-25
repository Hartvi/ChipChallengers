using System;
using System.Collections;
using System.Collections.Generic;

public class StringHelpers {
    public static bool IsFirstLetterAlphabeticOrUnderscore(string input) {
        if (string.IsNullOrEmpty(input)) {
            throw new ArgumentException("Input string cannot be null or empty.");
        }

        char firstChar = input[0];

        return char.IsLetter(firstChar) || firstChar == '_';
    }

    public static bool IsAlphanumericOrContainsUnderscore(string input) {
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
    public static bool IsVariableName(string input)
    {
        char firstChar = input[0];
        return IsAlphanumericOrContainsUnderscore(input) && (char.IsLetter(firstChar) || firstChar == '_');
    }
}


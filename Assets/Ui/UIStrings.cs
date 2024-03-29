using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIStrings
{
    public const string Dust = "Dust";

    public const string ReadDistance = "ReadDistance";
    public const string ReadAltitude = "ReadAltitude";
    public const string ReadAngularVelocity = "ReadAngularVelocity";
    public const string ReadRotation = "ReadRotation";
    public const string ReadAcceleration = "ReadAcceleration";
    public const string ReadVelocity = "ReadVelocity";
    public const string Read = "Read";
    public static readonly string[] SensorNames = new string[] { 
        ReadDistance, ReadAltitude, ReadAngularVelocity, 
        ReadRotation, ReadAcceleration, ReadVelocity 
    };

    public const string MiniControls = "   Mini controls:   F1 - show controls   Ctrl+U - go to start   Ctrl+R - reset model   Esc - go to main menu";

    public static readonly string[] ControlsKeys = {
        "F1", "1-9", "up/down/+/-/\"/\"/*", "Ctrl+O", "Ctrl+M", "Ctrl+U", "Ctrl+R", "Ctrl+S", "Ctrl+P"
    };
    public static readonly string[] ControlsActions = new string[] {
        "Display this panel", "Switch cameras", "Adjust camera", "Open model", "Open map", "Go to start", "Reset model", "Save model (in editor)", "Go to playmode (from editor)"
    };

    public const string ModelExtension = ".txt";
    public const string MapExtension = ".obj";

    public const string Core = "Core";

    public const string GameTitle = "Chip Challengers";
    public const string MainMenu = "Main menu";
    public const string Editor = "Editor";
    public const string Singleplayer = "Singleplayer";
    public const string Multiplayer = "Multiplayer";
    public const string Settings = "Settings";

    public const string Framerate = "Frame rate";
    public const string PhysicsRate = "Physics rate";
    public const string Volume = "Volume";
    public const string PhysicsParticles = "Physics particles";
    public const string EnterANumber = "Enter a number";

    public static readonly string[] SettingsIntProperties = new string[] { Framerate, PhysicsRate, Volume, PhysicsParticles };
    public static readonly string[] SettingsAllProperties = new string[] { }.Concat(SettingsIntProperties).ToArray();

    public const string Controls = "Controls";
    public const string Back = "Back";

    public const string IntroValues = "Show quick help\nShow menu";
    public const string IntroKeys = "F1\nESC";

    public const string Name = "Name";
    public const string Default = "Default";
    public const string Minimum = "Minimum";
    public const string Maximum = "Maximum";
    public const string Backstep = "Backstep";
    public static readonly string[] VariableArray = { Name, Default, Minimum, Maximum, Backstep };

    public static readonly string[] DefaultVariableValues = { "EmptyVariable", "0", "0", "0", "0" };

    public const string Add = "Add";
    public const string Delete = "Delete";
    public const string Paste = "Paste";
    public static readonly string[] AddDelete = { Add, Delete };

    public static readonly string[] EditorPanels = { "Chip", "Variables", "Script" };

    public const string Velocity = "Velocity";
    public const string Position = "Position";

    public const string GunRelated = "GunRelated";
    public const string Bullet = "Bullet";

    // MAP RELATED
    public const string EnterMapName = "Enter map name";
    public const string EnterModelName = "Enter model name";

    public static string NotAVariableMsg(string value)
    {
        return $"{value} is not in variable name format:\n '_' or [a-zA-Z] at the beginning.";
    }

    public static string NotAFloat(string value)
    {
        return $"'{value}' is not in float number format:\n a decimal number, e.g. '1.2'.";
    }

    public static string NotAColour(string value)
    {
        return $"'{value}' is not in colour format:\n#RRGGBB in hexadecimal\ne.g. '#FF55BB' or an integer.";
    }

    public static string NotAType(string value)
    {
        return $"'{value}' is not one of the permitted chip types.";
    }

    public static string NotAUInt(string value)
    {
        return $"'{value}' is not a non-negative integer.";
    }

    public static string OptionTooHigh(string value)
    {
        return $"Option '{value}' is too high of a value.";
    }

    public static string ModelExists(string modelName)
    {
        return $"Model '{modelName}' already exists.";
    }

    public static string ModelExistsNot(string modelName)
    {
        return $"Model '{modelName}' doesn't exist.";
    }

}

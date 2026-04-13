using UnityEngine;
using System;

public static class ConsoleConstants
{
    // Constant strings, don't modify.
#if UNITY_EDITOR
    public const string Editorwarning = "<color=red>Developer Console Editor Warning: </color>";
    public const string Unlimited = "Unlimited";
#endif
    public const string Helptext = "Type 'help' and press Enter to print all available commands.";
    public const string Registeredstatic = " (static commands only).";
    public const string Commandmessage = "All available commands:";
    public const string Consoleinit = "Console Initialized. ";
    public const string ColorRedStart = "<color=red>";
    public const string Datetimeformat = "HH:mm:ss";
    public const string Ienumerator = "IEnumerator";
    public const string ColorEnd = "</color>";
    public const string Openparenthesis = "(";
    public const string Closedbracket = "] ";
    public const string Openbracket = "[";
    public const char Emptychar = ' ';
    public const char Charcomma = ',';
    public const string Line = " - ";
    public const char Andchar = '&';
    public const string Space = " ";
    public const string Comma = ",";
    public const string Empty = "";
    public const string And = "&";
    public const string T = "\t";
    public const string F = "f";

    // Array of all supported parameter types
    // If you want to add types to this list,
    // you need to modify ParameterParser.ParseBuiltInTypes() function.
    public static readonly Type[] SupportedTypes =
    {
        typeof(int), typeof(float),
        typeof(decimal), typeof(double),
        typeof(bool), typeof(string),
        typeof(char), typeof(string[]),
        typeof(Vector2), typeof(Vector3),
        typeof(Vector4), typeof(Quaternion)
    };

    // Array of supported Unity types
    // If you want to add types to this list,
    // you need to modify ParameterParser.ParseUnityTypes() function.
    public static readonly Type[] UnityTypes =
    {
        typeof(Vector2), typeof(Vector3),
        typeof(Vector4), typeof(Quaternion)
    };
}
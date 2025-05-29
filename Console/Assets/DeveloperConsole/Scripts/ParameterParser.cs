using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System;

namespace Anarkila.DeveloperConsole
{
    public static class ParameterParser
    {
        // To avoid creating new list, which in C# creates garbage
        // we reuse this list
        private static List<float> unityTypeList = new();

        /// <summary>
        ///  Check if parameter type is supported
        /// </summary>
        /// <returns></returns>
        public static bool IsSupportedType(ParameterInfo[] parameters, bool isCoroutine, string methodName,
            string commandName, Type className)
        {
            // Early return if method doesn't take in any parameters.
            if (parameters == null || parameters.Length == 0)
            {
                return true;
            }

            // limit max number of parameters to 10, this is artifical limit.
            if (parameters.Length >= 10)
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING +
                          "10 or more parameters is a bit extreme for single method, don't you think? " +
                          $"Command '{commandName}' in '{className}' '{methodName}' will be ignored.");
#endif
                return false;
            }

            if (isCoroutine && parameters.Length >= 2)
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING + "Unity coroutines are limited to max one argument. " +
                          $"Command '{commandName}' in '{className}' '{methodName}' will be ignored.");
#endif
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters.Length >= 2)
                {
                    if (ConsoleConstants.UnityTypes.Contains(parameters[i].ParameterType) ||
                        parameters[i].ParameterType == typeof(string[]))
                    {
#if UNITY_EDITOR
                        // Multiple parameters with parameters: Vector2/3/4 or string[] is currently not supported
                        // because parameters are parsed by character ',' (comma)
                        // method that takes in single Vector2/3/4 or string[] is supported.
                        Debug.Log(ConsoleConstants.EDITORWARNING +
                                  $"Method contains multiple parameters with {parameters[i].ParameterType}, this is not supported! " +
                                  $"Command '{commandName}' in '{className}' '{methodName}' will be ignored!");
#endif
                        return false;
                    }
                }

                if (!ConsoleConstants.SupportedTypes.Contains(parameters[i].ParameterType))
                {
#if UNITY_EDITOR
                    Debug.Log(ConsoleConstants.EDITORWARNING +
                              $"Parameter typeof {parameters[i].ParameterType} is not supported! \n" +
                              $"Command '{commandName}' in '{className}' '{methodName}' will be ignored!");
#endif
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Try to parse parameter from string
        /// </summary>
        public static object[] ParseParametersFromString(string[] input, string raw, Type[] type,
            ConsoleCommandData data)
        {
            // UnityEngine types such as Vector2, Vector3 etc are not part of C# TypeCode
            // so they must be checked with other way

            if (input == null)
            {
                if (type.Length != 0)
                {
                    input = new string[type.Length];
                }
                else
                {
                    return input;
                }
            }

            object[] parameters = new object[type.Length];
            for (int i = 0; i < type.Length; i++)
            {
                bool containsUnityType = ConsoleConstants.UnityTypes.Contains(type[i]);

                if (InBounds(i, input.Length))
                {
                    parameters[i] = containsUnityType
                        ? ParseUnityTypes(raw, type[i])
                        : ParseBuiltInTypes(input[i], type[i], raw);
                }
                else if (data.optionalParameter[i])
                {
                    parameters[i] = null;
                }
            }

            return parameters;
        }

        private static bool InBounds(int index, int totalLength)
        {
            return index >= 0 && index < totalLength;
        }

        private static object ParseUnityTypes(string input, Type type)
        {
            if (input == null)
            {
                return input;
            }

            // Delete all character 'f' from string
            input = ConsoleUtils.DeleteCharacterF(input);

            string[] paramArr;
            if (input.Contains(ConsoleConstants.COMMA))
            {
                paramArr = input.Split(ConsoleConstants.CHARCOMMA);
            }
            else
            {
                paramArr = input.Split();
            }

            float f;

            // To avoid creating new list, which in C# creates garbage
            // we reuse this list
            unityTypeList.Clear();

            for (int i = 0; i < paramArr.Length; i++)
            {
                if (float.TryParse(paramArr[i], out f))
                {
                    unityTypeList.Add(f);
                }
            }

            if (type == typeof(Vector2) && unityTypeList.Count == 2)
            {
                return new Vector2(unityTypeList[0], unityTypeList[1]);
            }
            else if (type == typeof(Vector3) && unityTypeList.Count == 3)
            {
                return new Vector3(unityTypeList[0], unityTypeList[1], unityTypeList[2]);
            }
            else if (type == typeof(Vector4) && unityTypeList.Count == 4)
            {
                return new Vector4(unityTypeList[0], unityTypeList[1], unityTypeList[2], unityTypeList[3]);
            }
            else if (type == typeof(Quaternion) && unityTypeList.Count == 4)
            {
                return new Quaternion(unityTypeList[0], unityTypeList[1], unityTypeList[2], unityTypeList[3]);
            }
            else
            {
                return null;
            }
        }

        private static object ParseBuiltInTypes(string input, Type type, string raw)
        {
            if (type == typeof(string[]))
            {
                return ParseStringArray(raw);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int32:
                    return ParseInt(input);
                case TypeCode.Boolean:
                    return ParseBoolean(input);
                case TypeCode.Decimal:
                    return ParseDecimal(input);
                case TypeCode.Double:
                    return ParseDouble(input);
                case TypeCode.Single:
                    return ParseFloat(input);
                case TypeCode.String:
                    return input;
                case TypeCode.Char:
                    return ParseChar(input);
                default:
                    return null;
            }
        }

        private static object ParseStringArray(string input)
        {
            if (input == null)
            {
                return input;
            }

            string[] words = input.Split(ConsoleConstants.CHARCOMMA);
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = words[i].Trim(); // Remove all whitespaces from start and end of the string
            }

            return words as object;
        }

        private static object ParseInt(string input)
        {
            input = ConsoleUtils.DeleteWhiteSpacesFromString(input);

            int number;
            bool success = int.TryParse(input, out number);
            if (success)
            {
                return number;
            }
            else
            {
                return null;
            }
        }

        private static object ParseFloat(string input)
        {
            input = ConsoleUtils.DeleteWhiteSpacesFromString(ConsoleUtils.DeleteCharacterF(input));

            float number;
            bool success = float.TryParse(input, out number);
            if (success)
            {
                return number;
            }
            else
            {
                return null;
            }
        }

        private static object ParseBoolean(string input)
        {
            input = ConsoleUtils.DeleteWhiteSpacesFromString(input);

            bool success = bool.TryParse(input, out success);
            if (success)
            {
                return success;
            }
            else
            {
                return null;
            }
        }

        private static object ParseDouble(string input)
        {
            input = ConsoleUtils.DeleteWhiteSpacesFromString(input);

            double number;
            bool success = double.TryParse(input, out number);
            if (success)
            {
                return number;
            }
            else
            {
                return null;
            }
        }

        private static object ParseDecimal(string input)
        {
            input = ConsoleUtils.DeleteWhiteSpacesFromString(input);

            decimal number;
            bool success = decimal.TryParse(input, out number);
            if (success)
            {
                return number;
            }
            else
            {
                return null;
            }
        }

        private static object ParseChar(string input)
        {
            input = ConsoleUtils.DeleteWhiteSpacesFromString(input);

            char value;
            bool success = char.TryParse(input, out value);
            if (success)
            {
                return value;
            }
            else
            {
                return null;
            }
        }
    }
}
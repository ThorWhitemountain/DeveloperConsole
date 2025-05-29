using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Profiling;

namespace Anarkila.DeveloperConsole
{
#pragma warning disable 0168
#pragma warning disable 0219
    public static class CommandDatabase
    {
        private static readonly List<ConsoleCommandData> ConsoleCommandsRegisteredBeforeInit = new();
        private static readonly Dictionary<string, bool> CommandRemovedBeforeInit = new();
        private static readonly List<ConsoleCommandData> ConsoleCommands = new(32);
        private static readonly List<ConsoleCommandData> StaticCommands = new(32);
        private static readonly List<string> CommandStringsWithDefaultValues = new(32);
        private static readonly List<string> CommandStringsWithInfos = new(32);
        private static readonly List<string> ConsoleCommandList = new(32);
        private static readonly List<string> ExecutedCommands = new(32);
        private static readonly List<string> ParseList = new();
        private static bool allowMultipleCommands = true;
        private static bool staticCommandsCached;
        private static bool trackDuplicates;
        private static bool trackFailedCommands = true;
        private static int executedCommandCount;
        private static int failedCommandCount;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            // for domain reload purposes
            ConsoleCommandsRegisteredBeforeInit.Clear();
            CommandRemovedBeforeInit.Clear();
            ConsoleCommands.Clear();
            StaticCommands.Clear();
            CommandStringsWithDefaultValues.Clear();
            CommandStringsWithInfos.Clear();
            ConsoleCommandList.Clear();
            ExecutedCommands.Clear();
            ParseList.Clear();
            staticCommandsCached = false;
            allowMultipleCommands = true;
            trackDuplicates = false;
            executedCommandCount = 0;
            failedCommandCount = 0;
        }
#endif

        /// <summary>
        /// Try to execute console command
        /// </summary>
        public static bool TryExecuteCommand(string input)
        {
            if (!ConsoleManager.IsConsoleInitialized())
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING +
                          "Unable to execute command. Developer Console does not exist in the scene or has been destroyed.");
#endif
                return false;
            }

            bool success = false;

            // Does input contains character "&"
            bool constainsAnd = input.Contains(ConsoleConstants.AND);

            // Execute single command
            if (!constainsAnd || !allowMultipleCommands)
            {
                success = ExecuteCommand(input, constainsAnd);
            }

            // If single command failed then test multi but only if input contains character "&"
            if (!success && constainsAnd && allowMultipleCommands)
            {
                List<string> commandList = ParseMultipleCommands(input);
                if (commandList == null || commandList.Count == 0)
                {
                    return success;
                }

                for (int i = 0; i < commandList.Count; i++)
                {
                    success = ExecuteCommand(commandList[i]);
                    // uncomment this to return after command have failed.
                    //if (!success) return success;
                }
            }

            return success;
        }

        private static bool ExecuteCommand(string input, bool silent = false)
        {
            bool caseSensetive = ConsoleManager.IsCaseSensetive();
            string[] parametersAsString = null;
            string remaining = string.Empty;
            bool parametersParsed = false;
            object[] parameters = null;
            string rawInput = input;
            bool success = false;

            if (!caseSensetive)
            {
                input = input.ToLower();
            }

            // Parse command and parameter(s) from input
            if (input.Contains(ConsoleConstants.SPACE))
            {
                int index = input.IndexOf(ConsoleConstants.EMPTYCHAR);
                index = input.IndexOf(ConsoleConstants.EMPTYCHAR, index);
                remaining = input.Substring(index + 1);
                parametersAsString = remaining.Split(ConsoleConstants.CHARCOMMA);
                input = input.Substring(0, index);
                if (!caseSensetive)
                {
                    input = input.ToLower();
                }
            }

            // Loop through all console commands and try to find matching command
            for (int i = 0; i < ConsoleCommands.Count; i++)
            {
                string command = caseSensetive ? ConsoleCommands[i].commandName : ConsoleCommands[i].commandNameLower;
                if (command != input)
                {
                    continue;
                }

                // If command does not take parameter and user passed in parameter --> continue
                if (ConsoleCommands[i].parameters == null && parametersAsString != null)
                {
                    continue;
                }

                if (parametersAsString != null || ConsoleCommands[i].parameters.Length != 0)
                {
                    // We only need parse this once
                    if (!parametersParsed)
                    {
                        parameters = ParameterParser.ParseParametersFromString(parametersAsString, remaining,
                            ConsoleCommands[i].parameters, ConsoleCommands[i]);
                        parametersParsed = true;
                    }

                    // Do final checks before trying to call command
                    bool wrongParameter = false;
                    if (parameters != null)
                    {
                        for (int j = 0; j < parameters.Length; j++)
                        {
                            if (parameters[j] == null && !ConsoleCommands[i].optionalParameter[j])
                            {
                                wrongParameter = true;
                                break;
                            }
                        }

                        if (parameters.Length != ConsoleCommands[i].parameters.Length)
                        {
                            wrongParameter = true;
                        }
                    }

                    if (parameters == null && ConsoleCommands[i].parameters != null)
                    {
                        wrongParameter = true;
                    }

                    if (wrongParameter)
                    {
                        continue;
                    }
                }

                try
                {
                    if (ConsoleCommands[i].monoScript == null && !ConsoleCommands[i].isStaticMethod)
                    {
                        // This can happen when GameObject with [ConsoleCommand()] attribute is destroyed runtime.
                        ConsoleCommands.Remove(ConsoleCommands[i]);
                        UpdateLists();
                        ConsoleEvents.ListsChanged();
                        continue;
                    }

                    // if Command doesn't take in any parameters, use parsed input instead
                    if (ConsoleCommands[i].parameters.Length == 0)
                    {
                        rawInput = input;
                    }

                    if (ConsoleCommands[i].isCoroutine)
                    {
                        object param = ConsoleCommands[i].parameters.Length == 0 ? null : parameters[0];

                        // Starting coroutine by string limits argument count to one.
                        // https://docs.unity3d.com/ScriptReference/MonoBehaviour.StartCoroutine.html
                        // If you need to start coroutine with multiple parameters
                        // make a normal method that starts the coroutine instead.
                        ConsoleCommands[i].monoScript.StartCoroutine(ConsoleCommands[i].methodName, param);
                        success = true;
                        continue;
                    }

                    if (ConsoleCommands[i].methodInfo == null)
                    {
                        continue;
                    }

                    // MethodInfo.Invoke is quite slow but it should be okay for this use case.
                    // Commands are not called, or at least should not be called that often it to matter.
                    ConsoleCommands[i].methodInfo.Invoke(ConsoleCommands[i].monoScript, parameters);
                    success = true;
                }
                catch (ArgumentException e)
                {
                    // Allow expection to be thrown so it can be printed to console (depending on the print setting)
                }
                finally
                {
                    ++executedCommandCount;
                }
            }

            if (success || trackFailedCommands)
            {
                bool contains = ExecutedCommands.Contains(rawInput);
                if (!contains || trackDuplicates)
                {
                    ExecutedCommands.Add(rawInput);
                }
                // Not pretty, but this keeps the list ordered correctly
                else if (contains)
                {
                    ExecutedCommands.Remove(rawInput);
                    ExecutedCommands.Add(rawInput);
                }
            }

            if (!success)
            {
                // TODO: perhaps there should be log if command was right but parameter was wrong?
                if (!silent && ConsoleManager.PrintUnrecognizedCommandInfo())
                {
                    Console.Log($"Command '{rawInput}' was not recognized.");
                    ++failedCommandCount;
                }
            }

            ConsoleEvents.CommandExecuted(success);

            return success;
        }

        /// <summary>
        /// Parse multiple commands separated by "&" or "&&"
        /// </summary>
        private static List<string> ParseMultipleCommands(string input)
        {
            if (input == null || input.Length == 0)
            {
                return null;
            }

            ParseList.Clear();
            string[] commandArray = input.Split(ConsoleConstants.ANDCHAR);

            for (int i = 0; i < commandArray.Length; i++)
            {
                // if commandArray length is 0, skip it
                // this likely happens because "&&" was typed instead of "&"
                if (commandArray[i].Length == 0)
                {
                    continue;
                }

                char[] arr = commandArray[i].ToCharArray();
                for (int j = 0; j < arr.Length; j++)
                {
                    if (arr[j] != ConsoleConstants.EMPTYCHAR)
                    {
                        break;
                    }

                    commandArray[i] = commandArray[i].Substring(1);
                }

                ParseList.Add(commandArray[i]);
            }

            return ParseList;
        }

        /// <summary>
        /// Register new Console command
        /// </summary>
        public static void RegisterCommand(MonoBehaviour script, string methodName, string command,
            string defaultValue = "", string info = "",
            bool debugCommandOnly = false, bool isHiddenCommand = false, bool hiddenCommandMinimalGUI = false)
        {
            if (!ConsoleManager.IsRunningOnMainThread(System.Threading.Thread.CurrentThread))
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING +
                          "Console.RegisterCommand cannot be called from another thread.");
#endif
                return;
            }

            if (command == null || command.Length == 0 || methodName == null || methodName.Length == 0)
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING + "command or methodname is null or empty!");
# endif
                return;
            }

            if (script == null)
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING +
                          "MonoBehaviour reference is null! If you are registering non-Monobehaviour commands, Use [ConsoleCommand()] attribute instead.");
#endif
                return;
            }

            if (ConsoleManager.GetSettings().registerStaticCommandsOnly)
            {
#if UNITY_EDITOR
                Debug.Log(ConsoleConstants.EDITORWARNING +
                          "Trying to register new MonoBehaviour command while option RegisterStaticCommandsOnly is enabled.");
#endif
                return;
            }

            if (debugCommandOnly && !Debug.isDebugBuild)
            {
                return;
            }

            MethodInfo methodInfo = null;
            methodInfo = script.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (methodInfo == null)
            {
                return;
            }

            ConsoleCommandData data = CreateCommandData(methodInfo, script, methodName, command, defaultValue, info,
                isHiddenCommand, hiddenCommandMinimalGUI);
            if (data == null)
            {
                return;
            }

            if (ConsoleManager.IsConsoleInitialized())
            {
                ConsoleCommands.Add(data);
                UpdateLists();
                ConsoleEvents.ListsChanged();
            }
            else
            {
                // new command registered before console was initialized
                ConsoleCommandsRegisteredBeforeInit.Add(data);
            }
        }

        /// <summary>
        /// Remove command
        /// </summary>
        public static void RemoveCommand(string command, bool log = false, bool forceDelete = false)
        {
            if (!ConsoleManager.IsRunningOnMainThread(System.Threading.Thread.CurrentThread))
            {
#if UNITY_EDITOR
                Debug.Log(
                    ConsoleConstants.EDITORWARNING + "Console.RemoveCommand cannot be called from another thread.");
#endif
                return;
            }

            if (command == null || command.Length == 0 || !Application.isPlaying)
            {
                return;
            }

            if (!ConsoleManager.IsConsoleInitialized() && !forceDelete)
            {
                if (!CommandRemovedBeforeInit.ContainsKey(command))
                {
                    CommandRemovedBeforeInit.Add(command, log);
                }

                return;
            }

            bool foundAny = false;
            List<ConsoleCommandData> toRemove = new();
            for (int i = 0; i < ConsoleCommands.Count; i++)
            {
                if (command == ConsoleCommands[i].commandName)
                {
                    toRemove.Add(ConsoleCommands[i]);
                    foundAny = true;
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                ConsoleCommands.Remove(toRemove[i]);
                if (toRemove[i].isStaticMethod)
                {
                    StaticCommands.Remove(toRemove[i]);
                }
            }

#if UNITY_EDITOR
            if (log)
            {
                if (foundAny)
                {
                    Console.Log($"Removed command [{command}]");
                }
                else
                {
                    Console.Log($"Didn't find command with name [{command}]");
                }
            }
#endif
            UpdateLists();
            ConsoleEvents.ListsChanged();
        }

        /// <summary>
        /// Get all [ConsoleCommand()] attributes
        /// </summary>
        public static List<ConsoleCommandData> GetConsoleCommandAttributes(bool isDebugBuild, bool staticOnly,
            bool scanAllAssemblies = false, string projectAssemblyPrefix = "")
        {
            ConsoleCommands.Clear();

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                 BindingFlags.Static;

            if (staticCommandsCached)
            {
                if (staticOnly)
                {
                    return StaticCommands;
                }

                flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            }

            List<ConsoleCommandData> commandList = new(64);
            Profiler.BeginSample("GetAllAttributesFromAssembly");
            List<MethodInfo> methods = GetAllAttributesFromAssembly(flags, scanAllAssemblies, projectAssemblyPrefix);
            Profiler.EndSample();

            // Loop through all methods with [ConsoleCommand()] attributes
            foreach (MethodInfo method in methods)
            {
                if (method.IsStatic && staticCommandsCached)
                {
                    continue;
                }

                ConsoleCommand attribute = (ConsoleCommand)method.GetCustomAttributes(typeof(ConsoleCommand), false)[0];

                if (attribute == null || (attribute.IsDebugOnlyCommand() && !isDebugBuild))
                {
                    continue;
                }

                ConsoleCommandData data = CreateCommandData(method, null, method.Name,
                    attribute.GetCommandName(), attribute.GetValue(),
                    attribute.GetInfo(), attribute.IsHiddenCommand(),
                    attribute.IsHiddenMinimalGUI());

                if (data == null)
                {
                    continue;
                }

                if (method.IsStatic)
                {
                    StaticCommands.Add(data);
                }
                else
                {
                    commandList.Add(data);
                }
            }

            if (!staticCommandsCached && staticOnly)
            {
                ConsoleCommands.AddRange(StaticCommands);
            }

            staticCommandsCached = true;
            return commandList;
        }

        /// <summary>
        /// Create ConsoleCommandData data.
        /// </summary>
        private static ConsoleCommandData CreateCommandData(MethodInfo methodInfo, MonoBehaviour script,
            string methodName, string command, string defaultValue, string info, bool isHiddenCommand,
            bool hiddenCommandMinimalGUI)
        {
            if (methodInfo == null)
            {
                return null;
            }

            Type className = methodInfo.DeclaringType;
            string classNameString = className.ToString();

            if (string.IsNullOrEmpty(command))
            {
#if UNITY_EDITOR
                // this warning means you have method with [ConsoleCommand(null)] or [ConsoleCommand("")] somewhere.
                // Below message should print the script and method where this is located.
                Debug.Log(
                    $"{ConsoleConstants.EDITORWARNING}{classNameString}.{methodName} [ConsoleCommand] name is empty or null! Please assign different command name.");
#endif
                return null;
            }

            if (command.Contains(ConsoleConstants.AND) || command.Contains(ConsoleConstants.COMMA) ||
                command.Contains(ConsoleConstants.SPACE))
            {
#if UNITY_EDITOR
                // [ConsoleCommand()] cannot contain characters '&' or ',' (comma) because
                // character '&' is used to parse multiple commands
                // and character ',' (comma) is used to parse multiple parameters
                Debug.Log(
                    $"{ConsoleConstants.EDITORWARNING}[ConsoleCommand] cannot contain whitespace, '&' or comma. Rename command [{command}] in {classNameString}{methodName}");
#endif
                return null;
            }

            bool isStatic = methodInfo.IsStatic;
            bool isCoroutine = methodInfo.ToString().Contains(ConsoleConstants.IENUMERATOR);
            ParameterInfo[] methodParams = methodInfo.GetParameters();
            Type[] paraType = new Type[methodParams.Length];
            bool[] optionalParameters = new bool[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                paraType[i] = methodParams[i].ParameterType;
                optionalParameters[i] = methodParams[i].IsOptional;
            }

            if (!ParameterParser.IsSupportedType(methodParams, isCoroutine, methodName, command, className))
            {
                return null;
            }

            if (CheckForDuplicates(ConsoleCommands, paraType, command, classNameString, methodName))
            {
                return null;
            }

            if (defaultValue == null)
            {
                defaultValue = "";
            }

            ConsoleCommandData data = new(script, methodName, command, defaultValue, info, paraType, isStatic,
                methodInfo, isCoroutine, optionalParameters, isHiddenCommand, hiddenCommandMinimalGUI, classNameString);

            return data;
        }

        /// <summary>
        /// Get all ConsoleCommand attributes from assembly
        /// </summary>
        private static List<MethodInfo> GetAllAttributesFromAssembly(BindingFlags flags, bool scanAllAssemblies = false,
            string projectAssemblyPrefix = "")
        {
            List<MethodInfo> cb = new();

            // Looping through all assemblies is slow
            if (scanAllAssemblies)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (!assemblies[i].FullName.Contains(projectAssemblyPrefix))
                    {
                        continue;
                    }

                    Type[] types = assemblies[i].GetTypes();
                    for (int j = 0; j < types.Length; j++)
                    {
                        FindAttributeAndAdd(flags, j, types, cb);
                    }
                }
            }
            else
            {
                // else loop through current assembly which should be Unity assembly
                Assembly unityAssembly = Assembly.GetExecutingAssembly();
                Type[] types = unityAssembly.GetTypes();

                for (int i = 0; i < types.Length; i++)
                {
                    FindAttributeAndAdd(flags, i, types, cb);
                }
            }

            return cb;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FindAttributeAndAdd(BindingFlags flags, int j, Type[] type, List<MethodInfo> cb)
        {
            if (!type[j].IsClass)
            {
                return;
            }

            MethodInfo[] methodInfos = type[j].GetMethods(flags);
            for (int i = 0; i < methodInfos.Length; i++)
            {
                if (methodInfos[i].GetCustomAttributes(typeof(ConsoleCommand), false).Length > 0)
                {
                    cb.Add(methodInfos[i]);
                }
            }
        }

        /// <summary>
        /// Register MonoBehaviour commands
        /// </summary>
        public static void RegisterMonoBehaviourCommands(List<ConsoleCommandData> commands)
        {
            // Find all MonoBehaviour class names
            List<string> scriptNames = new();
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].isStaticMethod)
                {
                    continue;
                }

                if (!scriptNames.Contains(commands[i].scriptNameString))
                {
                    scriptNames.Add(commands[i].scriptNameString);
                }
            }

            // Loop through all MonoBehaviour classes added above.
            // This uses GameObject.FindObjectsOfType to find all those scripts in the current scene and
            // loops through them to find MonoBehaviour references.
            // these loops look scary but this is reasonable fast
            for (int i = 0; i < scriptNames.Count; i++)
            {
                Type type = Type.GetType(scriptNames[i]);
                if (type == null)
                {
                    continue;
                }

                MonoBehaviour[] monoScripts = GameObject.FindObjectsOfType(type) as MonoBehaviour[];
                for (int j = 0; j < monoScripts.Length; j++)
                {
                    if (monoScripts[j] == null)
                    {
                        continue;
                    }

                    string scriptName = monoScripts[j].GetType().ToString();

                    for (int k = 0; k < commands.Count; k++)
                    {
                        if (commands[k].isStaticMethod)
                        {
                            continue;
                        }

                        MonoBehaviour script = null;
                        if (scriptName == commands[k].scriptNameString)
                        {
                            script = monoScripts[j];
                        }

                        if (script != null)
                        {
                            ConsoleCommandData data = new(script, commands[k].methodName, commands[k].commandName,
                                commands[k].defaultValue, commands[k].info, commands[k].parameters, false,
                                commands[k].methodInfo, commands[k].isCoroutine, commands[k].optionalParameter,
                                commands[k].hiddenCommand);
                            ConsoleCommands.Add(data);
                        }
                    }
                }
            }

            // Add static commands to final console command list
            ConsoleCommands.AddRange(StaticCommands);

            // If user called Console.RegisterCommand before console was fully initialized, Add those commands now.
            if (ConsoleCommandsRegisteredBeforeInit.Count != 0)
            {
                for (int i = 0; i < ConsoleCommandsRegisteredBeforeInit.Count; i++)
                {
                    ConsoleCommandData command = ConsoleCommandsRegisteredBeforeInit[i];
                    if (CheckForDuplicates(ConsoleCommands, command.parameters, command.commandName,
                            command.scriptNameString, command.methodName))
                    {
                        ConsoleCommandsRegisteredBeforeInit.Remove(command);
                    }
                    else
                    {
                        ConsoleCommands.Add(command);
                    }
                }

                ConsoleCommandsRegisteredBeforeInit.Clear();
            }

            // If user called Console.RemoveCommand before console was fully initilized
            // Remove those commands now.
            if (CommandRemovedBeforeInit.Count != 0)
            {
                foreach (KeyValuePair<string, bool> dict in CommandRemovedBeforeInit)
                {
                    RemoveCommand(dict.Key, dict.Value, true);
                }
            }

            UpdateLists();
        }

        /// <summary>
        /// Generate needed console lists
        /// </summary>
        public static void UpdateLists()
        {
            trackFailedCommands = ConsoleManager.TrackFailedCommands();
            allowMultipleCommands = ConsoleManager.AllowMultipleCommands();
            trackDuplicates = ConsoleManager.TrackDuplicates();

            CommandStringsWithDefaultValues.Clear();
            CommandStringsWithInfos.Clear();
            ConsoleCommandList.Clear();

            ConsoleGUIStyle style = ConsoleManager.GetGUIStyle();
            char space = ' ';

            for (int i = 0; i < ConsoleCommands.Count; i++)
            {
                if (ConsoleCommands[i].hiddenCommand)
                {
                    continue;
                }

                if (ConsoleCommands[i].hiddenCommandMinimalGUI && style == ConsoleGUIStyle.Minimal)
                {
                    continue;
                }

                if (!ConsoleCommandList.Contains(ConsoleCommands[i].commandName))
                {
                    ConsoleCommandList.Add(ConsoleCommands[i].commandName);
                }

                if (!string.IsNullOrWhiteSpace(ConsoleCommands[i].info))
                {
                    string fullText = ConsoleCommands[i].commandName + ConsoleConstants.LINE + ConsoleCommands[i].info;
                    if (!CommandStringsWithInfos.Contains(fullText))
                    {
                        CommandStringsWithInfos.Add(fullText);
                    }
                }
                else
                {
                    if (!CommandStringsWithInfos.Contains(ConsoleCommands[i].commandName))
                    {
                        CommandStringsWithInfos.Add(ConsoleCommands[i].commandName);
                    }
                }

                // Ensure first character in a string is space
                string defaultValue = ConsoleCommands[i].defaultValue;
                char first = defaultValue.FirstOrDefault();
                if (first != space)
                {
                    defaultValue = space + defaultValue;
                }

                string full = ConsoleCommands[i].commandName + defaultValue;

                if (!CommandStringsWithDefaultValues.Contains(full))
                {
                    CommandStringsWithDefaultValues.Add(full);
                }
            }
        }

        public static void PrintAllCommands()
        {
            ConsoleSettings settings = ConsoleManager.GetSettings();

            if (settings == null)
            {
                return;
            }

            List<string> commands = settings.printCommandInfoTexts
                ? GetConsoleCommandsWithInfos()
                : GetConsoleCommandList();

            if (settings.printCommandsAlphabeticalOrder)
            {
                commands = commands.OrderBy(x => x).ToList();
            }

            Console.LogEmpty();
            ConsoleEvents.Log(ConsoleConstants.COMMANDMESSAGE, logType: LogType.Log);
            for (int i = 0; i < commands.Count; i++)
            {
                ConsoleEvents.Log(commands[i], logType: LogType.Log);
            }
        }

        /// <summary>
        /// Check that list doesn't already contain command that we are trying to register.
        /// </summary>
        private static bool CheckForDuplicates(List<ConsoleCommandData> commands, Type[] parameters, string commandName,
            string className, string methodName)
        {
            if (commands.Count == 0)
            {
                return false;
            }

            bool found = false;

            for (int i = 0; i < commands.Count; i++)
            {
                if (commandName == commands[i].commandName)
                {
                    if (className != commands[i].scriptNameString || methodName != commands[i].methodName ||
                        parameters != commands[i].parameters)
                    {
#if UNITY_EDITOR
                        Debug.Log(ConsoleConstants.EDITORWARNING +
                                  $"Command '{commandName}' has already been registered. " +
                                  $"Command '{commandName}' in class '{className}' with method name '{methodName}' will be ignored. " +
                                  "Give this attribute other command name!");
#endif
                        found = true;
                    }
                }
            }

            return found;
        }

        public static int GetExcecutedCommandCount()
        {
            return executedCommandCount;
        }

        public static int GetFailedCommandCount()
        {
            return failedCommandCount;
        }

        public static List<ConsoleCommandData> GetConsoleCommands()
        {
            return ConsoleCommands;
        }

        public static int GetConsoleCommandsCount()
        {
            return ConsoleCommands.Count;
        }

        public static int GetStaticConsoleCommandsCount()
        {
            return StaticCommands.Count;
        }

        public static List<string> GeCommandStringsWithDefaultValues()
        {
            return CommandStringsWithDefaultValues;
        }

        public static List<string> GetConsoleCommandList()
        {
            return ConsoleCommandList;
        }

        public static List<string> GetConsoleCommandsWithInfos()
        {
            return CommandStringsWithInfos;
        }

        public static bool StaticCommandsRegistered()
        {
            return staticCommandsCached;
        }

        public static List<string> GetPreviouslyExecutedCommands()
        {
            return ExecutedCommands;
        }
    }
}
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Anarkila.DeveloperConsole
{
    public class ConsoleInputField : MonoBehaviour
    {
        private WaitForSecondsRealtime cachedDelay = new(0.050f);
        private List<string> commandsWithValues = new();
        private List<string> allConsoleCommands = new();
        private List<string> executedCommands = new();
        private List<string> closestMatches = new();
        private List<string> predictions = new();
        private bool predictionPanelVisible;
        private bool shouldShowPredictions = true;
        private bool allowPredictions = true;
        private bool shouldRunPredictionCheck = true;
        private int previousCommandIndex;
        private bool allowEnterClick = true;
        private TMP_InputField inputField;
        private string currentSuggestion;
        private int suggestionIndex;
        private string previousText;

        ///Used to prevent cycling through suggestions from updating suggestions 
        private bool ignoreNext;

        private void Awake()
        {
            bool gotInput = TryGetComponent(out inputField);

#if UNITY_EDITOR
            if (!gotInput)
            {
                Debug.Log($"GameObject {gameObject.name} doesn't have TMP_InputField component!");
                enabled = false;
                return;
            }
#endif
            previousText = inputField.text;

            ConsoleEvents.RegisterInputPredctionChanged += InputPredictionSettingChanged;
            ConsoleEvents.RegisterPreviousCommandEvent += SearchPreviousCommand; // TODO. rename this event?
            ConsoleEvents.RegisterFillCommandEvent += FillCommandFromSuggestion; // TODO. rename this event?
            ConsoleEvents.RegisterInputfieldTextEvent += SetInputfieldText;
            ConsoleEvents.RegisterOnCommandExecuted += NewCommandExecuted;
            ConsoleEvents.RegisterInputFieldSubmit += InputFieldSubmit;
            ConsoleEvents.RegisterListsChangedEvent += UpdateLists;
        }

        private void Start()
        {
            if (inputField == null)
            {
                return;
            }

            UpdateLists();
            inputField.onValueChanged.AddListener(UpdatePredictions);
        }

        private void OnDestroy()
        {
            ConsoleEvents.RegisterInputPredctionChanged -= InputPredictionSettingChanged;
            ConsoleEvents.RegisterFillCommandEvent -= FillCommandFromSuggestion;
            ConsoleEvents.RegisterPreviousCommandEvent -= SearchPreviousCommand;
            ConsoleEvents.RegisterInputfieldTextEvent -= SetInputfieldText;
            ConsoleEvents.RegisterOnCommandExecuted -= NewCommandExecuted;
            ConsoleEvents.RegisterInputFieldSubmit -= InputFieldSubmit;
            ConsoleEvents.RegisterListsChangedEvent -= UpdateLists;
        }

        private void NewCommandExecuted(bool success)
        {
            executedCommands.Clear();
            executedCommands.AddRange(CommandDatabase.GetPreviouslyExecutedCommands());
            executedCommands.Reverse();
            previousCommandIndex = 0;

            // reset shouldShowPredictions
            shouldShowPredictions = true;
        }

        private void InputPredictionSettingChanged(bool showPredictions)
        {
            allowPredictions = showPredictions;
        }

        //Set via button press, not from keyboard inputs
        private void SetInputfieldText(string input)
        {
            inputField.text = input;
            previousText = inputField.text;
            inputField.caretPosition = inputField.text.Length;

            if (gameObject.activeInHierarchy)
            {
                FocusInputField();
            }
        }

        private void UpdateLists()
        {
            commandsWithValues = CommandDatabase.GeCommandStringsWithDefaultValues();
            allConsoleCommands = CommandDatabase.GetConsoleCommandList();
            allowPredictions = ConsoleManager.ShowConsolePredictions();
        }

        private void OnEnable()
        {
            allowEnterClick = true;
            FocusInputField();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearInputField();
            ResetParameters();
            ClearSuggestion();
            previousCommandIndex = 0;
            allowEnterClick = true;
        }

        private void SearchPreviousCommand()
        {
            if (inputField == null || executedCommands.Count == 0)
            {
                return;
            }

            if (previousCommandIndex < 0)
            {
                previousCommandIndex = executedCommands.Count - 1;
            }
            else if (previousCommandIndex > executedCommands.Count || previousCommandIndex == executedCommands.Count)
            {
                previousCommandIndex = 0;
            }

            shouldShowPredictions = false;
            inputField.text = executedCommands[previousCommandIndex];

            //inputField.caretPosition = inputField.text.Length;
            inputField.MoveTextEnd(false);

            ++previousCommandIndex;
        }

        private void FillCommandFromSuggestion()
        {
            if (inputField == null || currentSuggestion == null)
            {
                return;
            }

            if (!shouldShowPredictions && !predictionPanelVisible)
            {
                previousCommandIndex -= 2; // not really ideal solution here.
                SearchPreviousCommand();
                return;
            }

            if (suggestionIndex > closestMatches.Count || suggestionIndex == closestMatches.Count)
            {
                suggestionIndex = 0;
            }

            if (closestMatches == null || closestMatches.Count == 0)
            {
                return;
            }

            ignoreNext = true;

            shouldShowPredictions = false;
            shouldRunPredictionCheck = false;
            previousText = inputField.text;
            inputField.text = closestMatches[suggestionIndex];

            //inputField.caretPosition = inputField.text.Length;
            inputField.MoveTextEnd(false);

            ++suggestionIndex;

            StartCoroutine(AllowEnterClickDelay());
        }

        private void InputFieldSubmit()
        {
            if (inputField == null || !allowEnterClick)
            {
                return;
            }

            string text = inputField.text;

            if (string.IsNullOrWhiteSpace(text))
            {
                ClearSuggestion();
                return;
            }

            allowEnterClick = false;
            ClearInputField();
            FocusInputField();
            ClearSuggestion();

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(AllowEnterClickDelay());
            }

            // Try to execute command
            CommandDatabase.TryExecuteCommand(text);
        }

        private IEnumerator AllowEnterClickDelay()
        {
            yield return cachedDelay;
            allowEnterClick = true;
        }

        private void ClearInputField()
        {
            if (inputField == null)
            {
                return;
            }

            inputField.Select();
            inputField.text = string.Empty;
        }

        private void FocusInputField()
        {
            if (inputField == null)
            {
                return;
            }

            // For some reason TMP_InputField doesn't work in OnEnable without delay
            StartCoroutine(DelayEnable());
        }

        private IEnumerator DelayEnable()
        {
            yield return cachedDelay;
            inputField.interactable = true;
            inputField.Select();
            inputField.ActivateInputField();
            shouldRunPredictionCheck = true;
        }

        private void ClearSuggestion()
        {
            closestMatches.Clear();
            predictions.Clear();
            ConsoleEvents.Predictions(closestMatches);
            predictionPanelVisible = false;
        }

        private void ResetParameters()
        {
            currentSuggestion = string.Empty;
            suggestionIndex = 0;
        }


        /// <summary>
        /// Try to find predictions from current inputfield text
        /// </summary>
        private void UpdatePredictions(string input)
        {
            // are predictions turned on for the devconsole
            if (!allowPredictions)
            {
                return;
            }

            previousText = input;

            // Used to not make selecting a prediction update the available predictions.
            if (ignoreNext)
            {
                ignoreNext = false;
                return;
            }

            // update predictions. (Gone from selecting a prediction to writing)
            if (input.Length != previousText.Length)
            {
                shouldRunPredictionCheck = true;
            }

            if (inputField == null || inputField.text.Length == 0)
            {
                closestMatches.Clear();
                ConsoleEvents.Predictions(null);
                predictionPanelVisible = false;
                return;
            }

            // if input is null, empty or contains character '&', then don't show any predictions.
            //TODO: allow prediction matching for last command (input.split(&)[^1]) ?
            if (string.IsNullOrEmpty(input) || input.Length == 0 || input.Contains(ConsoleConstants.AND))
            {
                closestMatches.Clear();
                ConsoleEvents.Predictions(null);
                predictionPanelVisible = false;
                return;
            }

            // if you selected a prediction, we pause the updating of the other predictions
            // until you make a change to the input field
            if (!shouldRunPredictionCheck)
            {
                return;
            }

            closestMatches.Clear();
            predictions.Clear();

            //arbitrary limit for when the command should be allowed
            const int tooDissimilar = 100;
            const int numberOfBestMatchesToKeep = 5;
            List<(int distance, string command)> bestMatches = new();

            // loop through all console commands strings and find the closest matching commands
            for (int i = 0; i < commandsWithValues.Count; i++)
            {
                input = input.ToLowerInvariant();
                string command = allConsoleCommands[i].ToLowerInvariant();

                if (!command.Contains(input))
                {
                    continue;
                }

                int distance = ConsoleUtils.CalcLevenshteinDistance(input, command);

                if (distance > tooDissimilar)
                {
                    continue;
                }

                AddMatch(distance, commandsWithValues[i]);
            }

            for (int i = 0; i < bestMatches.Count; i++)
            {
                predictions.Add(bestMatches[i].command);
                closestMatches.Add(bestMatches[i].command);
            }

            // Send prediction event
            ConsoleEvents.Predictions(predictions);

            predictionPanelVisible = predictions.Count != 0;
            if (predictions.Count == 0)
            {
                ClearSuggestion();
                ResetParameters();
            }

            return;

            void AddMatch(int dist, string command)
            {
                // insert in sorted position
                int i = 0;
                while (i < bestMatches.Count && bestMatches[i].distance <= dist)
                {
                    i++;
                }

                bestMatches.Insert(i, (dist, command));

                if (bestMatches.Count > numberOfBestMatchesToKeep)
                {
                    bestMatches.RemoveAt(bestMatches.Count - 1);
                }
            }
        }
    }
}
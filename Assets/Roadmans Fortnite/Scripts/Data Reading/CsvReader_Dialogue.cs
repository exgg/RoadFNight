using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace Roadmans_Fortnite.Scripts.Data_Reading
{
    public class CsvReaderDialogue : MonoBehaviour
    {
        // Drag and drop CSV files in the inspector
        public TextAsset[] dialogueCsvs;

        // The dictionary to hold dialogues based on speaker, target, and category
        private Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> _dialogueData;

        private void Start()
        {
            // Initialize the dictionary
            _dialogueData = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();

            // Load dialogues from each CSV file
            foreach (var dialogueMap in dialogueCsvs)
            {
                if (dialogueMap)
                    LoadDialogueFromCsv(dialogueMap.text);
                else
                {
                    int index = Array.IndexOf(dialogueCsvs, dialogueMap);
                    Debug.LogError($"There was an issue with the CSV at index {index}");
                }
            }
        }

        // Load CSV content into the dictionary
        private void LoadDialogueFromCsv(string csvContent)
        {
            // Split CSV content into lines
            string[] lines = csvContent.Split('\n');

            bool isFirstLine = true;

            foreach (var line in lines)
            {
                if (isFirstLine)
                {
                    isFirstLine = false; // Skip the header row
                    continue;
                }

                string[] values = line.Split(',');

                if (values.Length < 4) continue;

                string speaker = values[0].Trim();
                string target = values[1].Trim();
                string category = values[2].Trim();
                string dialogueLine = values[3].Trim();

                // If the speaker does not exist in the dictionary, add it
                if (!_dialogueData.ContainsKey(speaker))
                    _dialogueData[speaker] = new Dictionary<string, Dictionary<string, List<string>>>();

                // If the target does not exist under the speaker, add it
                if (!_dialogueData[speaker].ContainsKey(target))
                    _dialogueData[speaker][target] = new Dictionary<string, List<string>>();

                // If the category does not exist under the target, add it
                if (!_dialogueData[speaker][target].ContainsKey(category))
                    _dialogueData[speaker][target][category] = new List<string>();

                // Add the dialogue line to the relevant speaker, target, and category
                _dialogueData[speaker][target][category].Add(dialogueLine);
            }
        }

        // Method to get random dialogue based on speaker, target, and category
        public string GetRandomDialogue(string speaker, string target, string category)
        {
            if (_dialogueData.ContainsKey(speaker) &&
                _dialogueData[speaker].ContainsKey(target) &&
                _dialogueData[speaker][target].ContainsKey(category))
            {
                List<string> dialogues = _dialogueData[speaker][target][category];
                int randomIndex = Random.Range(0, dialogues.Count);

                return dialogues[randomIndex];
            }

            return "No Dialogue Found";
        }
    }
}

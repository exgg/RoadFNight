using Roadmans_Fortnite.Scripts.Data_Reading;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using TMPro;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine
{
    public class PedestrianDialogueManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dialogue;
        private CsvReaderDialogue _csvReaderDialogue;
        
        void Start()
        {
            _csvReaderDialogue = FindObjectOfType<CsvReaderDialogue>();
            
            GetRandomDialogueFromLookup();
        }

        private void GetRandomDialogueFromLookup()
        {
            string test = _csvReaderDialogue.GetRandomDialogue("Pagan", "Christian","Neutral");

            dialogue.text = test;
        }
    }
}

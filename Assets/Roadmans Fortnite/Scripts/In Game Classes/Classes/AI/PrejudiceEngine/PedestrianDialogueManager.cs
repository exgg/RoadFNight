using System.Collections;
using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.Data_Reading;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine
{
    public class PedestrianDialogueManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dialogue;
        private CsvReaderDialogue _csvReaderDialogue;

        [SerializeField] private float dialogueTimer;
        [SerializeField] private float dialogueCooldown;
        
        private bool _startedDialogue;
        private bool _dialogueOnCooldown;
        
        private StateHandler _stateHandler;
        private Pedestrian _pedestrian;
        
        void Start()
        {
            dialogue.transform.parent.gameObject.SetActive(false);
            
            _csvReaderDialogue = FindObjectOfType<CsvReaderDialogue>();
            _stateHandler = GetComponent<StateHandler>();
            _pedestrian = GetComponent<Pedestrian>();
        }

        public void StartDialogue(string speaker, string target, string category)
        {
            Debug.Log("Starting dialogue");
            
            if (_startedDialogue && !_dialogueOnCooldown)
                return;
            
            
            
            StartCoroutine(TriggerDialogue(speaker, target, category));
        }
        
        private IEnumerator TriggerDialogue(string speaker, string target, string category)
        {
            Debug.Log("Looking for dialogue");

            _startedDialogue = true;
            
            GetRandomDialogueFromLookup(speaker, target, category);
            dialogue.transform.parent.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(dialogueTimer);
            
            dialogue.transform.parent.gameObject.SetActive(false);
            _startedDialogue = false;
            _dialogueOnCooldown = true;
            
            
            if (_stateHandler.InteractionChecker(_pedestrian.aggressionLevel, _pedestrian.confidenceLevel) !=
                "Aggressive")
                _stateHandler.currentTarget = null;

            StartCoroutine(StartCooldown());
        }

        private IEnumerator StartCooldown()
        {
            yield return new WaitForSeconds(dialogueCooldown);

            _dialogueOnCooldown = false;
        }
        
        private void GetRandomDialogueFromLookup(string speaker, string target, string category)
        {
            string text = _csvReaderDialogue.GetRandomDialogue(speaker, target, category);

            dialogue.text = text;
        }

   
    }
}

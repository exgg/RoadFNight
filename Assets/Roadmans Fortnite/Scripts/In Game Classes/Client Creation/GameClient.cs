using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Client_Creation
{
    public class GameClient : MonoBehaviour
    {
        private void Start()
        {
            NetworkClient.RegisterHandler<GameStartMessage>(OnGameStartMessageReceived);
            NetworkClient.RegisterHandler<GameEndMessage>(OnGameEndMessageReceived);
        }

        private void OnGameStartMessageReceived(GameStartMessage msg)
        {
            
            Debug.Log($"Game started {msg.GameID}");
            
            // Handle Start of game
                // spawn players in different locations
                // give initial money
                // register what stats are for that character
                // cont.
                // define game rules (scriptable object dictionary ? )
        }

        private void OnGameEndMessageReceived(GameEndMessage msg)
        {
            Debug.Log($"Game ended {msg.GameID}");
            
            // Handle end of game
                // Notify game is ending
                // tally up scores
                // configure leaderboard stats 1st, 2nd, 3rd
                // give xp based on what was recorded during match to players account
                // cont.
        }
    }
}

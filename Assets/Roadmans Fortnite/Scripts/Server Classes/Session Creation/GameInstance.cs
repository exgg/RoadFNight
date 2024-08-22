using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Session_Creation
{
    public class GameInstance
    {
        public string GameId;
        private readonly List<NetworkConnection> _players = new List<NetworkConnection>();
        
        public GameInstance(string iD)
        {
            GameId = iD;
        }

        public void AddPlayer(NetworkConnection conn)
        {
            _players.Add(conn);

            GameStartMessage message = new GameStartMessage { GameID = GameId };
            conn.Send(message);
        }

        public bool RemovePlayer(NetworkConnection conn)
        {
            if (_players.Contains(conn))
            {
                _players.Remove(conn);
                return true;
            }

            return false;
        }

        public bool IsEmpty()
        {
            return _players.Count == 0;
        }
        
        public IEnumerator StartGame()
        {
            // change the scene for the server:
            NetworkManager.singleton.ServerChangeScene("GameScene");

            yield return new WaitForSeconds(1.0f);
            
            // add more game start logic here:
            
            Debug.Log("Game started with ID" + GameId);
        }

        public void EndGame()
        {
            foreach (var conn in _players)
            {
                // remove players
                GameEndMessage message = new GameEndMessage { GameID = GameId };
                conn.Send(message);
                
                // remove players later
            }
            // notify players the game is ending
            // end the game to clean up server, remove from list and return to lobby for requing
            Debug.Log("Game ended with ID:" + GameId);
        }
    }

    public struct GameStartMessage : NetworkMessage
    {
        public string GameID;
    }

    public struct GameEndMessage : NetworkMessage
    {
        public string GameID;
    }
}

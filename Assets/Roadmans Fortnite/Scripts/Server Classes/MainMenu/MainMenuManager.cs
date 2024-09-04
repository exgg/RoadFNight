using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roadmans_Fortnite.Scripts.Server_Classes.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        public void EnterLobby()
        {
            SceneManager.LoadScene("Lobby");
        }
    }
}

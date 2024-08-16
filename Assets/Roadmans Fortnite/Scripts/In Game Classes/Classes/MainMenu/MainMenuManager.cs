using System;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Security;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;


namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject loginPanel;
        public GameObject signUpPanel;
        public GameObject mainMenuPanel;
        public GameObject invalidUserPass;

        public Button rememberMeButton;
        
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TMP_InputField usernameField;

        public Text playerNameText;
        public Text playerLevelText;
        
        public Text playerStatsText; // this is incorrect, I will need more for this, a full new UI

        private bool _rememberMe;
        
        // Check if logged in
        // check player-prefs if logged in before,
        // if so use previous email and password and sign in to account

        private void Start()
        {
            if (PlayerPrefs.HasKey("PlayerUsername") && PlayerPrefs.HasKey("PlayerPassword"))
            {
                string savedUsername = PlayerPrefs.GetString("PlayerUsername");
                string savedPassword = PlayerPrefs.GetString("PlayerPassword");
                
                AttemptLogin(savedUsername, savedPassword);
            }
            else
            {
                ShowSignUpPanel();

                var (encryptedText, iv)  = Encryption.Encrypt("SamuelCluttonWork@Gmail.com");
                print(encryptedText);
                print(Encryption.Decrypt(encryptedText, iv));
            }
        }


        // if not logged in before, then instead prompt them to sign up,
        // Prompt signup / login - 2 different UI modules
        
        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true); 
            signUpPanel.SetActive(false);
            mainMenuPanel.SetActive(false);
        }

        private void ShowSignUpPanel()
        {
            signUpPanel.SetActive(true); // this will have a button that allows for the player to login
            loginPanel.SetActive(false);
            mainMenuPanel.SetActive(false);
        }

        private void ShowMainMenuPanel()
        {
            mainMenuPanel.SetActive(true);
            loginPanel.SetActive(false);
            signUpPanel.SetActive(false);
        }

        public void OnLoginButton()
        {
            string username = usernameField.text;
            string password = passwordField.text;

            RememberMe(username, password);
            
            AttemptLogin(username, password);
        }

        public void OnSignUpButton()
        {
            string email = emailField.text;
            string password = passwordField.text;
            string username = usernameField.text;
            
            // check dictionary of offencive words

            CreateAccount(email, password, username);
        }
        
        public void OnRetryLogin()
        {
            invalidUserPass.SetActive(false);
        }

        public void OnRememberMe()
        {
            _rememberMe = !_rememberMe;
            if(_rememberMe)
                rememberMeButton.image.color = Color.green;
            else
                rememberMeButton.image.color = Color.white;

        }
        
        // make an account

        private void AttemptLogin(string username, string password)
        {
            bool loginSuccess = ServerLogin(username, password);

            if (loginSuccess)
            {
                LoadAccountData(username);
                ShowMainMenuPanel();
                    // find all account stats for this player - maybe give them a unique ID rather than make an email?
            }
            else
            {
                invalidUserPass.SetActive(true);
            }
            
        }
        private void CreateAccount(string email, string password, string username)
        {
            RememberMe(username, password);
            
            // username
            // password
            
            // Write to file the account folder for this player, then push this to the server for saving
                // send email to server, encrypt then log as UniqueID.
                // send password to server and encrypt
                // send username to server, this does not need encryption
                // this is where all the players account data will be stored, in a file write
                // this needs to be as optimized as possible
        }

        private void RememberMe(string username, string password)
        {
            if (_rememberMe)
            {
                PlayerPrefs.SetString("PlayerUsername", username);
                PlayerPrefs.SetString("PlayerPassword", password);
            }
        }
        
        private void LoadAccountData(string email)
        {
            // setup UI and Menus
                //Account level
                //Account username
                //Account Stats
                //Kills
                //Money Earned
                //Other statistics
        }
        
        bool ServerLogin(string username, string password)
        {
            // check for server login
            return true; // simulation of login
        }

        bool ServerCreateAccount(string email, string password, string username)
        {
            // check for account creation success
            
            return true; // simulate true
        }

        string ServerGetPlayerName(string email)
        {
            return "PlayerName"; // this is a sim, but will eventually fetch the player name from server written files
        }
        
        int ServerGetPlayerLevel(string email)
        {
            return 1;  // Simulate fetching player level
        }

        string ServerGetPlayerStats(string email)
        {
            return "Kills: 10, Wins: 2";  // Simulate fetching player stats
        }

        public void OnJoinLobbyButton()
        {
            // attempt to join a lobby, if non are available create one
            
            NetworkManager.singleton.StartClient();
        }

        public void OnCreateLobby()
        {
            // create a lobby - private games ?
            NetworkManager.singleton.StartHost();
        }

        public void OnOptionsButton()
        {
            // sound - Music, SFX, etc volume.
            // ui
            // other settings
        }
    }
}

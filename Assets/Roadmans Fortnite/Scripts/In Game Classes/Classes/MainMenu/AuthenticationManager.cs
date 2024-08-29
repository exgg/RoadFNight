using System;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.Account_Management;
using Roadmans_Fortnite.Scripts.Server_Classes.Security;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;


namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.MainMenu
{
    public class AuthenticationManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject loginPanel;
        public GameObject signUpPanel;
        public GameObject mainMenuPanel;
        public GameObject invalidUserPass;

        public Button rememberMeButton;
        
        
        [Header("Sign-Up Input Fields")]
        public TMP_InputField sEmailField;
        public TMP_InputField sPasswordField;
        public TMP_InputField sUsernameField;

        [Header("Login Input Fields")] 
        public TMP_InputField lPasswordField;
        public TMP_InputField lUsernameField;
        
        
        [Space]
        public bool rememberMe;
        public AccountManager playerAccountManager;
        
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

        public void ShowEmailVerificationArea()
        {
            print("Show the area for please confirm email");
        }
        
        public void ShowSignUpPanel()
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
            string username = lUsernameField.text;
            string password = lPasswordField.text;
            
            AttemptLogin(username, password);
        }

        public void OnSignUpButton()
        {
            string email = sEmailField.text;
            string password = sPasswordField.text;
            string username = sUsernameField.text;
            
            // check dictionary of offencive words
            
            CreateAccount(email, password, username);
        }
        
        public void OnRetryLogin()
        {
            invalidUserPass.SetActive(false);
        }

        public void OnRememberMe()
        {
            rememberMe = !rememberMe;
            rememberMeButton.image.color = rememberMe ? Color.green : Color.white;
        }
        
        // make an account

        private void AttemptLogin(string username, string password)
        {
            playerAccountManager.AttemptLogin(username, password);
        }
        private void CreateAccount(string email, string password,  string username)
        {
            playerAccountManager.CreateAccount(email, password, username);
        }

        public void RememberMe(string username, string password)
        {
            if (rememberMe)
            {
                PlayerPrefs.SetString("PlayerUsername", username);
                PlayerPrefs.SetString("PlayerPassword", password);
            }
        }

        public void RememberID(int id)
        {
            if (rememberMe)
            {
                PlayerPrefs.SetInt("PlayerId", id);
            }
        }
    }
}

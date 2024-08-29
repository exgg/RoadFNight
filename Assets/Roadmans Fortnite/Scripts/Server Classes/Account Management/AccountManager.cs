using System;
using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using Mirror;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.MainMenu;
using Roadmans_Fortnite.Scripts.Server_Classes.PulledData;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Account_Management
{
    public class AccountManager : MonoBehaviour
    {
        private string apiUrl = "http://localhost:5226/api";
        
        // handle account creation


        public PulledAccountData setupAccountData;

        private AuthenticationManager _authenticationManager;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _authenticationManager = FindObjectOfType<AuthenticationManager>();
        }

        #region Account Creation
        
        public void CreateAccount(string email, string password,  string username)
        {
            StartCoroutine(CheckUsernameAndCreateAccount(email,password, username));
        }

        /// <summary>
        /// This checks whether the email and the username is already in use, since the username is aimed to be consistent and accessible for login
        /// we require the username to be unique. Once that request has been approved it will begin to poll for whether or not the account email
        /// has been verified
        /// </summary>
        /// <param name="pEmail">Email input field</param>
        /// <param name="pPassword">Password Input Field</param>
        /// <param name="pUsername">Username Input Field</param>
        /// <returns></returns>
        private IEnumerator CheckUsernameAndCreateAccount(string pEmail, string pPassword, string pUsername)
        {
            TempAccountData newAccountData = new TempAccountData
            {
                Username = pUsername,
                TempEmailHold = pEmail,  // This will map to TempEmailHold
                TempPasswordHold = pPassword  // This will map to TempPasswordHold
            };

            string json = JsonUtility.ToJson(newAccountData);
            Debug.Log("Sending JSON: " + json); 

            UnityWebRequest request = new UnityWebRequest($"{apiUrl}/EmailConfirmation/InitialAccountCreation", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("An error occurred: " + request.error + " - Response: " + request.downloadHandler.text);
                if (request.responseCode == 409)
                {
                    Debug.LogError("Username already taken.");
                }
            }
            else
            {
                Debug.Log("Account created successfully. Check your email to confirm your account.");
                StartCoroutine(PollEmailConfirmation(pUsername));
            }
        }
        
        /// <summary>
        /// Once the email and username has been checked that they are available, then it will search for when the email has been verified, once the email has been verified
        /// it will then allow the player to be instantly logged in, pushing for a fetch of the ID to log for later use
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private IEnumerator PollEmailConfirmation(string username)
        {
            _authenticationManager.ShowEmailVerificationArea();

            int maxRetries = 60; // e.g., 60 retries for 5 minutes
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                string encodedUsername = UnityWebRequest.EscapeURL(username);
                string requestUrl = $"{apiUrl}/EmailConfirmation/IsEmailConfirmed/{encodedUsername}";
                Debug.Log("Polling URL: " + requestUrl); // Log the polling URL

                UnityWebRequest request = UnityWebRequest.Get(requestUrl);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error checking email confirmation: " + request.error);
                    yield break;
                }

                if (request.responseCode == 200)
                {
                    Debug.Log("Server Response: " + request.downloadHandler.text); // Log the server response
                    bool isEmailConfirmed;
                    if (bool.TryParse(request.downloadHandler.text, out isEmailConfirmed) && isEmailConfirmed)
                    {
                        Debug.Log("Email has been confirmed!");
                        StartCoroutine(FetchAccountID(username));
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError($"Unexpected response code {request.responseCode}: {request.downloadHandler.text}");
                }

                yield return new WaitForSeconds(5);
                currentRetry++;
            }

            Debug.LogWarning("Max retries reached. Stopping email confirmation polling.");
        }

        #endregion

        #region Speed Optimization
        
        /// <summary>
        /// Fetches the account ID from the SQL server in order to produce a less expensive search once logged
        /// this will allow the SQL server to only require an Int search using the key within the database
        /// this is logged into the setupAccountData for the Account manager. This needs to later be logged within player prefs
        /// for instant use.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private IEnumerator FetchAccountID(string username)
        {
            string requestUrl = $"{apiUrl}/AccountData/GetAccountIdFromUsername/{username}";
            UnityWebRequest request = UnityWebRequest.Get(requestUrl);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching account ID : {request.error}");
            }

            if (request.responseCode == 200)
            {
                int accountId = int.Parse(request.downloadHandler.text);
                
                Debug.Log($"Fetched account ID : {accountId}");
                setupAccountData.id = accountId; // log the account ID for less expensive SQL search

                yield return StartCoroutine(LoadPlayerData(accountId));
            }

            else
            {
                Debug.LogError($"Unexpected response code {request.responseCode} : {request.downloadHandler.text}");
            }
        }

        #endregion
        
        #region Data Loading
        
        /// <summary>
        /// This will fetch the player data to log it to the players account manager, this will eventually lead to use being able to
        /// write the data to the SQL server at a later data, allowing for ease of access with database manipulation
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        private IEnumerator LoadPlayerData(int accountId)
        {
            string requestUrl = $"{apiUrl}/PlayerData/{accountId}/GetPlayerData";
            UnityWebRequest request = UnityWebRequest.Get(requestUrl);

            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching PlayerData : {request.error}");
            }

            if (request.responseCode == 200)
            {
                PulledPlayerData playerData = JsonUtility.FromJson<PulledPlayerData>(request.downloadHandler.text);

                setupAccountData.playerData = playerData;
                
                // once done show full main menu

                Debug.Log("All is complete display front end");
            }

            else
            {
                Debug.LogError($"Unexpected response code {request.responseCode} : {request.downloadHandler.text}");
            }
        }

        #endregion
        
        #region Login

        public void AttemptLogin(string username, string password)
        {
            StartCoroutine(CheckAuthentication(username, password));
        }
        
        /// <summary>
        /// This will check whether the input email and password are correct. It will lookup the username first, to find that
        /// account, encrypt the password with the same IV as is written for that username, if they are identical it will push the player
        /// to login, once they are logged in then it will grab the player data and load it into the prefs. This will need to be
        /// setup for the remember me. Once the remember me is toggled this will all be done automatically, since the password, ID and username will all be logged to
        /// player prefs. This will then sort into the main menu (thinking about it I am best making a seperate scene known as main menu, which is not this scene as this is an authentication scene)
        /// </summary>
        /// <param name="pUsername"></param>
        /// <param name="pPassword"></param>
        /// <returns></returns>
        private IEnumerator CheckAuthentication(string pUsername, string pPassword)
        {
            string requestUrl = $"{apiUrl}/Login/CheckAuthentication";

            var loginRequest = new AuthenticateRequest
            {
                Username = pUsername,
                Password = pPassword
            };

            string json = JsonUtility.ToJson(loginRequest);

            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching PlayerData : {request.error}");
            }
            else if (request.responseCode == 200)
            {
                Debug.Log($"Login successful! Response : {request.downloadHandler.text}");

                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                
                OnLoginSuccess(response);
            }

            else
            {
                Debug.LogError($"Login failed with status code of: {request.responseCode}. Response : {request.downloadHandler.text}");
            }
        }

        private void OnLoginSuccess(LoginResponse loginResponse)
        {
            Debug.Log($"Userid : {loginResponse.Id}, Username {loginResponse.Username}");

            setupAccountData.id = loginResponse.Id;
            setupAccountData.username = loginResponse.Username;

            StartCoroutine(LoadPlayerData(setupAccountData.id)); // use the new found ID to gather player data and load it
        }
        
        #endregion
     
        
        // checks if username contains offencive wording
        private bool CheckUsername(string username)
        {
            return false;
        }
        
        
    }
}

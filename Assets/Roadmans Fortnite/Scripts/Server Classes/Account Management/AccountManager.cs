using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using Mirror;
using Roadmans_Fortnite.Scripts.Server_Classes.PulledData;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Account_Management
{
    public class AccountManager : MonoBehaviour
    {
        private string apiUrl = "http://localhost:5226/api";
        
        // handle account creation

        public void CreateAccount(string email, string password,  string username)
        {
            StartCoroutine(CheckUsernameAndCreateAccount(email,password, username));
        }
        
        private IEnumerator CheckUsernameAndCreateAccount(string pEmail, string pPassword, string pUsername)
        {
            PulledAccountData newAccountData = new PulledAccountData
            {
                username = pUsername,
                email = pEmail,
                password = pPassword,
                
                
                playerData = new PulledPlayerData
                {
                    level = 0,
                    wins = 0,
                    kills = 0,
                    deaths = 0,
                    moneyEarned = 0,
                    accuracy = 1.0f
                }
            };

            string json = JsonUtility.ToJson(newAccountData);
            UnityWebRequest request = new UnityWebRequest($"{apiUrl}/AccountData/CreateAccount", "Post");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error Creating the Account");
                Debug.LogError("Response: " + request.downloadHandler.text);
                Debug.LogError("Status Code: " + request.responseCode);
                if (request.responseCode == 409)
                {
                   // The username already exists 
                   Debug.LogError("The Email Already Exists");
                }
                else
                {
                    // handle or display other possible errors
                }
            }
            else
            {
                Debug.Log("The Account has been created successfully");
                Debug.Log("Response: " + request.downloadHandler.text);
            }
        }
        
        private IEnumerator GetPasswordFromUsername()
        {
            yield return new WaitForSeconds(1);
        }
        
        // checks if username contains offencive wording
        private bool CheckUsername(string username)
        {
            return false;
        }
        
        
    }
}

using UnityEngine;
using System.IO;
using Mirror;

namespace Roadmans_Fortnite.Scripts.Server_Classes.Account_Management
{
    public class AccountManager : NetworkBehaviour
    {
        // handle account creation

        [Command]
        public void CmdCreateAccount(string encryptedEmail, string emailIv, string encryptedPassword, string passwordIv, string username)
        {
            if (CheckUsernameExists(username))
            {
                TargetUsernameAlreadyExists(connectionToClient);
            }
            else
            {
                SaveAccountDataToFile(encryptedEmail, emailIv, encryptedPassword, passwordIv, username);
                TargetUsernameCreatedSuccessfully(connectionToClient);
            }
        }
        
        // checks if username contains offencive wording
        private bool CheckUsername(string username)
        {
            return false;
        }
        
        // checks to see if username is available
        private bool CheckUsernameExists(string username)
        {
            string userDirectory = Path.Combine("ServerData", username);
            return Directory.Exists(userDirectory);
        }
    
        // once approved save the data to the file for the server/ this may need adjusting once we have virtual servers
        private void SaveAccountDataToFile(string encryptedEmail, string emailIv, string encryptedPassword, string passwordIv, string username)
        {
            
        }

        // notify client that the name already exists
        [TargetRpc]
        private void TargetUsernameAlreadyExists(NetworkConnection target)
        {
            
        }
        
        // notify the client with a welcome etc
        [TargetRpc]
        private void TargetUsernameCreatedSuccessfully(NetworkConnection target)
        {
            
        }
    }
}

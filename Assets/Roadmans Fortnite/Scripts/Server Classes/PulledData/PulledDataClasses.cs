using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.Server_Classes.PulledData
{
    [System.Serializable]
    public class PulledAccountData
    {
        public int id;
        
        public string username;
        public string password;

        public PulledPlayerData playerData = new PulledPlayerData();
    }

    [System.Serializable]
    public class PulledPlayerData
    {
        public int id = 0;
        public int level = 0;
        public int exp = 0;
        public int wins = 0;
        public int kills = 0;
        public int deaths = 0;
        public float kd = 0;
        public int moneyEarned = 0;
        public float shotsFired = 0;
        public float accuracy = 1;
    }

    [System.Serializable]
    public class TempAccountData
    {
        public int id;
        public string Username;
        public string TempEmailHold;  // Use the exact field names expected by the server
        public string TempPasswordHold;  // Use the exact field names expected by the server
    }

    [System.Serializable]
    public class AuthenticateRequest
    {
        public string Username;
        public string Password;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public int id;
        public string username;
    }
}

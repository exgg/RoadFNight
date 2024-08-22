using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.PulledData
{
    [System.Serializable]
    public class PulledAccountData
    {
        public int id;
        public string username;

        public string email;
        public string password;

        public PulledPlayerData playerData;
    }

    [System.Serializable]
    public class PulledPlayerData
    {
        public int level;
        
        public int wins;
        public int kills;
        public int deaths;
        
        public int moneyEarned;
        
        public int shotsFired;
        public float accuracy;
    }
}

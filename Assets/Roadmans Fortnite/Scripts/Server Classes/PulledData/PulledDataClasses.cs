using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Server_Classes.PulledData
{
    [System.Serializable]
    public class PulledAccountData
    {
        public int id;
        
        public string username;
        public string password;

        public PulledPlayerData playerData;
    }

    [System.Serializable]
    public class PulledPlayerData
    {
        public int Id { get; set; }

        public int Level { get; set; } = 0;
        public int Exp { get; set; } = 0;
    
        public int Wins { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public float Kd { get; set; } = 0;
    
        public int MoneyEarned { get; set; } = 0;

        public float ShotsFired { get; set; } = 0;
        public float Accuracy { get; set; } = 1;
    }

    [System.Serializable]
    public class TempAccountData
    {
        public int id;
        public string Username;
        public string TempEmailHold;  // Use the exact field names expected by the server
        public string TempPasswordHold;  // Use the exact field names expected by the server
    }
}

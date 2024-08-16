using Roadmans_Fortnite.Scripts.Classes.Stats;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Data
{
    public class LoadPlayerData
    {
        // this is the initial phase, I will then need to move this server side
        // this will accommodate having possibility of cheaters
        
        public void LoadPlayerCharacterStats(PlayableCharacterStats playerStats)
        {
            // player health, name, username
            // move speed, level, etc
        }



        public void LoadPlayerInventory()
        {
            // bullets
            // items
            // stock
            // weapons unlocked

        }

        public void LoadPlayerPurchases()
        {
            // character purchases
            // skin purchases
            // Premium currency purchases
            // etc
        }
    }

    public class SavePlayerData
    {
        // this is the initial phase, I will then need to move this server side
        // this will accommodate having possibility of cheaters
        
        public void SavePlayerCharacterStats(PlayableCharacterStats playerStats)
        {
            // player health, name, username
            // move speed, level, etc
        }

        public void SaveAccountData()
        {
            //Account level
            //Account Stats
            //Kills
            //Money Earned
            //Other statistics
        }

        public void SavePlayerInventory()
        {
            // bullets
            // items
            // stock
            // weapons unlocked

        }

        public void SavePlayerPurchases()
        {
            // character purchases
            // skin purchases
            // Premium currency purchases
            // etc
        }
    }
}

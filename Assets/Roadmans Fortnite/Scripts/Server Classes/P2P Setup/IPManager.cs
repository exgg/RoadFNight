using UnityEngine;
using System.Net;
using System.Net.Sockets;


namespace Roadmans_Fortnite.Scripts.Server_Classes.P2P_Setup
{
    public class IPManager : MonoBehaviour
    {
        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new System.Exception("No network adaptive with an IPv4 address in the system");
        }
    }
}

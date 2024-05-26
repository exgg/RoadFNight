using System;
using UnityEngine;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace MasterServer {

	public enum AuthRequestType : byte {
		AccountCreation,
		Authorization
	}

	public class AuthRequestPacket {

		public int ClientVersion { get; set; }
		public AuthRequestType Type { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string EncryptedPassword { get; set; }
	}

	public class AuthResponsePacket {

		public byte Code { get; set; }
		public string Token { get; set; }
	}

	public class AccountDataRequestPacket {

		public string Token { get; set; }
	}

	public class AccountDataResponsePacket {

		public string Token { get; set; }
		public int Id { get; set; }
		public string Username { get; set; }
		public byte Status { get; set; }
		public int Funds { get; set; }
		public bool OwnsProperty { get; set; }
		public int Nutrition { get; set; }
        public int ExperiencePoints { get; set; }

    }

	#region Server Info & List

	[Serializable]
	public class InstanceInfo {

		public string uniqueName;
		public int numberOfPlayers;
		public int ping;
	}

	public class GetInstancesPacket { } // From Client

	public class InstancesPacket { // To Client

		public string JSON { get; set; }
	}

	#endregion

	#region Connection Info

	public class GetConnectionInfoPacket { // From Client


		public string InstanceUniqueName { get; set; }
	}

	public class ConnectionInfoPacket { // To Client

		public string Address { get; set; }
	}

	#endregion

	public class GetPlacedObjectsPacket {

		public int OwnerId { get; set; }
	}

	public class PlacedObjectsPacket {

		public int OwnerId { get; set; }
		public string JSON { get; set; }
	}

	public class SavePlacedObjectsPacket {

		public int OwnerId { get; set; }
		public string JSON { get; set; }
	}

	public class GetInventoryPacket {

		public int OwnerId { get; set; }
	}

	public class InventoryPacket {

		public int OwnerId { get; set; }
		public string JSON { get; set; }
	}

	public class SaveInventoryPacket {

		public int OwnerId { get; set; }
		public string JSON { get; set; }
	}
}

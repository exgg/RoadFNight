#if UNITY_SERVER || UNITY_EDITOR // (Server)
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SQLite;

namespace MasterServer {

	[System.Serializable] // ?
	[Table("Accounts")]
	public class AccountData {
		[PrimaryKey, AutoIncrement]
		[Column("id")]
		public int Id { get; set; }
		[Column("email"), Collation("NOCASE")]
		public string Email { get; set; }
		[Column("encrypted_password")]
		public string EncryptedPassword { get; set; }
		[Column("username"), Collation("NOCASE")]
		public string Username { get; set; }
		[Column("status")]
		public byte Status { get; set; }
		[Column("online")]
		public bool Online { get; set; }
		[Column("funds")]
		public int Funds { get; set; }
		[Column("owns_property")]
		public bool OwnsProperty { get; set; }
		[Column("nutrition")]
		public int Nutrition { get; set; }
        [Column("experience_points")]
        public int ExperiencePoints { get; set; }
    }

	[System.Serializable] // ?
	[Table("PlacedObjects")]
	public class PlacedObjectData {
		[PrimaryKey, AutoIncrement]
		[Column("id")]
		public int Id { get; set; }
		[Column("owner_id")]
		public int OwnerId { get; set; }
		[Column("unique_name")]
		public string UniqueName { get; set; }
		[Column("x")]
		public float X { get; set; }
		[Column("y")]
		public float Y { get; set; }
		[Column("z")]
		public float Z { get; set; }
		[Column("rot_x")]
		public float RotX { get; set; }
		[Column("rot_y")]
		public float RotY { get; set; }
		[Column("rot_z")]
		public float RotZ { get; set; }
		[Column("rot_w")]
		public float RotW { get; set; }
	}

	[System.Serializable] // ?
	[Table("Inventory")]
	public class InventoryData {

		[PrimaryKey, AutoIncrement]
		[Column("id")]
		public int Id { get; set; }
		[Column("owner_id")]
		public int OwnerId { get; set; }

		[Column("hash")]
		public int Hash { get; set; }

		[Column("amount")]
		public int Amount { get; set; }

		[Column("shelf_life")]
		public float ShelfLife { get; set; }
	}

	public static class Database {

		private static SQLiteConnection _connection;

		private const string _DatabaseFileName = "db.sqlite";

		public static void CloseConnection() {
			if (_connection != null) {
				_connection.Close();
				_connection = null;
			}
		}

		public static void OpenConnection() {
			CloseConnection(); // ?

#if UNITY_EDITOR
			string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, _DatabaseFileName);
#else
			string path = Path.Combine(Application.dataPath, _DatabaseFileName);
#endif

			_connection = new SQLiteConnection(path);

			_connection.CreateTable<AccountData>();
			_connection.CreateTable<PlacedObjectData>();
			_connection.CreateTable<InventoryData>();
		}

		public static AccountData CreateAccount(string email, string encryptedPassword, string username) {
			if (_connection.FindWithQuery<AccountData>("SELECT 1 FROM Accounts WHERE email=? OR username=?", email, username) != null) {
				return null;
			}

            AccountData result = new AccountData {
                Email = email,
                EncryptedPassword = encryptedPassword,
                Username = username,
                Status = 0,
                Online = true, // ?
                Funds = 1000,
                OwnsProperty = false,
                Nutrition = 50,
                ExperiencePoints = 100
            };

			_ = _connection.Insert(result);

			return result;
		}

		public static AccountData GetAccountData(string email, string encryptedPassword) {
			return _connection.FindWithQuery<AccountData>("SELECT * FROM Accounts WHERE email=? AND encrypted_password=?", email, encryptedPassword);
		}

		public static void UpdateAccountData(int id, int funds, bool ownsProperty, int nutrition, int experiencePoints) {
			_ = _connection.Execute("UPDATE Accounts SET funds=?, owns_property=?, nutrition=?, experience_points=? WHERE id=?", funds, ownsProperty, nutrition, experiencePoints, id);
		}

		public static PlacedObjectData[] GetPlacedObjects(int ownerId) {
			return _connection.Query<PlacedObjectData>("SELECT * FROM PlacedObjects WHERE owner_id=?", ownerId)?.ToArray();
		}

		public static void DeletePlacedObjects(int ownerId) {
			_ = _connection.Execute("DELETE FROM PlacedObjects WHERE owner_id=?", ownerId);
		}

		public static void SavePlacedObjects(PlacedObjectData[] placedObjects) {
			_connection.InsertAll(placedObjects);
		}

		public static InventoryData[] GetInventory(int ownerId) {
			return _connection.Query<InventoryData>("SELECT * FROM Inventory WHERE owner_id=?", ownerId)?.ToArray();
		}

		public static void DeleteInventory(int ownerId) {
			_ = _connection.Execute("DELETE FROM Inventory WHERE owner_id=?", ownerId);
		}

		public static void SaveInventory(InventoryData[] inventoryData) {
			_connection.InsertAll(inventoryData);
		}
	}
}
#endif

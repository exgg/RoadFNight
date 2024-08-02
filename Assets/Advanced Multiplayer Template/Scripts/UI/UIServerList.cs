using UnityEngine;
using MasterServer;
using TMPro;
using System.Collections;

public class UIServerList : MonoBehaviour {

#if !UNITY_SERVER || UNITY_EDITOR // (Client)
	[Header("Content")]
	[SerializeField] private GameObject _content;

	[Header("Components")]
	[SerializeField] private TextMeshProUGUI _headerText;
	[SerializeField] private UIButton _refreshButton;
	[SerializeField] private UIButton _createServerButton;
    [SerializeField] private UIButton _quickJoinButton;
    [SerializeField] private UICategory _uICategory;
	[SerializeField] private UIButton _connectButton;
	[SerializeField] private TextMeshProUGUI _serverNameText;

	private void Connect(string address) {
		Mirror.NetworkManager.singleton.networkAddress = address;
		Mirror.NetworkManager.singleton.StartClient();
	}

	private void Awake() {
		_content.SetActive(false);
		MSClient.OnStateChanged += () => {
			if (MSClient.State == MSClient.NetworkState.Lobby) {
				_content.SetActive(true);
				_refreshButton.onPressed.Invoke();
			}
			else {
				_content.SetActive(false);
			}
		};

		_connectButton.onPressed = () => {
			MSManager.SendPacket(new GetConnectionInfoPacket { InstanceUniqueName = CustomNetAuthenticator.local_instanceName });
		};

		_uICategory.onOptionSelect = instanceUniqueName => {
			CustomNetAuthenticator.local_instanceName = instanceUniqueName;
			_serverNameText.text = "<color=#aaa>Server:</color> " + instanceUniqueName;
		};

		MSClient.OnInstancesAction += () => {
			_headerText.text = "Global Server List <color=#aaa>(Debug UI)</color>                                <color=#aaa>Server Count:</color> " + MSClient.last_instances.Length;

			_uICategory.ClearOptions();
			for (int i = 0; i < MSClient.last_instances.Length; i++) {
				//_uICategory.AddOption(MSClient.last_instances[i]);
			}
		};

		_createServerButton.onPressed = () => {
			UICreateServer.instance.Show((serverName) => {
				CustomNetAuthenticator.local_instanceName = serverName;
				_serverNameText.text = "<color=#aaa>Server:</color> " + serverName;
				//_uICategory.SelectOption(null);
			});
		};

        _quickJoinButton.onPressed = () =>{
            bool isServerFound = false;

            for (int i = 0; i < MSClient.last_instances.Length; i++){
                if (MSClient.last_instances[i].numberOfPlayers < 16){ // Search if a server with less than 16 players is available
                    // Server with less than 16 players found
                    CustomNetAuthenticator.local_instanceName = MSClient.last_instances[i].uniqueName;
                    _serverNameText.text = "<color=#aaa>Server:</color> " + MSClient.last_instances[i].uniqueName;
                    //_uICategory.SelectOption(null);
                    MSManager.SendPacket(new GetConnectionInfoPacket { InstanceUniqueName = CustomNetAuthenticator.local_instanceName }); // Enter server
                    isServerFound = true;
                }
            }
            if(!isServerFound){
                // All available servers are full or no server has been created yet, so we need to create a server
                string _serverName = "";
                bool isServerNameTaken = false;

                StartCoroutine(CreateServerProcedure());

                IEnumerator CreateServerProcedure(){
                    isServerNameTaken = false;

                    _serverName = "Server" + Random.Range(1, 100); // Give the server a randomly generated name

                    // Check if the randomly generated server name matches the name of an already created server
                    for (int i = 0; i < MSClient.last_instances.Length; i++){
                        if (MSClient.last_instances[i].uniqueName == _serverName){
                            // Server name is already used

                            isServerNameTaken = true;
                        }
                    }

                    yield return new WaitUntil(() => !isServerNameTaken); // Waits until a server name is generated that does not matches the name of an already created server
                }

                CustomNetAuthenticator.local_instanceName = _serverName;
                _serverNameText.text = "<color=#aaa>Server:</color> " + _serverName;
				// _uICategory.SelectOption(null);

                StartCoroutine(EnterServerProcedure());

                IEnumerator EnterServerProcedure(){
                    yield return new WaitForEndOfFrame();

                    MSManager.SendPacket(new GetConnectionInfoPacket { InstanceUniqueName = CustomNetAuthenticator.local_instanceName }); // Enter the server
                }
            }
        };

        MSClient.OnConnectionInfoAction += () => {
			if (string.IsNullOrEmpty(MSClient.lastConnectionInfoPacket.Address) || MSClient.lastConnectionInfoPacket.Address == "full") {
				UIPopup.instance.Show("No servers available");
				return;
			}
			Connect(MSClient.lastConnectionInfoPacket.Address);
		};

		_refreshButton.onPressed = () => {
			MSManager.SendPacket(new GetInstancesPacket());
			UIPopup.instance.Show("The list of servers has been updated, keep in mind that server information may update more than 10 seconds after the change to increase the bandwidth of the master server");
		};
	}
#endif
}

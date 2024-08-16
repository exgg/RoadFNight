using UnityEngine;

public class UIInGame : MonoBehaviour {

	[SerializeField] private GameObject _content;

	private void Update() {
		_content.SetActive(NetPlayer.LocalNetPlayer != null);
	}
}

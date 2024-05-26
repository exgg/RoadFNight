using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIBuildInfoText : MonoBehaviour {

	private void Awake() {
		GetComponent<TextMeshProUGUI>().text = "Advanced Multiplayer Game Kit (v" + Application.version + ')';
	}
}

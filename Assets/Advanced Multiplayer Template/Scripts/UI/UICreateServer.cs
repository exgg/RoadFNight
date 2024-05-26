using UnityEngine;
using TMPro;

public class UICreateServer : UIBaseDialog {

	public static UICreateServer instance;
	public UICreateServer() {
		if (instance == null) {
			instance = this;
		}
	}

	[SerializeField] private TMP_InputField _serverNameIF;

	private System.Action<string> onResult;
	public void Show(System.Action<string> onResult) {
		base.Show();
		this.onResult = onResult;
	}

	public override void Hide() {
		base.Hide();
		onResult.Invoke(_serverNameIF.text);
		_serverNameIF.text = string.Empty;
	}
}

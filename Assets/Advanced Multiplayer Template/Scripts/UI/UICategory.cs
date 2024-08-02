using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UICategory : MonoBehaviour {

	[SerializeField] private GameObject _categoryOptionPrefab;
	[SerializeField] private GameObject _categoryItemPrefab;
	[SerializeField] private Transform _content;
	[SerializeField] private Transform _itemContent;
	[SerializeField] private Color _color;
	[SerializeField] private Color _selectedColor;
	public TextMeshProUGUI headerText;
	public UIButton returnButton;

	private struct Option {

		public GameObject gO;
		public bool item;
	}

	private Dictionary<string, Option> _options = new Dictionary<string, Option>();

	public System.Action<string> onOptionSelect;

	public void ClearOptions() {
		foreach (Option option in _options.Values) {
			Destroy(option.gO);
		}
		_options.Clear();
	}
	

	

	private static string EmptySpace(int length) {
		string result = string.Empty;
		for (int i = 0; i < length; i++) {
			result += ' ';
		}
		return result;
	}

	
}

using System;
using System.Collections;
using System.Collections.Generic;
using BSystem;
using TMPro;
using UnityEngine;

public class BSystemUI : MonoBehaviour {

	public static BSystemUI Instance { get; private set; }

	private void Awake() {
		if (Instance == null) {
			Instance = this;
			return;
		}
		throw new UnityException("Instance");
	}

	private static string _currentCategoryPath;

	private void LoadDefault() {
		_uICategory.ClearOptions();
		_currentCategoryPath = string.Empty;
		for (int i = 0; i < PlaceableSO.globalCategories.Count; i++) {
			_uICategory.AddOption(PlaceableSO.globalCategories[i].name, null);
		}
	}

	private void PathReturn() {
		if (!_currentCategoryPath.Contains("/")) {
			LoadDefault();
			return;
		}
		while (!_currentCategoryPath.EndsWith("/")) {
			_currentCategoryPath = _currentCategoryPath.Remove(_currentCategoryPath.Length - 1, 1);
		}
		_currentCategoryPath = _currentCategoryPath.Remove(_currentCategoryPath.Length - 1, 1);
		OnOptionSelect(string.Empty);
	}

	private void OnOptionSelect(string categoryName) {
		if (!string.IsNullOrEmpty(categoryName)) {
			_currentCategoryPath += (string.IsNullOrEmpty(_currentCategoryPath) ? string.Empty : "/") + categoryName;
			_uICategory.headerText.text = categoryName + " <color=#27ae60>(Category)</color>";
		}
		else {
			_uICategory.headerText.text = string.IsNullOrEmpty(_currentCategoryPath) ? "Choose Category" : (_currentCategoryPath + " <color=#27ae60>(Category)</color>");
		}
		PlaceableSO.Category category = PlaceableSO.GetCategory(_currentCategoryPath);
		_uICategory.ClearOptions();
		for (int i = 0; i < category.subcategories.Count; i++) {
			_uICategory.AddOption(category.subcategories[i].name, null);
		}
		for (int i = 0; i < category.placeableSOs.Count; i++) {
			_uICategory.AddOption(category.placeableSOs[i].uniqueName, category.placeableSOs[i]);
		}
	}

	private void OnItemSelect(PlaceableSO placeableSO) {
		BSystem.BSystem.currentPlaceableSO = placeableSO;
		_currentPlaceableSOUniqueNameText.text = placeableSO.uniqueName;
		BSystem.BSystem.OnCurrentPlaceableSOChangedAction.Invoke();
	}

	public void ResetUI() {
		((RectTransform)_uICategory.transform).anchoredPosition = _catPos;
	}

	private void Start() {
#if !UNITY_SERVER || UNITY_EDITOR
		_catPos = ((RectTransform)_uICategory.transform).anchoredPosition;

		_resetUIButton.onPressed = new Action(ResetUI);
		_clearPlaceableButton.onPressed = delegate () {
			BSystem.BSystem.currentPlaceableSO = null;
			_currentPlaceableSOUniqueNameText.text = "-";
			BSystem.BSystem.OnCurrentPlaceableSOChangedAction.Invoke();
		};

		_content.SetActive(false);

		_uICategory.onOptionSelect = OnOptionSelect;
		_uICategory.onItemSelect = OnItemSelect;
		_uICategory.returnButton.onPressed = PathReturn;
		LoadDefault();
#endif
	}

	private void Update() {
		if (Player.localPlayer == null) {
			return;
		}
		_accountInfoText.text = "Funds: " + Player.localPlayer.funds + '$';
	}

	public bool Active => _content.activeSelf;

	public void SetActive(bool value) {
		_content.SetActive(value);
	}

	[SerializeField]
	private GameObject _content;

	[SerializeField]
	private UICategory _uICategory;

	[SerializeField]
	private UIButton _clearPlaceableButton;

	[SerializeField]
	private TextMeshProUGUI _currentPlaceableSOUniqueNameText;

	[SerializeField]
	private TextMeshProUGUI _accountInfoText;

	private Vector2 _catPos;

	[SerializeField]
	private UIButton _resetUIButton;
}

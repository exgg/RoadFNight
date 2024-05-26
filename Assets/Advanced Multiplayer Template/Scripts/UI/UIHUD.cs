using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHUD : MonoBehaviour {

	[SerializeField] private Slider _nutritionSlider;
	[SerializeField] private TextMeshProUGUI _nutritionText;

    [SerializeField] private Slider _healthSlider;
    [SerializeField] private TextMeshProUGUI _healthText;

    private void Update() {
		if (Player.localPlayer == null) {
			return;
		}

		_nutritionSlider.value = Player.localPlayer.playerNutrition.value;
		_nutritionText.text = "Nutrition: " + Player.localPlayer.playerNutrition.value + "/100";

        _healthSlider.value = Health.localPlayer.currentHealth;
        _healthText.text = "Health: " + Health.localPlayer.currentHealth + "/100";
    }
}

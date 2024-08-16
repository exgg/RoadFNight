using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceManager : MonoBehaviour
{
    private float currentXP;
    private float targetXP = 100;
    private int currentLevel;
    [SerializeField] private GameObject playerLevelElement;

    [HideInInspector] public float lerpTimer;
    [HideInInspector] public float delayTimer;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentXPText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI currentLevelPlayerText;
    [SerializeField] private TextMeshProUGUI nextLevelText;
    [SerializeField] private Image backXPBar;
    [SerializeField] private Image frontXPBar;
    [Header("Multipliers")]
    [Range(1f, 300f)]
    public float additionMultiplier = 300;
    [Range(2f, 4f)]
    public float powerMultiplier = 2;
    [Range(7f, 14f)]
    public float divisionMultiplier = 7;
    [SerializeField] public GameObject ExperienceUI;

    private void Start()
    {
        frontXPBar.fillAmount = currentXP / targetXP;
        backXPBar.fillAmount = currentXP / targetXP;
        targetXP = CalculateRequiredXp();
        currentLevelText.text = currentLevel.ToString();
        currentLevelPlayerText.text = currentLevel.ToString();
        nextLevelText.text = "";
        int nextLevel = currentLevel += 1;
        nextLevelText.text = nextLevel.ToString();
    }

    private void Update()
    {
        UpdateXpUI();
        if (Input.GetKeyDown(KeyCode.M))
            GainExperienceFlatRate(20);
        if (currentXP > targetXP)
            LevelUp();
    }

    /// <summary>
    /// Formula to calculate how a player earns EXP, which will then turn into a
    /// Slider bar in order to give the player visual feedback on what level they are
    /// </summary>
    public void UpdateXpUI()
    {
        currentXP = GetComponent<NetPlayer>().experiencePoints; // expensive again given it is in the update

        float xpfraction = currentXP / targetXP;
        float FXP = frontXPBar.fillAmount;
        if(FXP < xpfraction)
        {
            delayTimer += Time.deltaTime;
            backXPBar.fillAmount = xpfraction;
            if(delayTimer > 3)
            {
                lerpTimer += Time.deltaTime;
                float percentComplete = lerpTimer / 4;
                frontXPBar.fillAmount = Mathf.Lerp(FXP, backXPBar.fillAmount, percentComplete);

            }
        }
        currentXPText.text = currentXP + " / " + targetXP;
    }

    /// <summary>
    /// Flat rate of how experience is earned once it has been earned, it will then proceed to reset the timer of the gradual
    /// progression of the player
    /// </summary>
    /// <param name="xpGained"></param>
    public void GainExperienceFlatRate(float xpGained)
    {
        currentXP += xpGained;
        lerpTimer = 0f;
        delayTimer = 0f;
    }

    /// <summary>
    /// Scalable experience which seems to be a way that we can use the template to generate XP at a greater rate.
    /// </summary>
    /// <param name="xpGained"></param>
    /// <param name="passedLevel"></param>
    public void GainExperienceScalable(float xpGained, int passedLevel)
    {
        if(passedLevel < currentLevel)
        {
            float multiplier = 1 + (currentLevel - passedLevel) * 0.1f;
            currentXP += xpGained * multiplier;
        }
        else
        {
            currentXP += xpGained;
        }
        lerpTimer = 0f;
        delayTimer = 0f;
    }

    /// <summary>
    /// Produces the ability to level up, which first calculates the required XP for the player to level up.
    /// This secondary method called seems to be exponential based on the formula behind it.
    /// Then will update the required UI and reset the bar and current XP to the start once more. Increasing the players level
    /// </summary>
    public void LevelUp()
    {
        currentLevel++;
        frontXPBar.fillAmount = 0f;
        backXPBar.fillAmount = 0f;
        currentXP = Mathf.RoundToInt(currentXP - targetXP);
        targetXP = CalculateRequiredXp();
        currentLevelText.text = currentLevel.ToString();
        currentLevelPlayerText.text = currentLevel.ToString();
        int nextLevel = currentLevel += 1;
        nextLevelText.text = nextLevel.ToString();
    }

    /// <summary>
    /// Formula to calculate how much xp is needed per level. This is exponential, we will need to figure out if this is something we want to include or
    /// we want a more linear transition into levels. Similar to CoD where it has a max XP needed.
    /// </summary>
    /// <returns></returns>
    private int CalculateRequiredXp()
    {
        int solveForRequiredXp = 0;
        for(int levelCycle = 1; levelCycle <= currentLevel; levelCycle++)
        {
            solveForRequiredXp += (int)Mathf.Floor(levelCycle + additionMultiplier * Mathf.Pow(powerMultiplier, levelCycle / divisionMultiplier));
        }
        return solveForRequiredXp / 4;
    }
}

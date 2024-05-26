using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("Loading Screen")]
    public LoadingScreenItem[] loadingScreenItems;
    public Image loadingScreenPicture;
    [Header("Loading Text")]
    public string[] loadingTexts;
    public TMPro.TMP_Text loadingText;
    [Header("Tips")]
    public TMPro.TMP_Text tipText;
    [Space]
    public GameObject loadingScreenContent;

    void Start()
    {
        loadingScreenContent.SetActive(true);
        LoadingScreenItem currentLoadingScreen = loadingScreenItems[Random.Range(0, loadingScreenItems.Length)];
        loadingScreenPicture.sprite = currentLoadingScreen.loadingScreenSprite;
        tipText.text = currentLoadingScreen.tip;

        StartCoroutine(Loading());
    }

    IEnumerator Loading()
    {
        yield return new WaitForSeconds(2);
        loadingText.text = loadingTexts[0];
        yield return new WaitForSeconds(2);
        loadingText.text = loadingTexts[1];
        yield return new WaitForSeconds(2);
        loadingText.text = loadingTexts[2];
        yield return new WaitForSeconds(2);
        loadingText.text = loadingTexts[3];
        yield return new WaitForSeconds(0.16f);
        Destroy(this.gameObject);
    }
}

[System.Serializable]
public class LoadingScreenItem
{
    public string Name;
    public Sprite loadingScreenSprite;
    [TextArea]
    public string tip;
}

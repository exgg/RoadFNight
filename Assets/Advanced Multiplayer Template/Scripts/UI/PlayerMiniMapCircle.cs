using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMiniMapCircle : MonoBehaviour
{
    public Image PlayerCicle;
    public Color PlayerCircleColor;
    public Color EnemyCircleColor;
    public GameObject MiniMap;

    private void Update()
    {
        SetCircleColor(Player.localPlayer != null);
    }

    public void SetCircleColor(bool isLocal)
    {
        if(isLocal)
        {
            MiniMap.SetActive(true);
            PlayerCicle.color = PlayerCircleColor;
            enabled = false;
            return;
        }
        else
            MiniMap.SetActive(false);
        PlayerCicle.color = EnemyCircleColor;
        if (transform.root.GetComponent<PlayerAI>().isSetAsAi == true)
            Destroy(gameObject);
        enabled = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RandomSpawn : MonoBehaviour
{
    private float Show = 0.0f;
    [Range(0, 1)]
    public float Possibility = 0.5f;
    // Start is called before the first frame update
    void OnEnable()
    {
        Show = Random.value;

      if (Show >= Possibility)
        {
            this.gameObject.SetActive(true);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
        

    }

}

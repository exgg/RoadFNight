using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudioTest : MonoBehaviour
{
    public AudioSource audioSource;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            audioSource.Play();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GunEcho : NetworkBehaviour
{
    public string shooter = "Deafault";

    public AudioSource audioSource;

    public AudioClip longEcho;
    public AudioClip shortEcho;

    public void StartEcho(int shotBullets, string shooterUsername)
    {
        shooter = shooterUsername;

        if (shotBullets > 25)
            audioSource.clip = longEcho;
        else
            audioSource.clip = shortEcho;
        if (NetworkClient.localPlayer.GetComponent<Player>().username == shooter)
        {
            return;
        }

        StartCoroutine(PlayEcho());
    }

    IEnumerator PlayEcho()
    {
        yield return new WaitForSeconds(2.5f);

        audioSource.Play();
    }
}

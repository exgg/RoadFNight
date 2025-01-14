using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Footsteps : NetworkBehaviour
{
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
    private CharacterController _controller;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// when the player character's walking or running animation steps on the ground
    /// </summary>
    /// <param name="animationEvent"></param>
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                //AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                PlayClipAt(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume, 1, 5);
            }
        }
    }

    /// <summary>
    /// when the player lands on the ground after falling
    /// </summary>
    /// <param name="animationEvent"></param>
    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            //AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            PlayClipAt(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume, 1, 5);
        }
    }

    /// <summary>
    /// plays footstep sound
    /// </summary>
    /// <param name="_clip">soundclip for footstep</param>
    /// <param name="_position">point where the footstep soundclip plays</param>
    /// <param name="_volume">volume of soundclip</param>
    /// <param name="_minDistance"></param>
    /// <param name="_maxDistance"></param>
    private void PlayClipAt(AudioClip _clip, Vector3 _position, float _volume, float _minDistance, float _maxDistance)
    {
        if (isServer)
            return;

        var tempGO = new GameObject("FootstepAudio");
        tempGO.transform.position = _position;
        var aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = _clip;
        aSource.volume = _volume;
        aSource.minDistance = _minDistance;
        aSource.maxDistance = _maxDistance;
        aSource.reverbZoneMix = 1;
        aSource.spatialBlend = 1;
        aSource.Play();
        Destroy(tempGO, _clip.length);
        return;
    }
}

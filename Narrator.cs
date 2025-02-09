using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Narrator : MonoBehaviour
{
    public event Action<string> OnNarration;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private string voice;

    private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        SoccerIntegration.Instance.OnEmit += Narrate;
    }

    private bool Play(string text)
    {
        if (audioSource.isPlaying)
            return true;
        var clip = clips.GetValueOrDefault(text);
        if (clip == null)
            return false;
        OnNarration?.Invoke(text);
        audioSource.PlayOneShot(clip);
        return true;
    }

    private async Task FetchClip(string text)
    {
        var clip = await TextToSpeechGenerator.GetClipFromGoogle(text, voice);
        clips.Add(text, clip);
    }

    private IEnumerator FetchClipAndPlay(string text)
    {
        yield return FetchClip(text);
        Play(text);
    }

    public void Narrate(string text)
    {
        if (Play(text))
            return;
        StartCoroutine(FetchClipAndPlay(text));
    }
}
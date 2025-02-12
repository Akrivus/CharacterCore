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
        SoccerGameSource.Instance.OnEmit += Narrate;
    }

    private bool Play(string text)
    {
        if (audioSource.isPlaying)
            return true;
        if (!clips.ContainsKey(text))
            return false;
        OnNarration?.Invoke(text);
        audioSource.PlayOneShot(clips[text]);
        return true;
    }

    private async Task FetchClip(string text)
    {
        if (clips.ContainsKey(text))
            return;
        var clip = await TextToSpeechGenerator.GetClipFromGoogle(text, voice);
        clips.Add(text, clip);
    }

    private IEnumerator FetchClipAndPlay(string text)
    {
        yield return FetchClip(text).AsCoroutine();
        Play(text);
    }

    public void Narrate(string text)
    {
        if (Play(text))
            return;
        StartCoroutine(FetchClipAndPlay(text));
    }
}
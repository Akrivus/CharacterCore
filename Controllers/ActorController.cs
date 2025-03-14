﻿using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ActorController : MonoBehaviour
{
    public static float GlobalSpeakingRate = 1.0f;

    public event Action<ChatNode> OnActivation;
    public event Action<ActorController> OnActorUpdate;
    public event Action<Sentiment> OnSentimentUpdate;

    public float TotalVolume => voice.GetAmplitude() + sound.GetAmplitude();
    public float VoiceVolume => voice.GetAmplitude();
    public bool IsTalking => voice.isPlaying && VoiceVolume > 0.0f;

    public AudioSource Voice => voice;
    public AudioSource Sound => sound;
    public Camera Camera { get; set; }

    public Color TextColor;

    [SerializeField]
    private AudioSource voice;

    [SerializeField]
    private AudioSource sound;

    [SerializeField]
    private float delay = 1.2f;

    private float _totalTalkTime;

    public ActorContext Context
    {
        get => _context;
        set
        {
            _context = value;
            OnUpdateActorCallbacks(value);
        }
    }

    public Actor Actor => Context.Reference;

    public Sentiment Sentiment
    {
        get => _sentiment ?? Actor.DefaultSentiment;
        set
        {
            _sentiment = value;
            OnUpdateSentimentCallbacks(value);
        }
    }
    
    private Sentiment _sentiment;
    private ActorContext _context;

    private ISubActor[] sub_Actor;
    private ISubSentiment[] sub_Sentiment;
    private ISubNode[] sub_Nodes;
    private ISubChats[] sub_Chats;
    private ISubExits[] sub_Exits;

    private void Awake()
    {
        sub_Actor = GetComponents<ISubActor>();
        sub_Sentiment = GetComponents<ISubSentiment>();
        sub_Nodes = GetComponents<ISubNode>();
        sub_Chats = GetComponents<ISubChats>();
        sub_Exits = GetComponents<ISubExits>();
    }

    private void Update()
    {
        if (IsTalking || _totalTalkTime <= 0) return;
        _totalTalkTime -= Time.deltaTime / delay;
    }

    public void OnUpdateActorCallbacks(ActorContext context)
    {
        foreach (var subActor in sub_Actor)
            subActor.UpdateActor(context);
        OnActorUpdate?.Invoke(this);

        Sentiment = context.Sentiment;
    }

    public void OnUpdateSentimentCallbacks(Sentiment sentiment)
    {
        foreach (var sub in sub_Sentiment)
            sub.UpdateSentiment(sentiment);
        OnSentimentUpdate?.Invoke(sentiment);
    }

    public IEnumerator Activate(ChatNode node)
    {
        yield return new WaitForSeconds(node.Delay);

        OnActivation?.Invoke(node);
        foreach (var subNode in sub_Nodes)
            subNode.Activate(node);

        var clip = node.AudioClip;

        if (clip == null)
            yield break;
        
        voice.clip = clip;
        voice.Play();

        var time = clip.length * voice.pitch * Actor.SpeakingRate * GlobalSpeakingRate;

        if (!node.Async)
            yield return new WaitUntilTimer(() => !voice.isPlaying, time);
        _totalTalkTime += time;
    }

    public IEnumerator Initialize(Chat chat)
    {
        foreach (var sub in sub_Chats)
            sub.Initialize(chat);
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, delay));
    }

    public IEnumerator Deactivate()
    {
        foreach (var sub in sub_Exits)
            sub.Deactivate();
        Destroy(gameObject);
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, delay));
    }
}
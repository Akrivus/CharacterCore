using System;
using System.Collections;
using UnityEngine;

public class ActorController : MonoBehaviour
{
    public event Action<ChatNode> OnActivation;
    public event Action<ActorController> OnActorUpdate;
    public event Action<Sentiment> OnSentimentUpdate;

    public float TotalVolume => voice.GetAmplitude() + sound.GetAmplitude();
    public float VoiceVolume => voice.GetAmplitude();
    public bool IsTalking => voice.isPlaying;

    public AudioSource Voice => voice;
    public AudioSource Sound => sound;

    public Color TextColor;

    [SerializeField]
    private AudioSource voice;

    [SerializeField]
    private AudioSource sound;

    public ActorContext Context
    {
        get => _context;
        set
        {
            _context = value;
            OnUpdateActorCallbacks(value);
        }
    }

    public Actor Actor => Context.Actor;

    public Sentiment Sentiment
    {
        get => _sentiment;
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
        voice.clip = clip;
        voice.Play();

        if (!node.Async)
            yield return new WaitUntilTimer(() => !voice.isPlaying,
                voice.clip.length * voice.pitch);
    }

    public IEnumerator Initialize(Chat chat)
    {
        foreach (var sub in sub_Chats)
            sub.Initialize(chat);
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
    }

    public IEnumerator Deactivate()
    {
        foreach (var sub in sub_Exits)
            sub.Deactivate();
        Destroy(gameObject);
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
    }
}
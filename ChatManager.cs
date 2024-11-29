using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance => _instance ?? (_instance = FindObjectOfType<ChatManager>());
    private static ChatManager _instance;

    public event Action BeforeIntermission;
    public event Func<Chat, IEnumerator> OnIntermission;

    public event Action<Chat> OnChatQueueAdded;
    public event Action<Chat> OnChatQueueTaken;

    public event Action<ChatNode> OnChatNodeActivated;

    public event Action OnChatQueueEmpty;

    public Chat NowPlaying { get; private set; }
    public List<Chat> PlayList => playList
        .ToList()
        .Prepend(NowPlaying)
        .ToList();

    [SerializeField]
    private GameObject prefab;

    private List<ActorController> actors = new List<ActorController>();
    private ConcurrentQueue<Chat> playList = new ConcurrentQueue<Chat>();

    [SerializeField]
    private string forceEpisodeName;

    private bool _firstTime = true;

    private void Awake()
    {
        _instance = this;
        Cursor.visible = false;
    }

    private async void Start()
    {
        Actors.Initialize();
        await StartPlayList();
    }

    public void AddToPlayList(Chat chat)
    {
        playList.Enqueue(chat);
        OnChatQueueAdded?.Invoke(chat);
    }

    private async Task StartPlayList()
    {
        if (!string.IsNullOrEmpty(forceEpisodeName))
            AddToPlayList(await Chat.Load(forceEpisodeName));
        if (playList.IsEmpty)
            OnChatQueueEmpty?.Invoke();
        StartCoroutine(UpdatePlayList());
    }

    private IEnumerator UpdatePlayList()
    {
        var chat = default(Chat);
        yield return new WaitUntilTimer(() => playList.TryDequeue(out chat), _firstTime ? 1 : 120);
        _firstTime = false;

        if (playList.IsEmpty)
        {
            OnChatQueueEmpty?.Invoke();
            yield return RemoveAllActors();
        }

        if (chat != null)
            yield return Play(chat);

        yield return UpdatePlayList();
    }

    private IEnumerator Play(Chat chat)
    {
        if (!chat.IsLocked || chat.Nodes.Count < 2)
            yield break;
        OnChatQueueTaken?.Invoke(chat);
        yield return Initialize(chat);

        foreach (var node in chat.Nodes)
            yield return Activate(node);
    }

    private IEnumerator Initialize(Chat chat)
    {
        yield return TryRemoveActors(chat);

        NowPlaying = chat;

        var incoming = chat.Actors.Where(a => !actors.Select(ac => ac.Actor).Contains(a.Actor));
        foreach (var context in incoming)
            yield return AddActor(context);

        BeforeIntermission?.Invoke();
        yield return OnIntermission?.Invoke(chat);
    }

    private IEnumerator Activate(ChatNode node)
    {
        yield return TryAddActor(node.Actor);
        OnChatNodeActivated?.Invoke(node);

        var actor = actors.Get(node.Actor);
        if (actor == null) yield break;
        yield return actor.Activate(node);

        yield return SetActorReactions(node);
    }

    private IEnumerator SetActorReactions(ChatNode node)
    {
        foreach (var reaction in node.Reactions)
            yield return TryAddActor(reaction.Actor);
        var reactions = node.Reactions
            .Select(c => actors.FirstOrDefault(a => a.Actor == c.Actor))
            .ToDictionary(a => a, a => node.Reactions
            .First(r => r.Actor == a.Actor).Sentiment);
        foreach (var reaction in reactions)
            reaction.Key.Sentiment = reaction.Value;
    }

    private IEnumerator TryAddActor(Actor actor)
    {
        if (actors.Get(actor) != null)
            yield break;
        var context = NowPlaying.Actors.Get(actor);
        yield return AddActor(context);
    }

    private int I = 0;

    private IEnumerator AddActor(ActorContext context)
    {
        var obj = Instantiate(prefab);
        obj.transform.Translate(Vector3.forward * I * 100f);
        var controller = obj.GetComponent<ActorController>();
        controller.OnActivation += SubtitlesUIManager.Instance.OnNodeActivated;
        controller.Context = context;
        controller.Sentiment = context.Actor.DefaultSentiment;
        actors.Add(controller);
        I++;
        yield return controller.Initialize(NowPlaying);
    }

    private IEnumerator TryRemoveActors(Chat chat)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
        StartCoroutine(RemoveActors(chat));
    }

    private IEnumerator RemoveActors(Chat chat)
    {
        var outgoing = actors
            .Where(a => !chat.Actors.Select(ac => ac.Actor).Contains(a.Actor))
            .ToList();
        for (var i = 0; i < outgoing.Count(); i++)
            yield return outgoing
                .ElementAt(i)
                .Deactivate();
        actors.RemoveAll(a => outgoing.Contains(a));
    }

    private IEnumerator RemoveAllActors()
    {
        for (var i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
            yield return actor.Deactivate();
            actors.Remove(actor);
        }
    }
}
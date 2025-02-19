using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance => _instance ?? (_instance = FindFirstObjectByType<ChatManager>());
    private static ChatManager _instance;

    public event Action OnChatQueueEmpty;
    public event Action<Chat> OnChatQueueAdded;

    public event Func<Chat, IEnumerator> OnChatQueueTaken;

    public event Func<Chat, IEnumerator> OnIntermission;

    public event Action BeforeIntermission;
    public event Action<Chat> AfterIntermission;

    public event Action<Chat, ActorController> OnActorAdded;
    public event Action<Chat, ActorController> OnActorRemoved;

    public event Action<ChatNode> OnChatNodeActivated;

    public bool RemoveActorsOnCompletion { get; set; } = true;
    public Chat NowPlaying { get; private set; }
    public List<Chat> PlayList => playList
        .ToList()
        .Prepend(NowPlaying)
        .ToList();

    private List<ActorController> actors = new List<ActorController>();
    private ConcurrentQueue<Chat> playList = new ConcurrentQueue<Chat>();

    [SerializeField]
    private string forceEpisodeName;

    [SerializeField]
    private Transform[] spawnPoints;

    private void Awake()
    {
        _instance = this;
        Cursor.visible = false;
    }

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);
        Actor.SearchableList.Initialize();
        await StartPlayList();
    }

    public void ForceRemoveAllActors()
    {
        StartCoroutine(RemoveAllActors());
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
        StartCoroutine(UpdatePlayList());
    }

    private IEnumerator UpdatePlayList()
    {
        while (Application.isPlaying)
        {
            if (playList.IsEmpty)
                OnChatQueueEmpty?.Invoke();

            var chat = default(Chat);
            yield return new WaitUntilTimer(() => playList.TryDequeue(out chat), 30);

            if (SubtitlesUIManager.Instance != null)
                SubtitlesUIManager.Instance.ClearSubtitles();
            if (playList.IsEmpty && RemoveActorsOnCompletion)
                yield return RemoveAllActors();

            if (chat != null)
                yield return Play(chat);
        }
    }

    private IEnumerator Play(Chat chat)
    {
        if (chat.IsLocked && chat.Nodes.Count < 2)
            yield break;

        if (OnChatQueueTaken != null)
            yield return OnChatQueueTaken(chat);

        yield return InitChat(chat);
        yield return PlayChat(chat);
    }

    private IEnumerator PlayChat(Chat chat)
    {
        if (chat.NextNode == null && !chat.IsLocked)
            yield return new WaitUntilTimer(() => chat.NextNode != null);
        
        var node = chat.NextNode;
        if (node == null)
            yield break;
        yield return Activate(node);

        node.New = false;
        yield return PlayChat(chat);
    }

    private IEnumerator InitChat(Chat chat)
    {
        yield return RemoveActors(chat);

        NowPlaying = chat;

        BeforeIntermission?.Invoke();
        yield return OnIntermission?.Invoke(chat);

        var incoming = chat.Actors
            .Where(a => !actors.Select(ac => ac.Actor).Contains(a.Reference));

        foreach (var context in incoming)
            yield return AddActor(context);

        foreach (var ac in actors)
            if (chat.Actors.Select(a => a.Reference).Contains(ac.Actor))
                ac.Sentiment = chat.Actors.Get(ac.Actor).Sentiment;

        if (chat.IsLocked)
            foreach (var node in chat.Nodes)
                node.New = true;

        AfterIntermission?.Invoke(chat);
    }

    private IEnumerator Activate(ChatNode node)
    {
        yield return TryAddActor(node.Actor);
        OnChatNodeActivated?.Invoke(node);

        var actor = actors.Get(node.Actor);
        if (actor == null)
            actor = actors.First();
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

    private IEnumerator AddActor(ActorContext context)
    {
        if (context == null)
            yield break;

        var spawnPoint = spawnPoints.FirstOrDefault(s => s.childCount == 0);
        var obj = Instantiate(context.Reference.Prefab, spawnPoint);

        var controller = obj.GetComponent<ActorController>();
        controller.Context = context;
        controller.Sentiment = context.Reference.DefaultSentiment;

        actors.Add(controller);
        yield return controller.Initialize(NowPlaying);
        OnActorAdded?.Invoke(NowPlaying, controller);
    }

    private IEnumerator RemoveActors(Chat chat)
    {
        var outgoing = actors
            .Where(a => !chat.Actors.Select(ac => ac.Reference).Contains(a.Actor))
            .ToArray();
        foreach (var actor in outgoing)
            yield return RemoveActor(actor);
    }

    private IEnumerator RemoveAllActors()
    {
        var outgoing = actors.ToArray();
        foreach (var actor in outgoing)
            yield return RemoveActor(actor);
    }

    private IEnumerator RemoveActor(ActorController controller)
    {
        yield return controller.Deactivate();
        actors.Remove(controller);
        OnActorRemoved?.Invoke(NowPlaying, controller);
    }
}
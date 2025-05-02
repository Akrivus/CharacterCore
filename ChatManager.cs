﻿using System;
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

    public static bool IsPaused { get; set; }
    public static bool SkipToEnd { get; set; }

    public event Action OnChatQueueEmpty;
    public event Action<Chat> OnChatQueueAdded;

    public event Func<Chat, IEnumerator> OnChatQueueTaken;

    public event Func<Chat, IEnumerator> OnIntermission;

    public event Action BeforeIntermission;
    public event Action<Chat> AfterIntermission;

    public event Action<Chat, ActorController> OnActorAdded;
    public event Action<Chat, ActorController> OnActorRemoved;

    public event Action<ChatNode> OnChatNodeActivated;

    public SpawnPointManager[] SpawnPoints => spawnPoints;
    public bool RemoveActorsOnCompletion { get; set; } = true;
    public Chat NowPlaying { get; private set; }

    private List<ActorController> actors = new List<ActorController>();
    private ConcurrentQueue<Chat> playList = new ConcurrentQueue<Chat>();

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private string forceEpisodeName;

    [SerializeField]
    private SpawnPointManager[] spawnPoints;

    [SerializeField]
    private Transform[] fallbackSpawnPoints;

    private SpawnPointManager spawnPointManager;

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

    private void OnApplicationQuit()
    {
        StopAllCoroutines();
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

        PostChatTitleCard(chat);

        yield return InitChat(chat);
        yield return PlayChat(chat);

        PostChatActorMemories(chat);

        SkipToEnd = false;
    }

    private IEnumerator PlayChat(Chat chat)
    {
        yield return new WaitUntil(() => !IsPaused);

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
        if (spawnPointManager != null)
            spawnPointManager.UnRegister();
        yield return RemoveActors(chat);

        NowPlaying = chat;

        if (!string.IsNullOrEmpty(chat.Location))
            spawnPointManager = spawnPoints.FirstOrDefault(s => s.name == chat.Location);
        if (spawnPointManager == null)
            spawnPointManager = spawnPoints.Shuffle().FirstOrDefault();
        if (spawnPointManager != null)
            spawnPointManager.Register();

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
        OnChatNodeActivated?.Invoke(node);

        if (SkipToEnd)
            yield break;

        var actor = actors.Get(node.Actor);
        if (actor == null)
            actor = actors.First();
        yield return actor.Activate(node);
        yield return SetActorReactions(actor, node);
    }

    private IEnumerator SetActorReactions(ActorController actor, ChatNode node)
    {
        var reactions = node.Reactions
            .Select(c => actors.FirstOrDefault(a => a.Actor == c.Actor))
            .ToDictionary(a => a, a => node.Reactions
            .First(r => r.Actor == a.Actor).Sentiment);
        foreach (var reaction in reactions)
        {
            reaction.Key.Sentiment = reaction.Value;
            reaction.Key.LookTarget = actor.LookObject.transform;
        }
        yield return PlayReactionClip(node.Reactions);
    }

    private IEnumerator PlayReactionClip(ChatNode.Reaction[] reactions)
    {
        if (audioSource == null)
            yield break;

        var reaction = reactions
            .GroupBy(r => r.Sentiment)
            .OrderByDescending(r => r.Count())
            .FirstOrDefault(r => r.Count() > 1)
            ?.First()?.Sentiment;
        if (reaction == null)
            yield break;
        var clip = reaction.Sound;
        if (clip == null)
            yield break;
        audioSource.clip = clip;
        audioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    private IEnumerator TryAddActor(Actor actor)
    {
        var context = NowPlaying.Actors.Get(actor);

        var controller = actors.Get(actor);
        if (controller != null)
        {
            controller.Context = context;
            controller.Sentiment = context.Reference.DefaultSentiment;
            yield return controller.Initialize(NowPlaying);
            yield break;
        }

        yield return AddActor(context);
    }

    private IEnumerator AddActor(ActorContext context)
    {
        if (context == null)
            yield break;

        var spawnPointTransform = fallbackSpawnPoints.FirstOrDefault(t => t.transform.childCount == 0);

        if (spawnPointManager != null)
        {
            var spawnPoint = spawnPointManager.spawnPoints.FirstOrDefault(t => t.name == context.Name);
            if (spawnPoint == null)
                spawnPoint = spawnPointManager.spawnPoints.FirstOrDefault(t => t.transform.childCount == 0);
            spawnPointTransform = spawnPoint.transform;
        }

        var obj = Instantiate(context.Reference.Prefab, spawnPointTransform);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        var controller = obj.GetComponent<ActorController>();
        controller.Context = context;
        controller.Sentiment = context.Reference.DefaultSentiment;

        actors.Add(controller);
        yield return controller.Initialize(NowPlaying);
        OnActorAdded?.Invoke(NowPlaying, controller);
    }

    private IEnumerator RemoveActors(Chat chat)
    {
        var outgoing = actors.ToList();
        if (!RemoveActorsOnCompletion)
            outgoing = actors.Except(chat.Actors.Select(ac => actors.Get(ac.Reference))).ToList();
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

    private void PostChatActorMemories(Chat chat)
    {
        foreach (var actor in chat.Actors)
        {
            DiscordManager.PutInQueue("#stream", new DiscordWebhookMessage(
                string.Empty, null, null,
                new DiscordEmbed
                {
                    Title = $"{actor.Costume} {actor.Name}",
                    Description = actor.Memory,
                    Color = actor.Reference.Color1.ToDiscordColor()
                }));
        }
    }

    private void PostChatTitleCard(Chat chat)
    {
        if (chat.Title == null)
            return;
        DiscordManager.PutInQueue("#stream", new DiscordWebhookMessage(
            "# :clapper: Now Streaming!", null, null,
            new DiscordEmbed
            {
                Title = chat.Title,
                Description = chat.Synopsis
            }));
    }
}
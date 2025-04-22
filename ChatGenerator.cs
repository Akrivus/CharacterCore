using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ChatGenerator : MonoBehaviour
{
    public int IdeaCount => ideaQueue.Count;

    [SerializeField]
    private bool save = true;

    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private Actor[] actors;

    [SerializeField]
    private SpawnPointManager[] locations;

    private string slug => name.Replace(' ', '-').ToLower();

    private ISubGenerator[] generators => _generators ?? (_generators = GetComponentsInChildren<ISubGenerator>());
    private ISubGenerator[] _generators;

    private ConcurrentQueue<Idea> ideaQueue = new ConcurrentQueue<Idea>();

    private void Start()
    {
        ServerSource.AddRoute("POST", $"/generate/{slug}", (_) => ServerSource.ProcessBodyString(_, AddPromptToQueue));
        StartCoroutine(UpdateQueue());
    }

    private void OnApplicationQuit()
    {
        StopAllCoroutines();
    }

    private IEnumerator UpdateQueue()
    {
        while (Application.isPlaying)
        {
            var idea = default(Idea);
            yield return new WaitUntilTimer(() => ideaQueue.TryDequeue(out idea), 120);

            if (idea == null)
                continue;

            yield return GenerateAndPlay(idea).AsCoroutine();
        }
    }

    public async Task<string> HandleRequest(Idea idea)
    {
        await Task.Run(() => AddIdeaToQueue(idea));
        return "OK.";
    }

    public void AddIdeaToQueue(Idea idea)
    {
        ideaQueue.Enqueue(idea);
    }

    public void AddPromptToQueue(string prompt)
    {
        AddIdeaToQueue(new Idea(prompt));
    }

    public async Task GenerateAndPlay(Idea idea)
    {
        var chat = await GenerateAndSave(idea);
        ChatManager.Instance.AddToPlayList(chat);
    }

    private async Task<Chat> GenerateAndSave(Idea idea)
    {
        var chat = new Chat(idea);

        try
        {
            chat = await Generate(chat);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        chat.Lock();
        if (save)
            chat.Save();
        return chat;
    }

    private async Task<Chat> Generate(Chat chat)
    {
        if (_prompt != null)
        {
            var options = "- " + string.Join("\n - ", GetCharacterNames());
            var secrets = string.Join(", ", GetCharacterNames(true));
            var locations = "- " + string.Join("\n - ", GetLocationNames());
            var idea = chat.Idea.Prompt;
            var context = await MemoryBucket.GetContext(slug);
            var prompt = _prompt.Format(context, options, idea, secrets, locations);
            var topic = await LLM.CompleteAsync(prompt, false);

            var characters = topic.Find("Characters");
            if (characters != null)
            {
                chat.Actors = characters.Split(',')
                    .Select(n => n.Trim())
                    .Select(n => Actor.All[n])
                    .OfType<Actor>()
                    .Select(a => new ActorContext(a))
                    .ToArray();
                topic = topic.Replace("Characters: " + characters, "");
            }

            var location = topic.Find("Location");
            if (location != null)
            {
                chat.Location = location;
                topic = topic.Replace("Location: " + location, "");
            }

            chat.Topic = topic;
            chat.Context = context;
        }

        foreach (var g in generators)
            await g.Generate(chat);

        return chat;
    }

    public void Receive(string message)
    {
        AddIdeaToQueue(new Idea(message));
    }

    private string[] GetCharacterNames(bool legacy = false)
    {
        var cast = actors.Length == 0 ? Actor.All.List : actors.ToList();
        var list = legacy ? Actor.All.List : cast;
        if (legacy)
            list = list
                .Except(cast)
                .OrderBy(a => a.IsLegacy)
                .ToList();
        return list
            .Select(a => string.Format("{0} ({1})", a.Name, a.Pronouns.Chomp()))
            .ToArray();
    }

    private string[] GetLocationNames()
    {
        var list = locations.Length == 0 ? ChatManager.Instance.SpawnPoints : locations;
        return list
            .Select(k => k.name)
            .Shuffle()
            .ToArray();
    }
}

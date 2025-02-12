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

    private string slug => name.Replace(' ', '-').ToLower();

    private ISubGenerator[] generators => _generators ?? (_generators = GetComponentsInChildren<ISubGenerator>());
    private ISubGenerator[] _generators;

    private ConcurrentQueue<Idea> ideaQueue = new ConcurrentQueue<Idea>();

    private void Start()
    {
        StartCoroutine(UpdateQueue());
        ServerSource.AddApiRoute<Idea, string>("POST", $"/generate?with={slug}", HandleRequest);
    }

    private IEnumerator UpdateQueue()
    {
        while (Application.isPlaying)
        {
            var idea = default(Idea);
            yield return new WaitUntil(() => ideaQueue.TryDequeue(out idea));

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
            var options = string.Join("\n - ", GetCharacterNames());
            var secrets = string.Join(", ", GetCharacterNames(true));
            var idea = chat.Idea.Prompt;
            var context = await EpisodeToEpisodeContinuity.GetLastEpisodeContext();
            var prompt = _prompt.Format(context, options, idea, secrets);
            var topic = await LLM.CompleteAsync(prompt, false);

            var characters = topic.Find("Characters");
            if (characters != null)
            {
                chat.Actors = characters.Split(',')
                    .Select(n => n.Trim())
                    .Select(n => ActorConverter.Find(n))
                    .OfType<Actor>()
                    .Select(a => new ActorContext(a))
                    .ToArray();
                topic = topic.Replace("Characters: " + characters, "");
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
        return Actor.All.List
            .Where(a => a.IsLegacy == legacy)
            .Select(k => string.Format("{0} ({1})", k.Name, k.Pronouns.Chomp()))
            .Shuffle()
            .ToArray();
    }
}

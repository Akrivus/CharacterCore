using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ChatGenerator : MonoBehaviour
{
    public event Func<Chat, Task> OnGeneration;

    public bool DisableLock { get; set; }

    [SerializeField]
    private TextAsset _prompt;

    private string slug => name.Replace(' ', '-').ToLower();

    private ISubGenerator[] generators => _generators ?? (_generators = GetComponentsInChildren<ISubGenerator>());
    private ISubGenerator[] _generators;

    private ConcurrentQueue<Idea> queue = new ConcurrentQueue<Idea>();

    private void Start()
    {
        StartCoroutine(UpdateQueue());
        ServerIntegration.AddApiRoute<Idea, string>("POST", $"/generate?with={slug}", HandleRequest);
    }

    private IEnumerator UpdateQueue()
    {
        var idea = default(Idea);
        yield return new WaitUntilTimer(() => queue.TryDequeue(out idea));

        if (idea != null)
        {
            var task = GenerateAndSave(idea);
            yield return new WaitUntilTimer(() => task.IsCompleted);
            var chat = task.Result;

            ChatManager.Instance.AddToPlayList(chat);
        }

        if (Application.isPlaying)
            yield return UpdateQueue();
    }

    public async Task<string> HandleRequest(Idea idea)
    {
        await Task.Run(() => AddIdeaToQueue(idea));
        return "OK.";
    }

    public void AddIdeaToQueue(Idea idea)
    {
        queue.Enqueue(idea);
    }

    public void AddPromptToQueue(string prompt)
    {
        AddIdeaToQueue(new Idea(prompt));
    }

    public async Task<Chat> GenerateAndSave(Idea idea)
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

        if (OnGeneration != null)
            await OnGeneration(chat);

        if (!DisableLock)
        {
            chat.Lock();
            chat.Save();
        }
        DisableLock = false;

        return chat;
    }

    public async Task<Chat> Generate(Chat chat)
    {
        if (_prompt != null)
        {
            var options = string.Join("\n - ", GetCharacterNames());
            var idea = chat.Idea.Prompt;
            var prompt = _prompt.Format(EpisodeToEpisodeContinuity.GroundState, options, idea);
            var topic = await OpenAiIntegration.CompleteAsync(prompt, false);

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
            chat.Context = EpisodeToEpisodeContinuity.GroundState;
        }

        foreach (var g in generators)
            await g.Generate(chat);

        return chat;
    }

    public void Receive(string message)
    {
        AddIdeaToQueue(new Idea(message));
    }

    private string[] GetCharacterNames()
    {
        return Actor.All.List.Select(k => string.Format("{0} ({1})", k.Name, k.Pronouns.Chomp())).ToArray();
    }
}

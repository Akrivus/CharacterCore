﻿using System;
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

    [SerializeField]
    private string chatType;

    private string slug => name.Replace(' ', '-').ToLower();

    private ISubGenerator[] generators => _generators ?? (_generators = GetComponentsInChildren<ISubGenerator>());
    private ISubGenerator[] _generators;

    private ConcurrentQueue<Idea> queue = new ConcurrentQueue<Idea>();

    private void Awake()
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
            yield return new WaitUntilTimer(() => task.IsCompleted, 1800);
            var chat = task.Result;

            ChatManager.Instance.AddToPlayList(chat);
        }

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
        var chat = await Generate(new Chat(idea));

        if (OnGeneration != null)
            await OnGeneration(chat);
        chat.Type = chatType;

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
            var options = string.Join(", ", GetCharacterNames());
            var prompt = _prompt.Format(chat.Idea.Prompt, options, Summarizer.GroundState);

            chat.Messages = await OpenAiIntegration.ChatAsync(prompt);
            chat.Topic = chat.Messages.Last().Content.ToString();
        }

        foreach (var generator in generators)
            await generator.Generate(chat);

        return chat;
    }

    public void Receive(string message)
    {
        AddIdeaToQueue(new Idea(message));
    }

    private static string[] _;

    private string[] GetCharacterNames()
    {
        return _ ??= Actor.All.List.Select(k => string.Format("{0} ({1})", k.Name, k.Pronouns.Chomp())).ToArray();
    }
}

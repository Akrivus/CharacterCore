using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class DialogueGeneration : MonoBehaviour, ISubGenerator
{
    private static string[] PluralNames = new string[] { "Everyone", "All" };

    [SerializeField]
    private bool fastMode = false;

    [SerializeField]
    private bool splitSentences = true;

    [SerializeField]
    private TextAsset _prompt;

    private int _attempts = 0;

    public async Task<Chat> Generate(Chat chat)
    {
        if (chat == null || chat.IsLocked)
            return chat;
        var prompt = _prompt.Format(chat.Idea.Prompt, chat.Characters, chat.Context);
        var content = await LLM.CompleteAsync(prompt, fastMode);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var actors = chat.Actors.ToList();
        var refs = actors.Select(a => a.Reference).ToList();

        var nodes = new List<ChatNode>();

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length <= 1)
                continue;
            var names = GetNames(parts[0], refs);
            var text = string.Join(":", parts.Skip(1));

            nodes.AddRange(AddNodes(names[0], text, refs, false));
            foreach (var n in names.Skip(1))
                nodes.AddRange(AddNodes(n, text, refs, true));
        }

        foreach (var n in nodes)
            if (actors.Get(n.Actor.Name) == null)
                actors.Add(new ActorContext(n.Actor));

        chat.Actors = actors.ToArray();
        chat.Nodes = nodes;

        if (_attempts < 3 && chat.Nodes.Count < 2)
        {
            _attempts++;
            return await Generate(chat);
        }
        _attempts = 0;

        return chat;
    }

    private List<ChatNode> AddNodes(string name, string text, List<Actor> actors, bool async)
    {
        var actor = actors.Find((a) => a.Aliases.Contains(name));
        if (actor == null)
            FindNewActor(name, text, actors, out actor);
        var nodes = new List<ChatNode>();
        if (actor == null)
            return nodes;

        var sentences = new string[] { text };
        if (splitSentences)
            sentences = text.ToSentences();
        
        nodes.Add(new ChatNode(actor, sentences[0]));
        foreach (var sentence in sentences.Skip(1))
        {
            var node = new ChatNode(actor, sentence);
            if (async)
                node.MarkAsync();
            nodes.Add(node);
        }

        return nodes;
    }

    private string[] GetNames(string name, List<Actor> actors)
    {
        var names = new List<string>();
        if (PluralNames.Contains(name))
            return actors.Select(a => a.Name).ToArray();
        var actor = actors.Find((a) => a.Aliases.Contains(name));
        if (actor != null)
            return new string[] { actor.Name };
        return name.Split(" and ");
    }

    private void FindNewActor(string name, string text, List<Actor> actors, out Actor actor)
    {
        actor = Actor.All[name];
        if (actor != null)
            return;
        actor = Actor.All["X"];
        text = $"{name} posted: {text}";
    }
}

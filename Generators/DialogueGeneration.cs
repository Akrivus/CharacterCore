using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class DialogueGeneration : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private bool fastMode = false;

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

        foreach (var line in lines)
        {
            var parts = line.Split(':');

            if (parts.Length <= 1)
                continue;
            var name = parts[0];
            var text = string.Join(":", parts.Skip(1));
            var sentences = text.ToSentences();
            
            var actor = actors.Select((a) => a.Reference).ToList().Find((a) => a.Aliases.Contains(name));
            if (actor == null)
                actor = Actor.All[name];
            if (actor == null)
                actor = Actor.All.List.Except(actors.Select(a => a.Reference)).Shuffle().FirstOrDefault();
            if (actor != null)
            {
                foreach (var sentence in sentences)
                    chat.Nodes.Add(new ChatNode(actor, sentence));
                if (chat.Actors.Get(actor.Name) == null)
                    actors.Add(new ActorContext(actor));
            }
        }

        chat.Actors = actors.ToArray();

        if (_attempts < 3 && chat.Nodes.Count < 2)
        {
            _attempts++;
            return await Generate(chat);
        }
        _attempts = 0;

        return chat;
    }
}

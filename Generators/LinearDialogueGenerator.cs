using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class LinearDialogueGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private string _override;

    private int _attempts = 0;

    public async Task<Chat> Generate(Chat chat)
    {
        if (chat == null || chat.IsLocked)
            return chat;
        var prompt = _prompt.Format(chat.Topic, chat.Context, _override);
        var content = await OpenAiIntegration.CompleteAsync(prompt);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            var name = parts[0];
            var text = string.Join(":", parts.Skip(1));

            var actor = ActorConverter.Find(name);
            if (actor != null)
                chat.Nodes.Add(new ChatNode(actor, text));
        }

        if (_attempts < 3 && chat.Nodes.Count < 2)
        {
            _attempts++;
            return await Generate(chat);
        }
        _attempts = 0;

        return chat;
    }
}

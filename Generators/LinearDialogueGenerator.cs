using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class LinearDialogueGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    private int _attempts = 0;

    private ChatGenerator ChatGenerator;

    private void Awake()
    {
        ChatGenerator = GetComponent<ChatGenerator>();
    }

    public async Task<Chat> Generate(Chat chat)
    {
        await ChatGenerator.GenerateContext(chat);

        var prompt = _prompt.Format(chat.Topic, chat.Context);
        var content = await ChatClient.CompleteAsync(prompt);
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

        if (_attempts < 3
            && chat.Nodes.Count < 2)
        {
            _attempts++;
            return await Generate(chat);
        }
        _attempts = 0;

        return chat;
    }
}

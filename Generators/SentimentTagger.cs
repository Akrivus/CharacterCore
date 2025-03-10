using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class SentimentTagger : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private bool doForNodes = true;

    private string log = "";

    public async Task<Chat> Generate(Chat chat)
    {
        var names = chat.Names;

        if (doForNodes)
            foreach (var node in chat.Nodes)
                if (node.Reactions == null || node.Reactions.Length == 0)
                    await GenerateForNode(node, names);
        if (string.IsNullOrEmpty(chat.Context))
            await GenerateForChat(chat, names, chat.Topic);
        else
            await GenerateForChat(chat, names, chat.Context);
        
        return chat;
    }

    private async Task GenerateForChat(Chat chat, string[] names, string context)
    {
        log = context;
        var reactions = await ParseReactions(names);
        foreach (var reaction in reactions)
            chat.Actors.Get(reaction.Actor).Sentiment = reaction.Sentiment;
        log = "";
    }

    public async Task GenerateForNode(ChatNode node, string[] names)
    {
        log += string.Format("{0}: {1}\n", node.Actor.Name, node.Say);
        node.Reactions = await ParseReactions(names);
    }

    private async Task<ChatNode.Reaction[]> ParseReactions(string[] names)
    {
        var faces = string.Join("\n- ", Sentiment.All.Select(s => s.Name));
        var options = string.Join("\n- ", names);
        var prompt = _prompt.Format(faces, options, log);

        var message = await LLM.CompleteAsync(prompt, true);
        var lines = message.Parse(names);
        var reactions = new ChatNode.Reaction[lines.Count];
        var i = 0;

        foreach (var line in lines)
            if (TryParseReaction(line.Key, line.Value, out Actor actor, out Sentiment sentiment))
                reactions[i++] = new ChatNode.Reaction(actor, sentiment);
        return reactions.OfType<ChatNode.Reaction>().ToArray();
    }

    private bool TryParseReaction(string name, string text, out Actor actor, out Sentiment sentiment)
    {
        actor = ActorConverter.Convert(name);
        sentiment = SentimentConverter.Convert(text);
        return sentiment != null && actor != null;
    }
}
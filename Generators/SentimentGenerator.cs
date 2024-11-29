using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class SentimentGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    private string context = "";

    public async Task<Chat> Generate(Chat chat)
    {
        var names = chat.Names;
        var topic = chat.Topic;

        foreach (var node in chat.Nodes)
            node.Reactions = await GenerateForNode(chat, node, names, topic);
        await GenerateForChat(chat, names, topic);
        
        return chat;
    }

    private async Task GenerateForChat(Chat chat, string[] names, string topic)
    {
        context = "Use the context to generate the initial emotional states of our characters.";
        var reactions = await ParseReactions(topic, names);
        foreach (var reaction in reactions)
            chat.Actors.Get(reaction.Actor).Sentiment = reaction.Sentiment;
        context = "";
    }

    private async Task<Reaction[]> GenerateForNode(Chat chat, ChatNode node, string[] names, string topic)
    {
        context += string.Format("{0}: {1}\n", node.Actor.Name, node.Text);
        return await ParseReactions(topic, names);
    }

    private async Task<Reaction[]> ParseReactions(string topic, string[] names)
    {
        var faces = string.Join(", ", Sentiment.All.Select(s => s.Name));
        var options = string.Join("\n- ", names);
        var prompt = _prompt.Format(faces, options, topic, context);

        var message = await ChatClient.CompleteAsync(prompt, true);
        var lines = message.Parse(names);
        var reactions = new Reaction[lines.Count];
        var i = 0;

        foreach (var line in lines)
            if (TryParseReaction(line.Key, line.Value, out Actor actor, out Sentiment sentiment))
                reactions[i++] = new Reaction(actor, sentiment);
        return reactions.OfType<Reaction>().ToArray();
    }

    private bool TryParseReaction(string name, string text, out Actor actor, out Sentiment sentiment)
    {
        actor = ActorConverter.Convert(name);
        sentiment = SentimentConverter.Convert(text);
        return sentiment != null && actor != null;
    }
}
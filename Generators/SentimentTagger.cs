using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class SentimentTagger : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private bool doForNodes = true;

    public async Task<Chat> Generate(Chat chat)
    {
        var names = chat.Names;

        if (doForNodes)
            foreach (var node in chat.Nodes)
                if (node.Reactions == null || node.Reactions.Length == 0)
                    await GenerateForNode(chat, node, names);
        if (string.IsNullOrEmpty(chat.Context))
            await GenerateForChat(chat, names, chat.Topic);
        else
            await GenerateForChat(chat, names, chat.Context);
        
        return chat;
    }

    private async Task GenerateForChat(Chat chat, string[] names, string context)
    {
        var sentiment = await GetSentiment(names, chat.Log, "Analyze initial conversation state based on context.", chat.Context);
        var reactions = ParseReactions(sentiment, names);
        foreach (var reaction in reactions)
            chat.Actors.Get(reaction.Actor).Sentiment = reaction.Sentiment;
    }

    public async Task GenerateForNode(Chat chat, ChatNode node, string[] names)
    {
        var actor = chat.Actors.Get(node.Actor);
        var context = actor.Prompt + "\n\n" + actor.Context;
        var sentiment = await GetSentiment(names, chat.Log, node.Say, context);

        var edit = sentiment.Find("Edit");
        if (!string.IsNullOrEmpty(edit))
            node.SetText(edit);

        node.Thoughts = sentiment.Find("Delivery");
        node.Reactions = ParseReactions(sentiment, names);
    }

    private async Task<string> GetSentiment(string[] names, string transcript, string line, string context)
    {
        var faces = "- " + string.Join("\n- ", Sentiment.All.Select(s => s.Name));
        var options = "- " + string.Join("\n- ", names);
        var prompt = _prompt.Format(faces, options, transcript, line, context);

        return await LLM.CompleteAsync(prompt, true);
    }

    private ChatNode.Reaction[] ParseReactions(string message, string[] names)
    {
        var lines = message.Parse(names);
        var reactions = new ChatNode.Reaction[lines.Count];
        var i = 0;

        foreach (var l in lines)
            if (TryParseReaction(l.Key, l.Value, out Actor actor, out Sentiment sentiment))
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
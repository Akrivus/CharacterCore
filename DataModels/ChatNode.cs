using Newtonsoft.Json;
using UnityEngine;


public class ChatNode
{
    [JsonConverter(typeof(ActorConverter))]
    public Actor Actor { get; set; }
    public string Line { get; set; }
    public string Text { get; set; }
    public string[] Actions { get; set; }
    public string Item { get; set; }
    public Reaction[] Reactions { get; set; }
    public bool Async { get; set; }
    public float Delay { get; set; } = 0;
    public string AudioData { get; set; }

    [JsonIgnore]
    public AudioClip AudioClip
    {
        get => AudioData.ToAudioClip();
        set => AudioData = value
            .ReSample()
            .ToBase64();
    }

    public ChatNode()
    {

    }

    public ChatNode(Actor actor, string text)
    {
        Actor = actor;
        Line = text;
        Text = text.Scrub();
        Actions = text.Rinse();
        Reactions = new Reaction[0];
    }

    public bool ShouldSerializeItem() => !string.IsNullOrEmpty(Item);
}

public class Reaction
{
    [JsonConverter(typeof(ActorConverter))]
    public Actor Actor { get; set; }

    [JsonConverter(typeof(SentimentConverter))]
    public Sentiment Sentiment { get; set; }

    public Reaction()
    {

    }

    public Reaction(Actor actor, Sentiment sentiment)
    {
        Actor = actor;
        Sentiment = sentiment ?? Sentiment.Default;
    }
}

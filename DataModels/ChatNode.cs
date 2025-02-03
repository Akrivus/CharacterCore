using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class ChatNode
{
    [JsonConverter(typeof(ActorConverter))]
    public Actor Actor { get; set; }

    public string Text { get; set; }
    public string Line { get; set; }
    public string[] Actions { get; set; }
    public string Item { get; set; }

    public string[] To { get; set; }
    public string Say { get; set; }
    public string Thoughts { get; set; }
    public string Notes { get; set; }

    public Reaction[] Reactions { get; set; }
    public bool Async { get; set; }
    public float Delay { get; set; } = 0;
    public int Frequency { get; set; } = 48000;
    public string AudioData { get; set; }

    [JsonIgnore]
    public AudioClip AudioClip
    {
        get => AudioData.ToAudioClip(Frequency);
        set => AudioData = value
            .ToBase64();
    }

    [JsonIgnore]
    public bool New { get; set; }

    public ChatNode()
    {

    }

    public ChatNode(Actor actor, Dictionary<string, string> chain, string say)
    {
        Actor = actor;
        Thoughts = chain["Thoughts"];
        Notes = chain["Notes"];
        Line = chain["Say"];
        Say = say.Scrub();
        Actions = say.Rinse();

        Text = $"Thoughts:\n{Thoughts}" +
            $"\n\nNotes:\n{Notes}" +
            $"\n\nSay:\n{Say}";
        Reactions = new Reaction[0];
    }

    public ChatNode(Actor actor, string text)
    {
        Actor = actor;
        Line = Text = text;
        Say = text.Scrub();
        Actions = text.Rinse();
        Reactions = new Reaction[0];
    }

    public ChatNode MarkAsync()
    {
        Async = true;
        return this;
    }

    public bool ShouldSerializeItem() => !string.IsNullOrEmpty(Item);

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
}
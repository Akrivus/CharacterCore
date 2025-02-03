using Newtonsoft.Json;

public class ActorContext
{
    [JsonConverter(typeof(ActorConverter))]
    public Actor Reference { get; set; }

    [JsonConverter(typeof(SentimentConverter))]
    public Sentiment Sentiment { get; set; }

    public string Costume { get; set; }
    public string Item { get; set; }
    public string SoundGroup { get; set; }
    public string SpawnPoint { get; set; }
    public string Context { get; set; }

    [JsonIgnore]
    public string Name => Reference.Name;

    public ActorContext(Actor actor)
    {
        Reference = actor;
        Costume = actor.Costume;
    }

    public ActorContext()
    {

    }

    [JsonIgnore]
    public string Prompt => Reference.Prompt.text;
}
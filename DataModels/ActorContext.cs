using Newtonsoft.Json;

public class ActorContext
{
    [JsonConverter(typeof(ActorConverter))]
    public Actor Actor { get; set; }

    [JsonConverter(typeof(SentimentConverter))]
    public Sentiment Sentiment { get; set; }

    public string Item { get; set; }
    public string SoundGroup { get; set; }
    
    [JsonIgnore]
    public string Name => Actor.Name;

    public ActorContext(Actor actor)
    {
        Actor = actor;
    }

    public ActorContext()
    {

    }

    [JsonIgnore]
    public string Context => Actor.Prompt.text;
}
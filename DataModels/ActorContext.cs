using Newtonsoft.Json;
using System.IO;

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
    public string Context => (Actor.Prompt.Format(Actor.Pronouns) + "\n" + Memories).Trim();

    [JsonIgnore]
    public string Memories => LoadMemories();

    private string LoadMemories()
    {
        if (!Directory.Exists("memories"))
            Directory.CreateDirectory("memories");
        var path = $"memories/{Actor.Name}.txt";

        if (!File.Exists(path))
            File.WriteAllText(path, "");
        return File.ReadAllText(path);
    }

    public void SaveMemories(string memories)
    {
        if (!Directory.Exists("memories"))
            Directory.CreateDirectory("memories");
        var path = $"memories/{Actor.Name}.txt";
        File.WriteAllText(path, memories);
    }
}
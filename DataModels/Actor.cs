using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Country", menuName = "UN/Country")]
public class Actor : ScriptableObject
{
    public static readonly Actors All = new Actors();

    [Header("Caption")]
    public string Title;
    public string Pronouns;

    [Header("Character")]
    public string Name;
    public string[] Aliases;

    public string Voice;

    public TextAsset Prompt;

    public float SpeakingRate;
    public float Pitch;
    public Color Color;

    public Sentiment DefaultSentiment;

    public static bool Has(string name) => All[name] != null;
}

public class Actors
{
    public Actor this[string name] => List.Find(actor => actor.Aliases.Contains(name));
    public void Add(Actor actor) => List.Add(actor);

    public List<Actor> List;

    public Actors()
    {
        List = new List<Actor>();
    }

    public Actors(List<ActorContext> actors)
    {
        List = actors.Select(actor => actor.Actor).ToList();
    }

    public static void Initialize()
    {
        var actors = Resources.LoadAll<Actor>("Actors");
        foreach (var chatter in actors)
            Actor.All.Add(chatter);
    }

    public static Actor Random()
    {
        var index = UnityEngine.Random.Range(0, Actor.All.List.Count);
        return Actor.All.List[index];
    }
}
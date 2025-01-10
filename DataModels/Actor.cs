using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Country", menuName = "UN/Country")]
public class Actor : ScriptableObject
{
    public static SearchableList All => _all;
    private static SearchableList _all = new SearchableList();

    [Header("Caption")]
    public string Title;
    public string Pronouns;

    [Header("Character")]
    public string Name;
    public string[] Aliases;
    public string[] Players;

    public string Voice;
    public string ColorScheme;

    public TextAsset Prompt;
    public GameObject Prefab;

    public float SpeakingRate;
    public float Pitch;
    public Color Color;

    public Color Color1;
    public Color Color2;
    public Color Color3;

    public Sentiment DefaultSentiment;

    public static bool Has(string name) => All[name] != null;

    public class SearchableList
    {
        public Actor this[string name] => List.Find(actor => actor.Aliases.Contains(name));
        public void Add(Actor actor) => List.Add(actor);

        public List<Actor> List;

        public SearchableList()
        {
            List = new List<Actor>();
        }

        public SearchableList(List<ActorContext> actors)
        {
            List = actors.Select(actor => actor.Reference).ToList();
        }

        public static void Initialize()
        {
            var actors = Resources.LoadAll<Actor>("Actors");
            foreach (var chatter in actors)
                All.Add(chatter);
        }

        public static Actor Random()
        {
            var index = UnityEngine.Random.Range(0, All.List.Count);
            return All.List[index];
        }
    }
}
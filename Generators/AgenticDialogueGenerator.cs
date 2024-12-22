using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AgenticDialogueGenerator : MonoBehaviour, ISubGenerator, IEventReceiver
{
    public Func<Chat, ChatNode, Task> OnNodeGenerated;

    public bool AlreadyReceivedAnEvent => _recieved;

    public int MinTurns = 10;
    public int MaxTurns = 30;

    [SerializeField]
    private TextAsset _prompt;

    [SerializeField]
    private bool _stopUntilRecieved;

    [SerializeField]
    private bool _stopUntilSilent;

    [SerializeField]
    private RedditIntegrator reddit;

    private Dictionary<string, DialogueAgent> agents = new Dictionary<string, DialogueAgent>();

    private TextToSpeechGenerator _tts;
    private SentimentTagger _stg;

    private bool _recieved;

    private void Awake()
    {
        _tts = GetComponent<TextToSpeechGenerator>();
        _stg = GetComponent<SentimentTagger>();

        OnNodeGenerated += async (chat, node) =>
        {
            await _tts.GenerateTextToSpeech(node);
            await _stg.GenerateForNode(node, chat.Names, chat.Topic);
        };
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var topic = chat.Topic.Find("Topic") ?? chat.Topic;
        var topics = chat.Topic.Parse(chat.Names);
        agents = chat.Actors
            .Select(actor => new DialogueAgent(
                actor, _prompt, $"{topic}\n\n{topics[actor.Name]}",
                chat.Names.Where(n => n != actor.Name).ToArray()))
            .ToDictionary(agent => agent.Actor.Name);
        if (agents.Count == 0)
            return chat;

        foreach (var a in agents.Values)
        {
            if (reddit != null)
                await a.Reddit(reddit);
            await a.Memorize();
            a.FinalizePrompt();
        }

        var order = new Queue<string>();
        var names = chat.Names.Shuffle();
        foreach (var name in names)
            order.Enqueue(name);

        var i = 0;

        while (!chat.IsLocked && i < MaxTurns && (MinTurns < i || order.Count > 0))
        {
            if (_stopUntilRecieved)
                while (!_recieved)
                    await Task.Delay(100);

            var name = chat.Names.Random();
            if (order.Count > 0)
                name = order.Dequeue();
            var agent = agents[name];
            var response = await agent.Respond();
            var chain = response.Parse("Thoughts", "Notes", "Say");

            foreach (var key in order)
            {
                var actor = agents[key];
                if (actor.IsExited)
                    continue;
                actor.AddToBuffer(agent.Actor.Name, chain["Say"]);
            }

            var node = new ChatNode(agent.Actor.Actor, chain);
            chat.Nodes.Add(node);
            await OnNodeGenerated(chat, node);

            var actors = chat.Names
                .Where(n => !agents[n].IsExited);
            if (actors.Count() < 2)
                break;
            var aliases = actors
                .Select(n => agents[n].Actor.Actor.Aliases)
                .SelectMany(a => a)
                .ToArray();
            names = aliases
                .Where(n => node.Say.Contains(n))
                .OrderBy(n => node.Say.IndexOf(n))
                .Select(n => Actor.All[n].Name)
                .Distinct()
                .ToArray();
            foreach (var n in names)
                order.Enqueue(n);
            ++i;

            _recieved = false;

            if (_stopUntilSilent)
                while (chat.NextNode != null)
                    await Task.Delay(100);
        }

        return chat;
    }

    public void Receive(string message, bool initialOnly = true)
    {
        if (_recieved && initialOnly)
            return;
        
        foreach (var agent in agents.Values)
            agent.AddToBuffer(name, message);
        _recieved = true;
    }

    public class DialogueAgent
    {
        public static readonly string END_TOKEN = "[TERMINATE]";

        public string Prompt { get; set; }
        public bool IsExited { get; private set; }
        public ActorContext Actor => _actor;

        private ActorContext _actor;
        private List<Message> _messages;
        private string _buffer;

        public DialogueAgent(ActorContext actor, TextAsset prompt, string topic, string[] names)
        {
            Prompt = prompt.Format(END_TOKEN, actor.Name, actor.Context, topic, actor.SoundGroup);
            _actor = actor;
            _buffer = GenerateBufferSentence(names);
        }

        public async Task<DialogueAgent> Memorize()
        {
            var bucket = new MemoryBucket(Actor.Name);
            await bucket.Load();
            Prompt += bucket.Get();
            return this;
        }

        public async Task<DialogueAgent> Reddit(RedditIntegrator reddit)
        {
            Prompt = await reddit.ReplaceSubReddits(Prompt);
            return this;
        }

        public void FinalizePrompt()
        {
            _messages = new List<Message>()
            {
                new Message(Role.System, Prompt)
            };
        }

        public void AddToBuffer(string name, string text)
        {
            _buffer += name + ": " + text + "\n\n";
        }

        public async Task<string> Respond()
        {
            _messages.Add(new Message(Role.User, _buffer));
            _messages = await OpenAiIntegration.ChatAsync(_messages, true);

            var response = _messages.Last().Content.ToString();
            if (response.Contains(END_TOKEN))
                IsExited = true;
            _buffer = "";
            return response;
        }

        private string GenerateBufferSentence(string[] names)
        {
            var everyone = names.Length == 1 ?
                $" and {names[0]}" :
                $", {string.Join(", ", names.Take(names.Length - 1))} and {names.Last()}";
            return $"(The scene starts with you{everyone}.)\n\n";
        }
    }
}
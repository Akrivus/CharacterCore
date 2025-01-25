using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections;
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
    private bool _stopUntilBackedUp;

    private Dictionary<string, DialogueAgent> agents = new Dictionary<string, DialogueAgent>();

    private SentimentTagger _sentiment;
    private MemoryManager _memories;
    private TextToSpeechGenerator _tts;

    private Queue<ChatNode> AddNodeQueue = new Queue<ChatNode>();

    private bool _recieved;
    private Coroutine _coroutine;

    private bool _async;

    private void Awake()
    {
        _sentiment = GetComponent<SentimentTagger>();
        _memories = GetComponent<MemoryManager>();
        _tts = GetComponent<TextToSpeechGenerator>();

        _async = GetComponent<ChatEventBroker>() != null;

        if (!_async) return;

        OnNodeGenerated += async (chat, node) =>
        {
            await _sentiment.GenerateForNode(node, chat.Names, chat.Topic);
            await _memories.Memorize(node);
            await _tts.GenerateTextToSpeech(node);
        };
        ChatManager.Instance.AfterIntermission += StartNodeQueue;
        ChatManager.Instance.BeforeIntermission += StopNodeQueue;
    }

    private void StartNodeQueue(Chat chat)
    {
        _coroutine = StartCoroutine(UpdateNodeQueue(chat));
    }

    private void StopNodeQueue()
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);
    }

    private IEnumerator UpdateNodeQueue(Chat chat)
    {
        if (chat == null || chat.IsLocked)
            yield break;

        yield return new WaitUntil(() => AddNodeQueue.Count > 0);

        var node = AddNodeQueue.Dequeue();
        chat.Nodes.Add(node);
        yield return OnNodeGenerated(chat, node);

        yield return UpdateNodeQueue(chat);
    }

    public async Task<Chat> Generate(Chat chat)
    {
        var topic = chat.Topic;
        var topics = topic.Parse(chat.Names);

        chat.Topic = topic.Find("Activity") ?? topic;

        agents = chat.Actors
            .Select(actor => new DialogueAgent(actor, _prompt,
                string.Join("\n\n", chat.Topic, topics[actor.Name]),
                chat.Names.Where(n => n != actor.Name).ToArray()))
            .ToDictionary(agent => agent.Actor.Name);
        if (agents.Count == 0)
            return chat;

        foreach (var a in agents.Values)
        {
            await a.Memorize();
            a.FinalizePrompt();
        }

        var cues = new Queue<string>(chat.Cues);

        var order = new Queue<string>();
        var names = chat.Names;

        var i = 0;

        while (!chat.IsLocked && i < MaxTurns && (MinTurns < i || cues.Count > 0 || order.Count > 0) && Application.isPlaying)
        {
            while (_stopUntilRecieved && !_recieved && Application.isPlaying)
                await Task.Delay(100);

            var actors = chat.Names
                .Where(n => !agents[n].IsExited);
            if (actors.Count() < 2)
                break;
            var aliases = actors
                .Select(n => agents[n].Actor.Reference.Aliases)
                .SelectMany(a => a)
                .ToArray();

            if (order.Count == 0 && cues.TryDequeue(out var cue))
                foreach (var key in chat.Names)
                {
                    var actor = agents[key];
                    if (actor.IsExited)
                        continue;
                    actor.AddToBuffer(cue);
                    names = aliases
                        .Where(n => cue.Contains(n))
                        .OrderBy(n => cue.IndexOf(n))
                        .Select(n => Actor.All[n].Name)
                        .Distinct()
                        .ToArray();
                    if (names.Length > 0)
                        order = new Queue<string>(names);
                }

            var name = chat.Names.Random();
            if (order.Count > 0)
                name = order.Dequeue();
            var agent = agents[name];
            var response = await agent.Respond();
            var chain = response.Parse("Thoughts", "Notes", "Say");
            var message = chain["Say"];

            foreach (var key in chat.Names)
            {
                var actor = agents[key];
                if (actor.IsExited)
                    continue;
                actor.AddToBuffer(agent.Actor.Name, message);
            }

            AddNodes(agent.Actor.Reference, chain, message.ToSentences());

            while (!_async && AddNodeQueue.TryDequeue(out var node))
                chat.Nodes.Add(node);

            names = aliases
                .Where(n => message.Contains(n))
                .OrderBy(n => message.IndexOf(n))
                .Select(n => Actor.All[n].Name)
                .Distinct()
                .ToArray();
            if (names.Length > 0)
                order = new Queue<string>(names);
            ++i;

            _recieved = false;

            while (_stopUntilSilent && chat.NextNode != null && Application.isPlaying)
                await Task.Delay(100);

            while (_stopUntilBackedUp && chat.NextNode == null && Application.isPlaying)
                await Task.Delay(100);
        }

        return chat;
    }

    private void AddNodes(Actor actor, Dictionary<string, string> chain, string[] sentences)
    {
        for (int _ = 0; _ < sentences.Length; _++)
            if (_ == 0)
                AddNodeQueue.Enqueue(new ChatNode(actor, chain, sentences[_]));
            else
                AddNodeQueue.Enqueue(new ChatNode(actor, sentences[_]));    
    }

    public void Receive(string message, bool initialOnly = true)
    {
        if (_recieved && initialOnly)
            return;
        
        foreach (var agent in agents.Values)
            agent.AddToBuffer(message);
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

        public void AddToBuffer(string text)
        {
            _buffer += text + "\n\n";
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
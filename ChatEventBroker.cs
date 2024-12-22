using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChatEventBroker : MonoBehaviour
{
    public bool IsBusy => chat == null || chat.IsLocked || chat.NextNode != null;

    [SerializeField]
    private Actor narrator;

    [SerializeField]
    private AgenticDialogueGenerator generator;

    [SerializeField]
    private AudioSource source;

    private Chat chat;

    [SerializeField]
    private TextToSpeechGenerator tts;

    [SerializeField]
    private ISubGenerator[] generators;

    [SerializeField]
    private bool narrating;

    [SerializeField]
    private bool emitting;

    private ChatNode _node;
    private ConcurrentQueue<Event> _events = new ConcurrentQueue<Event>();
    private Dictionary<string, ChatNode> _nodes = new Dictionary<string, ChatNode>();

    private void Awake()
    {
        generators = GetComponentsInChildren<ISubGenerator>();
    }

    private void Start()
    {
        ChatManager.Instance.OnChatQueueTaken += async (c) => await Generate(c);
    }

    private void Update()
    {
        if (_events.TryDequeue(out var e))
            Receive(e.Message, e.InitialOnly, e.Narrate);
    }

    private IEnumerator Narrate()
    {
        if (_node.AudioData == null)
        {
            var task = tts.GenerateTextToSpeech(_node);
            yield return new WaitUntil(() => _node.AudioData != null);
        }
        source.PlayOneShot(_node.AudioClip);
    }

    public void Receive(string message, bool initialOnly = true, bool narrate = false)
    {
        if (chat == null)
            return;

        if (_nodes.ContainsKey(message))
            _node = _nodes[message];
        else
            _node = new ChatNode(narrator, message);

        if (narrating || narrate)
            StartCoroutine(Narrate());

        generator.Receive(message, initialOnly);
    }

    public void RecieveAsync(string message, bool initialOnly = true, bool narrate = false)
    {
        _events.Enqueue(new Event
        {
            Message = message,
            InitialOnly = initialOnly,
            Narrate = narrate
        });
    }

    public void Close()
    {
        if (chat == null)
            return;
        chat.Lock();
        chat = null;
    }

    private async Task Generate(Chat chat)
    {
        if (chat == null || chat.IsLocked)
            return;
        this.chat = chat;
        foreach (var g in generators)
            await g.Generate(chat);
    }

    public class Event
    {
        public string Message { get; set; }
        public bool InitialOnly { get; set; }
        public bool Narrate { get; set; }
    }
}
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AgenticDialogueGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public int MaxTurns = 30;

    public async Task<Chat> Generate(Chat chat)
    {
        var topics = chat.Topic.Parse(chat.Names);
        var agents = chat.Actors
            .Select(actor => new ChatTreeAgent(actor, _prompt, topics[actor.Name], chat.Names.Where(n => n != actor.Name).ToArray()))
            .ToDictionary(agent => agent.Actor.Name);
        var memories = chat.Actors.ToDictionary(actor => actor.Name, actor => actor.Memories);
        var agent = agents.First().Value;

        int exited = 0;

        var order = agents.Keys.ToArray();

        for (var i = 0; i < MaxTurns && exited < agents.Count; i++)
        {
            var response = await agent.Respond();
            var chain = response.Parse("Thoughts", "Notes", "Say");

            memories[agent.Actor.Name] += chain["Notes"] + " ";

            foreach (var actor in agents.Values)
                if (!actor.IsExited)
                {
                    actor.AddToBuffer(agent.Actor.Name, chain["Say"]);
                    if (agent.IsExited)
                        actor.AddToBuffer(agent.Actor.Name, $"(Exits scene. Say `{ChatTreeAgent.END_TOKEN}` to exit scene.)");
                }

            chat.Nodes.Add(new ChatNode(agent.Actor.Actor, chain));

            var name = order[(i + 1) % order.Length];
            if (agents[name].IsExited)
                exited++;
            else
                agent = agents[name];
        }

        foreach (var m in memories)
            chat.Actors.Get(m.Key).SaveMemories(m.Value);

        return chat;
    }
}

public class ChatTreeAgent
{
    public static readonly string END_TOKEN = "END_CONVERSATION";

    public bool IsExited { get; private set; }
    public ActorContext Actor => _actor;

    private ActorContext _actor;
    private List<Message> _messages;
    private string _buffer;

    private string _prompt;

    public ChatTreeAgent(ActorContext actor, TextAsset prompt, string context, string[] names)
    {
        _actor = actor;
        _prompt = prompt.Format(END_TOKEN, actor.Context, actor.Memories, context, ContextGenerator.GroundStateContext);
        _buffer = GenerateBufferSentence(names);
        _messages = new List<Message>()
        {
            new Message(Role.System, _prompt)
        };
    }

    public void AddToBuffer(string name, string text)
    {
        _buffer += name + ": " + text + "\n\n";
    }

    public async Task<string> Respond()
    {
        _messages.Add(new Message(Role.User, _buffer));
        _messages = await ChatClient.ChatAsync(_messages, true);

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

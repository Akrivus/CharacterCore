using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FreeformDialogueGenerator : MonoBehaviour, ISubGenerator
{
    [SerializeField]
    private TextAsset _prompt;

    public async Task<Chat> Generate(Chat chat)
    {
        var actors = chat.Actors
            .Select(character => new ChatTreeAgent(character, _prompt))
            .ToDictionary(agent => agent.Actor.Name);
        var queue = new Queue<ChatTreeAgent>();
        queue.Enqueue(actors.First().Value);

        do
        {
            var agent = queue.Dequeue();
            var response = await agent.Respond();
            var tos = response.FindAll("To")
                .Select(s => s.Split(',')
                    .Select(s => s.Trim()))
                .SelectMany(s => s)
                .ToArray();
            foreach (var actor in actors.Values)
            {
                actor.AddToBuffer(agent.Actor.Name, response);
                if (tos.Contains(actor.Actor.Name))
                    queue.Enqueue(actor);
            }
        } while (queue.Any());
        return chat;
    }
}

public class ChatTreeAgent
{
    private static readonly string END_TOKEN = "[END CONVERSATION]";

    public bool IsExited { get; private set; }
    public ActorContext Actor => _actor;

    private ActorContext _actor;
    private List<Message> _messages;
    private string _buffer;

    private string _prompt;

    public ChatTreeAgent(ActorContext context, TextAsset prompt)
    {
        _actor = context;
        _prompt = prompt.Format(context.Context, END_TOKEN);
        _buffer = "";
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
        _buffer = "";
        _messages = await ChatClient.ChatAsync(_messages, true);

        var response = _messages.Last().Content.ToString();
        if (response.Contains(END_TOKEN))
            IsExited = true;
        return response;
    }
}

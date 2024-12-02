using System.Collections.Generic;
using UnityEngine;

public class NotesGenerator : MonoBehaviour, ISubGenerator.Sync
{
    public Chat Generate(Chat chat)
    {
        var memories = new Dictionary<string, string>();
        foreach (var actor in chat.Actors)
            memories[actor.Name] = actor.Memories + "\n";
        foreach (var node in chat.Nodes)
            memories[node.Actor.Name] += node.Notes + "\n";
        foreach (var actor in chat.Actors)
            actor.SaveMemories(memories[actor.Name]);
        return chat;
    }
}
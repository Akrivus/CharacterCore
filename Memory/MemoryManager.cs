using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MemoryManager : MonoBehaviour, ISubGenerator
{
    private List<MemoryBucket> buckets = new List<MemoryBucket>();

    public async Task<Chat> Generate(Chat chat)
    {
        foreach (var bucket in buckets)
        {
            bucket.Clean();
            await bucket.Save();
        }
        buckets.Clear();
        return chat;
    }

    public async Task Memorize(ChatNode node)
    {
        var bucket = buckets.FirstOrDefault(x => x.Name == node.Actor.Name);
        if (bucket == null)
        {
            bucket = new MemoryBucket(node.Actor.Name);
            await bucket.Load();
            buckets.Add(bucket);
        }
        await bucket.Add(node.Notes);
    }
}
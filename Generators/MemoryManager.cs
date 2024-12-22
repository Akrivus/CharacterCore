using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MemoryManager : MonoBehaviour
{
    [SerializeField]
    private AgenticDialogueGenerator generator;

    private void Awake()
    {
        generator.OnNodeGenerated += Memorize;
    }

    private async Task Memorize(Chat chat, ChatNode node)
    {
        var bucket = new MemoryBucket(node.Actor.Name);
        await bucket.Load();
        await bucket.Add(node.Notes);

        bucket.Clean();
        await bucket.Save();
    }
}
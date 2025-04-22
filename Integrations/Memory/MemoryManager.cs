using System.Linq;
using UnityEngine;

public class MemoryManager : MonoBehaviour
{
    public async void OnApplicationQuit()
    {
        var buckets = MemoryBucket.Buckets.Values.ToArray();
        foreach (var bucket in buckets)
            await bucket.Save();
    }
}
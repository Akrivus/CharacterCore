using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MemoryBucket
{
    public string Name { get; private set; }
    public List<Memory> Memories { get; private set; }

    public MemoryBucket(string name)
    {
        Name = name;
        Memories = new List<Memory>();
    }

    public async Task Add(string text)
    {
        var embeddings = await OpenAiIntegration.EmbedAsync(text);
        Memories.Add(new Memory(text, embeddings));
    }

    public async Task Save()
    {
        if (!Directory.Exists("./Memories"))
            Directory.CreateDirectory("./Memories");

        var json = JsonConvert.SerializeObject(Memories);
        await File.WriteAllTextAsync($"./Memories/{Name}.json", json);
    }

    public async Task Load()
    {
        if (!Directory.Exists("./Memories"))
            Directory.CreateDirectory("./Memories");
        if (!File.Exists($"./Memories/{Name}.json"))
            await File.WriteAllTextAsync($"./Memories/{Name}.json", "[]");

        var json = await File.ReadAllTextAsync($"./Memories/{Name}.json");
        Memories = JsonConvert.DeserializeObject<List<Memory>>(json);
    }

    public async Task<string> Recall(string text)
    {
        var embeddings = await OpenAiIntegration.EmbedAsync(text);
        var memory = Memories.OrderBy(x => CosineSimilarity(x.Embeddings, embeddings)).First();
        return memory.Text;
    }

    public string Get()
    {
        var memory = Memories.OrderBy(x => x.Created).Take(24).Select(x => x.Text).ToArray();
        return string.Join("\n", memory);
    }

    public void Clean()
    {
        for (var i = 0; i < Memories.Count; i++)
        {
            var memory = Memories[i];
            var similar = Memories
                .Where(x => x != memory)
                .Where(x => CosineSimilarity(x.Embeddings, memory.Embeddings) > 0.9)
                .OrderBy(x => x.Created)
                .ToList();
            foreach (var s in similar)
                Memories.Remove(s);
        }
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        var dotProduct = a.Zip(b, (x, y) => x * y).Sum();
        var magnitudeA = Math.Sqrt(a.Sum(x => x * x));
        var magnitudeB = Math.Sqrt(b.Sum(x => x * x));
        return dotProduct / (magnitudeA * magnitudeB);
    }
}

public class Memory
{
    public string Text { get; private set; }
    public double[] Embeddings { get; private set; }
    public DateTime Created { get; private set; }

    public Memory(string text, double[] embeddings)
    {
        Text = text;
        Embeddings = embeddings;
        Created = DateTime.Now;
    }
}
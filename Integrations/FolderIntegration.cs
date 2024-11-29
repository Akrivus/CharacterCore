using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FolderIntegration : MonoBehaviour, IConfigurable<FolderConfigs>
{
    [SerializeField]
    private ChatGenerator ChatGenerator;

    public string ReplayDirectory;
    public int ReplayRate = 80;
    public int ReplaysPerBatch = 20;
    public int MaxReplayAgeInMinutes = 1440;
    public bool AutoPlay = true;
    public bool AutoPlayOnEmpty = true;

    private List<string> replays = new List<string>();

    public void Configure(FolderConfigs c)
    {
        ReplayDirectory = c.ReplayDirectory;
        ReplayRate = c.ReplayRate;
        ReplaysPerBatch = c.ReplaysPerBatch;
        MaxReplayAgeInMinutes = c.MaxReplayAgeInMinutes;
        AutoPlay = c.AutoPlay;
        AutoPlayOnEmpty = c.AutoPlayOnEmpty;

        replays = LoadReplays();

        Chat.FolderName = ReplayDirectory;

        for (var i = 0; i < c.Prompts.Count; i++)
            if (File.Exists(c.Prompts[i]))
                c.Prompts[i] = File.ReadAllText(c.Prompts[i]);
        foreach (var prompt in c.Prompts)
            ChatGenerator.AddIdeaToQueue(new Idea(prompt));

        if (AutoPlayOnEmpty)
            ChatManager.Instance.OnChatQueueEmpty += ReplayEpisode;
        if (AutoPlay)
            ReplayEpisode();

    }

    private void Awake()
    {
        ConfigManager.Instance.RegisterConfig(typeof(FolderConfigs), "folder", (config) => Configure((FolderConfigs) config));
    }

    private void OnDestroy()
    {
        File.WriteAllLines("replays.txt", replays);
    }

    private async void ReplayEpisode()
    {
        var ideas = new List<Idea>();
        await FetchFiles(ReplaysPerBatch);

        foreach (var idea in ideas)
            ChatGenerator.AddIdeaToQueue(idea);
    }

    private async Task FetchFiles(int count)
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var path = Path.Combine(docs, Chat.FolderName);

        var tasks = Directory.GetFiles(path, "*.json")
            .Where(file => File.GetLastWriteTime(file) > DateTime.Now.AddMinutes(-MaxReplayAgeInMinutes))
            .OrderBy(file => File.GetLastWriteTime(file))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(title => !replays.Contains(title))
            .Take(count).Select(LogThenLoad)
            .ToList();

        foreach (var task in tasks)
            ChatManager.Instance.AddToPlayList(await task);
    }

    private async Task<Chat> LogThenLoad(string title)
    {
        replays = replays.TakeLast(ReplayRate - 1).ToList();
        replays.Add(title);
        return await Chat.Load(title);
    }

    private List<string> LoadReplays()
    {
        if (!File.Exists("replays.txt"))
            return new List<string>();
        return File.ReadAllLines("replays.txt")
            .ToList();
    }
}
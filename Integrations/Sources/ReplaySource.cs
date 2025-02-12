using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ReplaySource : MonoBehaviour, IConfigurable<ReplayConfigs>
{
    public string ReplayDirectory;
    public int ReplayRate = 80;
    public int ReplaysPerBatch = 20;
    public int MaxReplayAgeInMinutes = 1440;

    private List<string> replays = new List<string>();
    private ConcurrentQueue<Chat> queue = new ConcurrentQueue<Chat>();

    public string Name => "replay";
    public bool IsRunning => queue.Count > 0;

    public void Configure(ReplayConfigs c)
    {
        ReplayDirectory = c.ReplayDirectory;
        ReplayRate = c.ReplayRate;
        ReplaysPerBatch = c.ReplaysPerBatch;
        MaxReplayAgeInMinutes = c.MaxReplayAgeInMinutes;

        if (MaxReplayAgeInMinutes < 1)
            MaxReplayAgeInMinutes = 1440 * 365;

        replays = LoadReplays();

        Chat.FolderName = ReplayDirectory;

        ChatManager.Instance.OnChatQueueEmpty += OnChatQueueEmpty;
    }

    public void OnChatQueueEmpty()
    {
        StartCoroutine(ReplayEpisodes());
    }

    private IEnumerator ReplayEpisodes()
    {
        yield return FetchFiles(ReplaysPerBatch).AsCoroutine();
        while (queue.TryDequeue(out var chat))
            ChatManager.Instance.AddToPlayList(chat);
    }

    private void Awake()
    {
        ConfigManager.Instance.RegisterConfig(typeof(ReplayConfigs), "folder", (config) => Configure((ReplayConfigs) config));
    }

    private void OnDestroy()
    {
        File.WriteAllLines("replays.txt", replays);
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
            queue.Enqueue(await task);
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
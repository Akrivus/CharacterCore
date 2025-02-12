public class ReplayConfigs : IConfig
{
    public string Type => "replay";
    public string ReplayDirectory { get; set; }
    public int ReplayRate { get; set; }
    public int ReplaysPerBatch { get; set; }
    public int MaxReplayAgeInMinutes { get; set; }
}
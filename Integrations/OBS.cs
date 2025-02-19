using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class OBS : MonoBehaviour, IConfigurable<OBSConfigs>
{
    [SerializeField]
    private string OBSWebSocketURI = "ws://localhost:4455";
    [SerializeField]
    private bool IsStreaming = false;
    [SerializeField]
    private bool IsRecording = false;
    [SerializeField]
    private bool DoSplitRecording = false;
    [SerializeField]
    private bool OnlyNewEpisodes = true;
    [SerializeField]
    private int EmptyQueueChances = 2;

    private bool isObsRecording = false;
    private bool isObsStreaming = false;

    private int emptyQueueChance = 0;

    public void Configure(OBSConfigs c)
    {
        OBSWebSocketURI = c.OBSWebSocketURI;
        IsStreaming = c.IsStreaming;
        IsRecording = c.IsRecording;
        DoSplitRecording = c.DoSplitRecording;
        OnlyNewEpisodes = c.OnlyNewEpisodes;
        EmptyQueueChances = c.EmptyQueueChances;

        if (IsStreaming)
            StartStreaming();
        if (IsRecording)
        {
            if (OnlyNewEpisodes)
                ChatManager.Instance.AfterIntermission += StopOrStartRecording;
            if (DoSplitRecording)
                ChatManager.Instance.BeforeIntermission += SplitRecording;
            ChatManager.Instance.OnChatQueueEmpty += CheckEmptyQueue;
            ChatManager.Instance.BeforeIntermission += StartRecording;
        }
    }

    private void Awake()
    {
        ConfigManager.Instance.RegisterConfig(typeof(OBSConfigs), "obs", (config) => Configure((OBSConfigs) config));
    }

    private void OnDestroy()
    {
        if (IsStreaming)
            StopStreaming();
        if (IsRecording)
            StopRecording();
    }

    private void CheckEmptyQueue()
    {
        if (emptyQueueChance >= EmptyQueueChances)
        {
            emptyQueueChance = 0;
            StopRecording();
        }
        else
            emptyQueueChance++;
    }

    public void StartRecording()
    {
        if (isObsRecording)
            return;
        isObsRecording = true;
        SendRequestAsync("StartRecord");
    }

    public void StopRecording()
    {
        if (!isObsRecording)
            return;
        isObsRecording = false;
        SendRequestAsync("StopRecord");
    }

    public void StopOrStartRecording(Chat _)
    {
        if (_.NewEpisode)
            StartRecording();
        else
            StopRecording();
    }

    public void SplitRecording()
    {
        if (!isObsRecording)
            return;
        SendRequestAsync("SplitRecordFile");
    }

    public void StartStreaming()
    {
        if (isObsStreaming)
            return;
        isObsStreaming = true;
        SendRequestAsync("StartStreaming");
    }

    public void StopStreaming()
    {
        if (!isObsStreaming)
            return;
        isObsStreaming = false;
        SendRequestAsync("StopStreaming");
    }

    public async void SendRequestAsync(string requestType)
    {
        using (var client = new ClientWebSocket())
        {
            try
            {
                await ConnectAsync(client);
                await SendAsync(client, new Message<Request<object>>(6, new Request<object>(requestType)));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    private async Task SendAsync<T>(ClientWebSocket client, Message<T> m)
    {
        await SendStringAsync(client, JsonConvert.SerializeObject(m));
    }

    private async Task SendStringAsync(ClientWebSocket client, string message)
    {
        var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        await client.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task<string> ReceiveAsync(ClientWebSocket client, int bufferSize = 1024)
    {
        var buffer = new ArraySegment<byte>(new byte[bufferSize]);
        var result = await client.ReceiveAsync(buffer, CancellationToken.None);
        return Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
    }

    private async Task ConnectAsync(ClientWebSocket client)
    {
        await client.ConnectAsync(new Uri(OBSWebSocketURI), CancellationToken.None)
            .ContinueWith(async (_) => await ReceiveAsync(client))
            .ContinueWith(async (_) => await SendAsync(client, new Message<Handshake>(1, new Handshake())))
            .ContinueWith(async (_) => await ReceiveAsync(client));
    }

    private class Message<T>
    {
        public int op { get; set; }
        public T d { get; set; }

        public Message(int op, T d)
        {
            this.op = op;
            this.d = d;
        }
    }

    private class Request<T>
    {
        public string requestType { get; set; }
        public string requestId { get; set; } = Guid.NewGuid().ToString();
        public T requestData { get; set; }

        public Request(string requestType, T requestData)
        {
            this.requestType = requestType;
            this.requestData = requestData;
        }

        public Request(string requestType)
        {
            this.requestType = requestType;
        }

        public bool ShouldSerializeData()
        {
            return requestData != null;
        }
    }

    private class Handshake
    {
        public int rpcVersion = 1;
    }
}
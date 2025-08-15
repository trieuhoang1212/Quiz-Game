using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;

public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }
    private SocketIOUnity socket;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        var uri = new Uri("http://localhost:8000");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string> { { "token", "UNITY" } },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) => Debug.Log("Connected to server from SocketManager");
        socket.OnError += (sender, e) => Debug.LogError($"Socket Error: {e}");
        socket.OnDisconnected += (sender, e) => Debug.Log($"Disconnected from server: {e}");


        socket.Off("finalResult");
        socket.On("finalResult", static (response) =>
        {
            var data = response.GetValue<Score.FinalResultData>();
            Debug.Log($"[SocketManager] Final Result - My Score: {data.myScore}, Opponent Score: {data.opponentScore}, Result: {data.result}");

            Score.FinalResultData finalResultData = new Score.FinalResultData
            {
                myScore = data.myScore,
                opponentScore = data.opponentScore,
                result = data.result
            };
        });

        socket.ConnectAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to connect to server: " + task.Exception);
            }
        });
    
        socket.ConnectAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to connect to server: " + task.Exception);
            }
        });
    }

    public SocketIOUnity Socket => socket;

    void OnDestroy()
    {
        if (Instance == this)
        {
            socket?.DisconnectAsync();
        }
    }
}
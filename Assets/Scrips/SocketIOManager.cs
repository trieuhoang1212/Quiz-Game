using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
        socket.On("finalResult", (response) =>
        {
            var data = response.GetValue<Manager.FinalResult>();
            Debug.Log($"[SocketManager] Final Result - My Score: {data.myScore}, Opponent Score: {data.opponentScore}, Result: {data.result}");

            Manager.GameResult.player1Score = data.myScore;
            Manager.GameResult.player2Score = data.opponentScore;
            Manager.GameResult.winner = data.result;

            SceneManager.LoadScene("Final");
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
using UnityEngine;
using SocketIOClient;
using System.Collections.Generic;
using System;

public class SocketIOManager : MonoBehaviour
{
    // Singleton quản lý Socket.IO cho toàn game
    public static SocketIOManager Instance { get; private set; }
    // Cho script khác dùng socket
    public SocketIOUnity socket;

    void Awake()
    {
    // Tạo singleton, giữ lại khi đổi scene
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
    // Khởi tạo socket và kết nối tới server
        var uri = new Uri("http://localhost:8000");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string> { { "token", "UNITY" } },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) => Debug.Log("Connected to server from SocketManager");
        socket.OnError += (sender, e) => Debug.LogError($"Socket Error: {e}");
        socket.OnDisconnected += (sender, e) => Debug.Log($"Disconnected from server: {e}");

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
    // Ngắt kết nối khi object bị hủy
    if (Instance == this)
        {
            socket?.DisconnectAsync();
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SocketIOClient;
using System.Collections.Generic;

public class Score : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    private SocketIOUnity socket;

    [Serializable]
    public class FinalResultData
    {
        public string result; // "WIN", "LOSE", "DRAW"
        public int myScore;
        public int opponentScore;
    }

    void Start()
    {
        var uri = new Uri("http://localhost:8000");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string> { { "token", "UNITY" } },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) => Debug.Log("Connected to server from Score");

        socket.On("finalResult", response =>
        {
            var data = response.GetValue<FinalResultData>();
            string result = data.result == "WIN" ? "Winner" : data.result == "LOSE" ? "Loser" : "Draw";
            resultText.text = $"Your Score: {data.myScore}\nOpponent Score: {data.opponentScore}\nResult: {result}";
            Debug.Log($"Game Result: {result}, Your Score: {data.myScore}, Opponent Score: {data.opponentScore}");
        });

        socket.ConnectAsync();
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.DisconnectAsync();
        }
    }
}



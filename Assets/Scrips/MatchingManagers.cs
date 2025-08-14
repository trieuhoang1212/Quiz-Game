using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class Matching : MonoBehaviour
{
    private SocketIOUnity socket;
    public Button startButton;
    public Button playButton;
    public TMP_Text resultText;
    public bool isDone = false;
    public string result;
    private bool isMatched = false;
    
    public class UserData
    {
        public string data { get; set; }
        public string result { get; set; }
        public int playerNumber { get; set; }
    }

    async void Start()
    {
        // Lúc đầu tắt nút Play
        playButton.interactable = false;
        
        socket = SocketManager.Instance.Socket;

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connected to the server");
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("Disconnected from the server");
        };

        // Khi server báo đã đủ 2 người
        socket.On("startGame", response =>
        {
            Debug.Log("Matching is completed!");
            isMatched = true;
            playButton.interactable = true; 
        });

        socket.On("result", response =>
        {
            Debug.Log("Game is over");
            var obj = response.GetValue<UserData>();
            Debug.Log("Game Result: " + obj.result);
            isDone = true;
            result = obj.result;
        });

        try
        {
            await socket.ConnectAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Connection Error: {ex.Message}\n{ex.StackTrace}");
        }

        
        startButton.onClick.AddListener(searchGame);
        playButton.onClick.AddListener(() =>
        {
            if (isMatched) // chỉ chơi khi đã match đủ người
            {
                playGame("Play");
                SceneManager.LoadScene("UI");
            }
            else
            {
                Debug.Log("Please wait, matching is not completed yet.");
            }
        });
    }

    void searchGame()
    {
        socket.Emit("searchGame");
        Debug.Log("Searching game...");
    }

    void Update()
    {
        if (isDone)
        {
            resultText.text = result;
            isDone = false;
        }
    }

    void playGame(string play)
    {
        socket.EmitAsync("play", play);
        Debug.Log($"Player chose completed");
    }
}

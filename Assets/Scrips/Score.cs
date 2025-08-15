using UnityEngine;
using TMPro;
using System;
using SocketIOClient;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Score : MonoBehaviour
{
    public static Score Instance;
    private int maxPlatformScore = 0;
    public TextMeshProUGUI PointSocre;
    public TextMeshProUGUI winnerText;

    [Serializable]
    public class FinalResultData
    {
        public string result;
        public int myScore;
        public int opponentScore;
    }

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
        var socket = SocketManager.Instance.Socket;

        socket.On("finalResult", response =>
        {
            Debug.Log($"Raw response: {response}");
            var data = response.GetValue<FinalResultData>();
            string result = data.result == "WIN" ? "Winner" : data.result == "LOSE" ? "Loser" : "Draw";
            maxPlatformScore = data.myScore;
            PlayerPrefs.SetInt("myScore", data.myScore);
            PlayerPrefs.SetInt("opponentScore", data.opponentScore);
            PlayerPrefs.SetString("result", result);
            PlayerPrefs.Save();

            PointSocre.text = $"Your Score: {data.myScore}\nOpponent Score: {data.opponentScore}\nResult: {result}";
            Winner(result);
            Debug.Log($"Game Result: {result}, Your Score: {data.myScore}, Opponent Score: {data.opponentScore}");

            SceneManager.LoadScene("Final");
        });

        // Lấy điểm từ Manager
        Manager gameManager = FindObjectOfType<Manager>();
        if (gameManager != null)
        {
            maxPlatformScore = gameManager.GetScore();
            Setup(maxPlatformScore);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }

        
    }

    public void Setup(int point)
    {
        gameObject.SetActive(true);
        maxPlatformScore = point;
        if (PointSocre != null)
        {
            PointSocre.text = "Score: " + maxPlatformScore;
            Debug.Log($"Score setup with score: {point}");
        }
        else
        {
            Debug.LogError("PointSocre is not assigned!");
        }
    }

    public async void MainMenu()
    {
        var socket = SocketManager.Instance.Socket;
        if (socket != null)
        {
            socket.Off("finalResult");
            await socket.DisconnectAsync();
            Debug.Log("Disconnected from server.");
        }

        // Xóa các instance cũ để UI Final không còn hiển thị
        Destroy(SocketManager.Instance.gameObject);
        Destroy(Manager.Instance.gameObject);
        Destroy(gameObject);

        SceneManager.LoadScene("Menu");
    }

    public void Winner(string result)
    {
        if (winnerText != null)
        {
            winnerText.text = result;
        }
        else
        {
            Debug.LogError("WinnerText is not assigned!");
        }
    }


}
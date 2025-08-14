using UnityEngine;
using TMPro;
using System;
using SocketIOClient;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Score : MonoBehaviour
{
    public static Score Instance;
    private int maxPlatformScore = 0;
    public TextMeshProUGUI PointSocre;

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
            PointSocre.text = $"Your Score: {data.myScore}\nOpponent Score: {data.opponentScore}\nResult: {result}";
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
}
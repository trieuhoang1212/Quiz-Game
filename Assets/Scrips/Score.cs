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
    public TextMeshProUGUI ResultText;
    private string lastResult;

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

        socket.On("finalResult", static response =>
        {
            var data = response.GetValue<FinalResultData>();

            string resultText = data.result == "WIN" ? "WINNER" :
                                data.result == "LOSE" ? "LOSER" : "DRAW";

            // Hiển thị điểm
            Score.Instance.ScoreFinal(data.myScore);

            // Hiển thị kết quả ở ResultText
            Score.Instance.SetResult(resultText);
        });


        // Lấy điểm từ Manager
        Manager gameManager = FindObjectOfType<Manager>();
        if (gameManager != null)
        {
            maxPlatformScore = gameManager.GetScore();
            ScoreFinal(maxPlatformScore);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }

    }

    public void ScoreFinal(int point)
    {
        gameObject.SetActive(true);
        maxPlatformScore = point;

        if (PointSocre != null)
        {
            PointSocre.text = $"Score: {maxPlatformScore}";
            Debug.Log($"Score setup with score: {point}");
        }
        else
        {
            Debug.LogError("PointSocre is not assigned!");
        }
    }
    public void SetResult(string result)
    {
        gameObject.SetActive(true);
        lastResult = result;
        Debug.Log($"SetResult called with: {result}");

        if (ResultText != null)
        {
            ResultText.text = lastResult;
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



}
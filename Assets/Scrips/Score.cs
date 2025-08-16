using UnityEngine;
using TMPro;
using System;
using SocketIOClient;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

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
        public int myScore;
        public int opponentScore;
        public string result;
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

        socket.On("finalResult", static (response) =>
        {
            string json = response.GetValue().ToString();
            Debug.Log("Raw finalResult: " + json);
            
            var data = JsonConvert.DeserializeObject<Score.FinalResultData>(json);

            Debug.Log($"Final Result - My Score: {data.myScore}, Opponent Score: {data.opponentScore}, Result: {data.result}");

            Score.FinalResultData finalResultData = new Score.FinalResultData
            {
                myScore = data.myScore,
                opponentScore = data.opponentScore,
                result = data.result
            };
            
            string resultText = finalResultData.result == "WIN" ? "WINNER" :
                                finalResultData.result == "LOSE" ? "LOSER" : "DRAW";

            // Hiển thị điểm
            Score.Instance.ScoreFinal(finalResultData.myScore);

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

        if (ResultText != null)
        {
            ResultText.text = lastResult;
            Debug.Log("UI ResultText updated: " + lastResult);
        }
        else
        {
            Debug.LogError("ResultText is NULL in SetResult!");
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
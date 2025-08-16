using UnityEngine;
using TMPro;
using System;
using SocketIOClient;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Score : MonoBehaviour
{
    public static Score Instance;
    private int maxPlatformScore = 0;
    public TextMeshProUGUI PointSocre;
    public TextMeshProUGUI ResultText;
    private string lastResult;

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
        Manager ResultManager = FindObjectOfType<Manager>();
        Manager ScoreManager = FindObjectOfType<Manager>();
        if (ResultManager != null)
        {
            int finalScore = ScoreManager.GetCurrentScore();
            string finalresult = ResultManager.GetFinalResult();
            ScoreFinal(finalScore);
            SetResult(finalresult);
            
        }
        else
        {
            Debug.LogError("ResultManager is NULL in Start!");
            ResultText.text = "Waiting for result...";
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
        Debug.Log("SetResult called with: " + result);
        gameObject.SetActive(true);
        lastResult = result;

        if (ResultText != null)
        {
            ResultText.text = lastResult;
            Debug.Log("UI ResultText updated: " + lastResult);
        }
    }

    public async void MainMenu()
    {
        var socket = SocketManager.Instance.Socket;
        if (socket != null)
        {
            socket.Off("gameResult");
            await socket.DisconnectAsync();
            Debug.Log("Disconnected from server.");
        }

        Destroy(SocketManager.Instance.gameObject);
        Destroy(Manager.Instance.gameObject);
        Destroy(gameObject);

        SceneManager.LoadScene("Menu");
    }
}
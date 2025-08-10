using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SocketIOClient;
using TMPro;
using UnityEngine.Playables;
using Unity.VisualScripting;

public class Manager : MonoBehaviour
{
    private SocketIOUnity socket;
    public Question[] questions;
    private static List<Question> Questions;
    private Question currentQuestion;

    private static int score = 0;

    [SerializeField] private int maxEasyQuestions = 2;
    [SerializeField] private int maxHardQuestions = 2;
    [SerializeField] private Text factText;
    [SerializeField] private Text trueAnswerText;
    [SerializeField] private Text falseAnswerText;
    [SerializeField] private Text scoreText;
    [SerializeField] private float timeBetweenQuestions = 1f;
    [SerializeField] private float TimeSkipQuestion = 5f;
    private float currentTime;
    private bool isQuestionActive = false;
    [SerializeField] private Animator animator;

    [System.Serializable]
    public class FinalResult
    {
        public int player1Score;
        public int player2Score;
        public int winner; // 0: hòa, 1: player1 thắng, 2: player2 thắng
    }

    public static class GameResult
    {
        public static int player1Score;
        public static int player2Score;
        public static int winner;
    }

    void Awake()
    {
        // Đảm bảo GameManager không bị hủy khi reload scene
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Kiểm tra xem đã có socket hay chưa
        var uri = new System.Uri("http://localhost:8000");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string> { { "token", "UNITY" } },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connected to the server");
            socket.Emit("searchGame"); // Tự động tìm game khi kết nối
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("Disconnected from the server");
        };

        socket.On("finalResult", (response) =>
        {
            var data = response.GetValue<FinalResult>();
            Debug.Log($"Player 1: {data.player1Score}, Player 2: {data.player2Score}, Winner: {data.winner}");

            GameResult.player1Score = data.player1Score;
            GameResult.player2Score = data.player2Score;
            GameResult.winner = data.winner;

            SceneManager.LoadScene("Final"); // Chuyển sang scene kết quả
        });

        socket.ConnectAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to connect to server: " + task.Exception);
            }
            else
            {
                Debug.Log("Connection attempt completed");
            }
        });

        currentTime = TimeSkipQuestion;

        if (Questions == null || Questions.Count == 0)
        {
            score = 0;
            Questions = RandomQuestion();
        }

        if (Questions.Count > 0)
        {
            currentQuestion = Questions[0];
            CurrentQuestion();
            UpdateScoreText();
        }
    }

    void Update()
    {
        if (isQuestionActive)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                StartCoroutine(TransitionToNextQuestion());
                currentTime = TimeSkipQuestion;
            }
        }
    }

    List<Question> RandomQuestion()
    {
        // Lọc câu hỏi theo độ khó và random
        var easy = questions.Where(q => q.difficulty == DifficultyLevel.Easy).OrderBy(x => Random.value).Take(maxEasyQuestions);
        var hard = questions.Where(q => q.difficulty == DifficultyLevel.Hard).OrderBy(x => Random.value).Take(maxHardQuestions);

        return easy.Concat(hard).OrderBy(x => Random.value).ToList();
    }

    void CurrentQuestion()
    {
        if (currentQuestion != null)
        {
            factText.text = currentQuestion.fact;
            trueAnswerText.text = currentQuestion.isTrue ? "Correct!" : "Wrong!";
            falseAnswerText.text = currentQuestion.isTrue ? "Wrong!" : "Correct!";
            isQuestionActive = true;
        }
    }

    int GetPointsForQuestion(Question q)
    {
        return q.difficulty == DifficultyLevel.Hard ? 40 : 20;
    }

    IEnumerator TransitionToNextQuestion()
{
    if (Questions == null || Questions.Count == 0)
    {
        Debug.LogWarning("Không còn câu hỏi để chuyển!");
        socket.EmitAsync("sendScore", score);
        SceneManager.LoadScene("Final");
        yield break;
    }

    Questions.RemoveAt(0); // Xóa câu hỏi đầu tiên
    Debug.Log($"Số câu hỏi còn lại: {Questions.Count}");
    yield return new WaitForSeconds(timeBetweenQuestions);

    if (Questions.Count == 0)
    {
        Debug.Log("Hoàn thành quiz!");
        Debug.Log($"Điểm cuối: {score}");
        socket.EmitAsync("sendScore", score);
        SceneManager.LoadScene("Final");
    }
    else
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    currentTime = TimeSkipQuestion;
    isQuestionActive = true;
}

public void UserSelectTrue()
{
    animator.SetTrigger("True");
    if (currentQuestion.isTrue)
    {
        score += GetPointsForQuestion(currentQuestion);
        Debug.Log("Correct!");
    }
    else
    {
        Debug.Log("Wrong!");
    }

    socket.EmitAsync("answerResult", new
    {
        playerId = socket.Id,
        score
    });

    UpdateScoreText();
    StartCoroutine(TransitionToNextQuestion());
}

public void UserSelectFalse()
{
    if (currentQuestion == null || !isQuestionActive)
    {
        Debug.LogWarning("No question available or question is not active!");
        return;
    }

    animator.SetTrigger("False");
    if (currentQuestion.isTrue)
    {
        score += GetPointsForQuestion(currentQuestion);
        Debug.Log("Correct!");
    }
    else
    {
        Debug.Log("Wrong!");
    }

    socket.EmitAsync("answerResult", new
    {
        playerId = socket.Id,
        score
    });

    UpdateScoreText();
    StartCoroutine(TransitionToNextQuestion());
}

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    void OnDestroy()
    {
        // Chỉ ngắt kết nối khi game thực sự kết thúc
        if (socket != null && SceneManager.GetActiveScene().name != "Final")
        {
            socket.DisconnectAsync();
            Debug.Log("Socket disconnected on destroy");
        }
    }
}
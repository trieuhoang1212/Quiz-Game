using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SocketIOClient;
using TMPro;

public class Manager : MonoBehaviour
{
    public static Manager Instance;
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
    private bool finalSceneLoaded = false;
    [SerializeField] private Animator animator;

    [System.Serializable]
    public class FinalResult
    {
        public int result; // 0: hòa, 1: player1 thắng, 2: player2 thắng
        public int myScore;
        public int opponentScore; 
    }

    public static class GameResult
    {
        public static int player1Score;
        public static int player2Score;
        public static int winner;
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
        socket.Off("finalResult"); // tránh lặp listener

        socket.On("finalResult", async (response) =>
        {
            if (finalSceneLoaded) return;
            finalSceneLoaded = true;

            var data = response.GetValue<FinalResult>();
            Debug.Log($"My Score: {data.myScore}, Opponent Score: {data.opponentScore}, Result: {data.result}");

            GameResult.player1Score = data.myScore;
            GameResult.player2Score = data.opponentScore;
            GameResult.winner = data.result;

            await socket.DisconnectAsync();
            SceneManager.LoadScene("Final");
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
        var easy = questions.Where(q => q.difficulty == DifficultyLevel.Easy).OrderBy(x => Random.value).Take(maxEasyQuestions);
        var hard = questions.Where(q => q.difficulty == DifficultyLevel.Hard).OrderBy(x => Random.value).Take(maxHardQuestions);
        return easy.Concat(hard).OrderBy(x => Random.value).ToList();
    }

    void CurrentQuestion()
    {
        ResetAnimations();
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
        if (Questions != null && Questions.Count > 0)
        {
            Questions.RemoveAt(0);
            Debug.Log($"Số câu hỏi còn lại: {Questions.Count}");
        }
        else
        {
            Debug.LogWarning("Danh sách câu hỏi rỗng hoặc null!");
            yield break;
        }

        yield return new WaitForSeconds(timeBetweenQuestions);

        if (Questions == null || Questions.Count == 0)
        {
            Debug.Log("Hoàn thành quiz!");
            Debug.Log($"Điểm cuối: {score}");
            SceneManager.LoadScene("Final");

            isQuestionActive = false;

            var socket = SocketManager.Instance.Socket;
            socket.EmitAsync("submitScore", new { score = score }); // Sử dụng submitScore thay vì sendScore

            Score scoreManager = Score.Instance;
            if (scoreManager != null)
            {
                scoreManager.Setup(score);
            }
            yield break;
        }

        else
        {
            currentQuestion = Questions[0];
            CurrentQuestion();
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
        var socket = SocketManager.Instance.Socket;
        socket.EmitAsync("answerResult", new { playerId = socket.Id, score });

        UpdateScoreText();
        StartCoroutine(TransitionToNextQuestion());
    }

    public void UserSelectFalse()
    {
        animator.SetTrigger("False");
        if (!currentQuestion.isTrue)
        {
            score += GetPointsForQuestion(currentQuestion);
            Debug.Log("Correct!");
        }
        else
        {
            Debug.Log("Wrong!");
        }
        var socket = SocketManager.Instance.Socket;
        socket.EmitAsync("answerResult", new { playerId = socket.Id, score });

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

    public int GetScore()
    {
        return score;
    }

    void ResetAnimations()
    {
        animator.ResetTrigger("True");
        animator.ResetTrigger("False");
        animator.Play("NoAnswer");
}


}
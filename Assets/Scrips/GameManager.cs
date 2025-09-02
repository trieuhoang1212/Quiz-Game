using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Concurrent;

public class GameManager : MonoBehaviour
{
    // Singleton của GameManager
    public static GameManager Instance;
    // Danh sách câu hỏi nguồn (gán từ Inspector)
    public Question[] questions;
    // Hàng câu hỏi đã trộn theo độ khó
    private static List<Question> Questions;
    // Câu hỏi hiện tại
    private Question currentQuestion;
    // Hàng đợi hành động để chạy trên main thread (cho callback socket)
    private static readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    private static void EnqueueMain(Action a) { if (a != null) _mainThreadQueue.Enqueue(a); }

    // Điểm hiện tại & kết quả cuối cùng (WINNER/LOSER/DRAW)
    private static int score = 0;
    private static string result;

    // Giới hạn số câu theo độ khó
    [SerializeField] private int maxEasyQuestions = 2;
    [SerializeField] private int maxHardQuestions = 2;
    // Tham chiếu UI
    [SerializeField] private Text factText;
    [SerializeField] private Text trueAnswerText;
    [SerializeField] private Text falseAnswerText;
    [SerializeField] private Text scoreText;
    // Thời gian chuyển câu & thời gian tự bỏ qua câu
    [SerializeField] private float timeBetweenQuestions = 1f;
    [SerializeField] private float TimeSkipQuestion = 5f;
    private float currentTime;
    private bool isQuestionActive = false; // Cờ đang hiển thị câu hỏi
    // Chỉ nhận 1 lần chọn/ câu và chống chuyển câu trùng
    private bool hasAnswered = false;
    private bool isTransitioning = false;
    [SerializeField] private Animator animator;

    void Awake()
    {
        // Tạo singleton, giữ qua scene
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
    // Socket (nếu có) để nhận kết quả cuối từ server
    var socket = SocketIOManager.Instance != null ? SocketIOManager.Instance.Socket : null;

        currentTime = TimeSkipQuestion;

    // Nếu mới vào hoặc đã hết câu -> trộn lại bộ câu hỏi
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

    // Đăng ký sự kiện kết quả cuối trận từ server
        if (socket != null)
        {
            socket.On("gameResult", (response) =>
            {
                EnqueueMain(() =>
                {
                    Debug.Log("gameResult event received!");
                    try
                    {
                        var data = response.GetValue<FinalResultData>();
                        Debug.Log($"Final Result - My Score: {data.myScore}, Opponent Score: {data.opponentScore}, Result: {data.result}");

                        string resultText = data.result == "WIN" ? "WINNER" :
                                           data.result == "LOSE" ? "LOSER" : "DRAW";

                        Score scoreManager = Score.Instance;
                        SetFinalResult(resultText);
                        if (scoreManager != null)
                        {
                            scoreManager.ScoreFinal(data.myScore);
                            scoreManager.SetResult(resultText);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to parse gameResult: {ex.Message}");
                    }
                });
            });
        }
    }

    void Update()
    {
        // Rút và chạy các action cần chạy trên main thread
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action(); } catch (Exception ex) { Debug.LogException(ex); }
        }

        if (isQuestionActive)
        {
            // Đếm ngược tự chuyển câu nếu không trả lời
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
        // Lấy ngẫu nhiên theo độ khó và trộn toàn bộ
        var easy = questions.Where(q => q.difficulty == DifficultyLevel.Easy).OrderBy(x => UnityEngine.Random.value).Take(maxEasyQuestions);
        var hard = questions.Where(q => q.difficulty == DifficultyLevel.Hard).OrderBy(x => UnityEngine.Random.value).Take(maxHardQuestions);
        return easy.Concat(hard).OrderBy(x => UnityEngine.Random.value).ToList();
    }

    void CurrentQuestion()
    {
        ResetAnimations();
        if (currentQuestion != null)
        {
            // Gán UI theo câu hỏi hiện tại
            factText.text = currentQuestion.fact;
            trueAnswerText.text = currentQuestion.isTrue ? "Correct!" : "Wrong!";
            falseAnswerText.text = currentQuestion.isTrue ? "Wrong!" : "Correct!";
            // Reset trạng thái cho câu mới
            hasAnswered = false;
            isTransitioning = false;
            isQuestionActive = true;
        }
    }

    int GetPointsForQuestion(Question q)
    {
        // Hard: 40 điểm, Easy: 20 điểm
        return q.difficulty == DifficultyLevel.Hard ? 40 : 20;
    }

    IEnumerator TransitionToNextQuestion()
    {
        // Chống gọi chuyển câu nhiều lần
        if (isTransitioning)
        {
            yield break;
        }
        isTransitioning = true;
        if (Questions != null && Questions.Count > 0)
        {
            // Bỏ câu đã trả lời/đã hết thời gian
            Questions.RemoveAt(0);
            Debug.Log($"Số câu hỏi còn lại: {Questions.Count}");
        }
        else
        {
            Debug.LogWarning("Danh sách câu hỏi rỗng hoặc null!");
        }

        // Delay ngắn trước khi sang câu kế
        yield return new WaitForSeconds(timeBetweenQuestions);

        if (Questions == null || Questions.Count == 0)
        {
            // Hết câu -> sang màn Final
            Debug.Log("Hoàn thành quiz!");
            Debug.Log($"Điểm cuối: {score}");
            SceneManager.LoadScene("Final");

            isQuestionActive = false;
            GetScore(score);
            GetResult(result);
            yield break;
        }
        else
        {
            currentQuestion = Questions[0];
            CurrentQuestion();
        }
        // Reset đồng hồ và trạng thái cho câu mới
        currentTime = TimeSkipQuestion;
        isQuestionActive = true;
        // Cho phép chọn lại cho câu mới
        hasAnswered = false;
        isTransitioning = false;
    }

    public void UserSelectTrue()
    {
        // Chặn nhấn nhiều lần / khi không ở trạng thái trả lời
        if (!isQuestionActive || hasAnswered || isTransitioning) return;
        hasAnswered = true;
        isQuestionActive = false;
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
        UpdateScoreText();
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToNextQuestion());
        }
    }

    public void UserSelectFalse()
    {
        // Chặn nhấn nhiều lần / khi không ở trạng thái trả lời
        if (!isQuestionActive || hasAnswered || isTransitioning) return;
        hasAnswered = true;
        isQuestionActive = false;
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
        UpdateScoreText();
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToNextQuestion());
        }
    }

    void UpdateScoreText()
    {
        // Cập nhật UI điểm
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    public int GetScore(int score)
    {
        // Gửi điểm lên server (nếu có) và cập nhật UI Final
        var socket = SocketIOManager.Instance != null ? SocketIOManager.Instance.Socket : null;
        if (socket != null)
        {
            socket.EmitAsync("submitScore", new { score = score });
        }

        Score scoreManager = Score.Instance;
        if (scoreManager != null)
        {
            scoreManager.ScoreFinal(score);
        }
        return score;
    }

    public string GetResult(string result)
    {
    // Cập nhật kết quả cuối vào UI Final (nếu đã có)
        Score scoreManager = Score.Instance;
        if (scoreManager != null)
        {
            scoreManager.SetResult(result);
        }
        return result;
    }

    public void SetFinalResult(string resultF)
    {
    // Lưu kết quả cuối cùng (WINNER/LOSER/DRAW)
        result = resultF;
    }

    public string GetFinalResult()
    {
    // Lấy kết quả cuối để hiển thị ở Final
        return result;
    }

    public int GetCurrentScore()
    {
    // Lấy điểm hiện tại
        return score;
    }

    void ResetAnimations()
    {
    // Reset các trigger animation về trạng thái mặc định
        animator.ResetTrigger("True");
        animator.ResetTrigger("False");
        animator.Play("NoAnswer");
    }

    [System.Serializable]
    public class FinalResultData
    {
    // Dữ liệu phản hồi từ server khi kết thúc trận
        public int myScore { get; set; }
        public int opponentScore { get; set; }
        public string result { get; set; }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Score : MonoBehaviour
{
    // Singleton của Score
    public static Score Instance;
    // Điểm cuối để hiển thị ở màn Final
    private int maxPlatformScore = 0;
    // UI điểm & kết quả
    public TextMeshProUGUI PointSocre;
    public TextMeshProUGUI ResultText;
    private string lastResult;

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
    // Lấy điểm & kết quả từ GameManager khi vào scene Final
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            int finalScore = gm.GetCurrentScore();
            string finalresult = gm.GetFinalResult();
            ScoreFinal(finalScore);
            SetResult(finalresult);
            
        }
        else
        {
            Debug.LogError("ResultManager is NULL in Start!");
            if (ResultText != null) ResultText.text = "Waiting for result...";
        }
    }

    public void ScoreFinal(int point)
    {
    // Cập nhật UI điểm cuối
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
    // Cập nhật UI kết quả cuối (WINNER/LOSER/DRAW)
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
    // Trở về Menu: ngắt socket, dọn singleton, load scene Menu
        var socket = SocketIOManager.Instance != null ? SocketIOManager.Instance.Socket : null;
        if (socket != null)
        {
            socket.Off("gameResult");
            await socket.DisconnectAsync();
            Debug.Log("Disconnected from server.");
        }

        if (SocketIOManager.Instance != null)
        {
            Destroy(SocketIOManager.Instance.gameObject);
        }
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
        Destroy(gameObject);

        SceneManager.LoadScene("Menu");
    }
}
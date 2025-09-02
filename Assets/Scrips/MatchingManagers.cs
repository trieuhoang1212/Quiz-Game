using UnityEngine;
using SocketIOClient;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MatchingManagers : MonoBehaviour
{
    // Kết nối socket để ghép trận
    private SocketIOUnity socket;
    // Nút tìm trận và nút vào chơi
    public Button startButton;
    public Button playButton;
    // Text hiển thị tạm
    public TMP_Text resultText;
    public bool isDone = false;
    public string result;
    private bool isMatched = false;
    private bool listenersBound = false;

    public class UserData
    {
        public string data { get; set; }
        public string result { get; set; }
        public int playerNumber { get; set; }
    }

    void Start()
    {
    // Lúc đầu tắt nút Play
        playButton.interactable = false;

    // Lấy socket từ manager
    socket = SocketIOManager.Instance.Socket;

        if (!listenersBound)
        {
            socket.OnConnected += (sender, e) =>
            {
                Debug.Log("Connected to the server (Matching)");
            };

            socket.OnDisconnected += (sender, e) =>
            {
                Debug.Log("Disconnected from the server (Matching)");
            };

            // Khi server báo đã đủ 2 người
            socket.On("startGame", response =>
            {
                Debug.Log("Matching is completed!");
                var data = response.GetValue<UserData>();
                isMatched = true;
                playButton.interactable = true;
                Debug.Log($"Assigned as Player {data.playerNumber}");
            });

            socket.On("playerSearching", response =>
            {
                var data = response.GetValue<UserData>();
                Debug.Log($"Searching... You are in queue as Player {data.playerNumber}");
            });

            socket.On("matchError", response =>
            {
                var msg = response.GetValue().ToString();
                Debug.LogWarning($"Match error: {msg}");
            });

            socket.On("gameReset", response =>
            {
                Debug.Log("Server reset the game, you can search again.");
                isMatched = false;
                playButton.interactable = false;
            });
            listenersBound = true;
        }

    // Gắn sự kiện nút
    startButton.onClick.AddListener(searchGame);
        playButton.onClick.AddListener(() =>
        {
            if (isMatched)
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
    // Gửi yêu cầu tìm trận lên server
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
    // Báo server là đã sẵn sàng vào chơi
        socket.Emit("play", play);
        Debug.Log($"Player chose completed");
        
    }

}

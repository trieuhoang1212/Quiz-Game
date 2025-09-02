using UnityEngine;
// SocketIOClient not needed here

[System.Serializable]

public class Question
{
    // Câu hỏi/ phát biểu
    public string fact;
    // Đáp án đúng/sai
    public bool isTrue;
    // Độ khó của câu
    public DifficultyLevel difficulty; // Thêm độ khó
}

public enum DifficultyLevel
{
    Easy,
    Hard
}

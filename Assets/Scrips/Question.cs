using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;

[System.Serializable]

public class Question
{
    public string fact;
    public bool isTrue;
    public DifficultyLevel difficulty; // Thêm độ khó
}

public enum DifficultyLevel
{
    Easy,
    Hard
}

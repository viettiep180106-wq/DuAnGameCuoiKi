using System;

public enum GamePlayState
{
    Lobby,          // Trạng thái 1: Chuẩn bị / Sảnh
    IntroBanner,    // Trạng thái 2: Chuyển cảnh / Intro màn chơi
    MatchBoard,     // Trạng thái 3: Bắt đầu bàn chơi (ghép đá)
    Combat,         // Trạng thái 4: Gameplay chiến đấu
    InterWaveReward,// Cơ chế thưởng phạt (Buff / Debuff) giữa các wave
    GameOverWin,    // Hoàn thành Level
    GameOverLose    // Thất bại
}

public static class GameStateEvents
{
    public static Action<GamePlayState> OnStateChanged;
    public static Action<int> OnWaveChanged;
    public static Action<int, int> OnCoinChanged; // (current, delta)
}


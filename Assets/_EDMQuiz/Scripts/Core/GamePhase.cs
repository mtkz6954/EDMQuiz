namespace EDMQuiz
{
    public enum GamePhase
    {
        Idle,           // タイトル画面・未開始
        Question,       // 問題文表示
        BuildUp,        // BGM 0 → 36.644s。入力ロック・カウントダウン表示
        AnswerWindow,   // 4 拍だけ入力 ON・スポットライト ON
        Drop,           // 正誤判定 + 結果 VFX + 余韻
        FadeOut,        // BGM をゆっくりフェードアウト
        Next,           // 次問題への遷移
        GameEnd         // 結果画面
    }
}

namespace EDMQuiz
{
    public enum GamePhase
    {
        Idle,       // タイトル画面・未開始
        Question,   // 問題文表示
        BuildUp,    // 入力受付
        Drop,       // 正誤判定 + 演出
        Next,       // 次問題への遷移
        GameEnd     // 結果画面
    }
}

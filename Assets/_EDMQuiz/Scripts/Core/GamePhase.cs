namespace EDMQuiz
{
    public enum GamePhase
    {
        Idle,          // ゲーム開始前（タイトル画面）
        Question,      // 問題文を表示する期間
        BuildUp,       // 入力可能期間（ビルドアップ中）
        Drop,          // 正誤判定・演出期間（ドロップ後）
        ResultReveal,  // 判定結果の演出表示期間
        Next,          // 次問題への遷移期間
        GameEnd        // 全5問終了・結果画面表示
    }
}

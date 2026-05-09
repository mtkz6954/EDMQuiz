namespace EDMQuiz
{
    public static class GameConstants
    {
        // ── ゲーム進行 ──────────────────────────────
        public const int   TOTAL_QUESTIONS      = 5;
        public const int   ANSWER_LENGTH        = 4;
        public const int   MIN_OPTIONS          = 5;
        public const int   MAX_OPTIONS          = 10;

        public const float QUESTION_PHASE_SEC   = 0f;
        public const float DROP_REVEAL_SEC      = 4.0f;
        public const float NEXT_TRANSITION_SEC  = 1.5f;

        // ── ドロップ同期 ────────────────────────────
        // BGM の elapsed 秒数で answer window を開く時刻（BGM のドロップポイント）
        public const float BUILDUP_MUSIC_SEC    = 36.644f;
        // 回答可能ウィンドウの拍数（8 拍）
        public const int   ANSWER_WINDOW_BEATS  = 8;
        // 判定後 BGM を流したまま結果を見せる秒数
        public const float RESULT_HOLD_SEC      = 10.0f;
        // BGM をゆっくりフェードアウトする秒数
        public const float BGM_FADE_OUT_SEC     = 5.0f;
        // 次の問題への画面フェードイン秒数（黒→明）
        public const float SCENE_FADE_IN_SEC    = 0.5f;

        // ── BPM 同期 ────────────────────────────────
        public const float BPM            = 152f;
        public const int   BEATS_PER_BAR  = 4;
        public const int   BUILDUP_BARS   = 16;

        public static float GetBeatDuration()       => 60f / BPM;
        public static float GetBuildUpDurationSec() => BUILDUP_BARS * BEATS_PER_BAR * GetBeatDuration();
        public static float GetAnswerWindowSec()    => ANSWER_WINDOW_BEATS * GetBeatDuration();

        // ── VFX ────────────────────────────────────
        public const float CORRECT_SCALE_PEAK           = 1.2f;
        public const float CORRECT_SCALE_DURATION       = 0.3f;
        public const float BEAT_PULSE_DURATION_RATIO    = 0.2f;
        public const float QUESTION_PULSE_SCALE         = 1.05f;
        public const float BLUE_OVERLAY_ALPHA           = 0.6f;
        public const float INCORRECT_FREEZE_SEC          = 0.1f;
        public const float INCORRECT_OVERLAY_DELAY_SEC  = 0.5f;
        public const float INCORRECT_OVERLAY_FADE_SEC   = 0.2f;
        public const float INCORRECT_OVERLAY_TOTAL_SEC  = INCORRECT_OVERLAY_DELAY_SEC + INCORRECT_OVERLAY_FADE_SEC;

        // 不正解時のコケアニメ／ラベル
        public const float TUMBLE_ROTATE_DEG          = 90f;   // 横倒し角度
        public const float TUMBLE_FALL_PX             = 50f;   // 下方向への沈み込み (px)
        public const float TUMBLE_DURATION            = 0.45f; // コケるまでの所要秒
        public const float INCORRECT_LABEL_SCALE_PEAK = 1.15f;
        public const float INCORRECT_LABEL_FADE_DUR   = 0.35f;
        public const float INCORRECT_LABEL_HOLD_SEC   = 1.8f;

        // 正解時の最高潮演出 — 初期バースト
        public const float CORRECT_FLASH_ALPHA         = 0.85f;
        public const float CORRECT_FLASH_IN_SEC        = 0.05f;
        public const float CORRECT_FLASH_OUT_SEC       = 0.35f;
        public const float CORRECT_SHAKE_PX            = 22f;
        public const float CORRECT_SHAKE_DUR           = 0.5f;
        public const int   CORRECT_SHAKE_VIBRATO       = 18;
        public const float CHARACTER_SPIN_DEG          = 720f;  // 2 周
        public const float CHARACTER_SPIN_DUR          = 1.0f;
        public const float CHARACTER_HYPE_BOUNCE_PX    = 45f;   // 大ジャンプ (px)
        public const float UI_HYPE_SCALE_PEAK          = 1.12f;
        public const float UI_HYPE_DURATION            = 0.25f;

        // ── 正解 Drop 中の継続ハイプダンス ──
        public const float HYPE_DANCE_BOUNCE_PX        = 36f;
        public const float HYPE_DANCE_TILT_DEG         = 22f;
        public const int   HYPE_FLIP_INTERVAL          = 2;
        public const float HYPE_UI_SCALE_BIG           = 1.14f;
        public const float HYPE_UI_SCALE_SMALL         = 1.07f;
        public const float HYPE_UI_TILT_DEG            = 6f;
        public const float CORRECT_LABEL_DANCE_PEAK    = 1.55f; // pop-in 後の更にスケール
        public const float CORRECT_LABEL_BASE_SCALE    = 1.2f;  // pop-in の到達点
        public const float CORRECT_LABEL_TILT_DEG      = 10f;
        public const float CORRECT_LABEL_BOUNCE_PX     = 24f;

        // ── ムービングスポットライト (Light2D) ──
        public const int   SPOTLIGHT_COUNT             = 6;
        public const float SPOTLIGHT_FIGURE8_SCALE     = 6.8f;
        public const float SPOTLIGHT_BARS_PER_LAP      = 1.5f;
        public const float SPOTLIGHT_INTENSITY         = 0.8f;
        public const float SPOTLIGHT_OUTER_RADIUS      = 7.5f;
        public const float SPOTLIGHT_INNER_RADIUS      = 0.5f;
        public const float SPOTLIGHT_FALLOFF           = 0.7f;

        // ── 正解時 (Hype) ムービングスポットライト ──
        public const float SPOTLIGHT_HYPE_INTENSITY_MIN  = 0.8f;
        public const float SPOTLIGHT_HYPE_INTENSITY_MAX  = 2.5f;
        public const float SPOTLIGHT_HYPE_PULSE_HZ       = 4f;     // サイン波振動 [回/秒]
        public const float SPOTLIGHT_HYPE_FIGURE8_SCALE  = 9.5f;   // 軌道振幅 (通常より拡大)
        public const float SPOTLIGHT_HYPE_BARS_PER_LAP   = 0.75f;  // 通常の半分の小節 = 倍速周回
        public const float SPOTLIGHT_HYPE_ROTATE_HZ      = 0.75f;  // 軌道全体の回転速度 [回/秒]
        public const float SPOTLIGHT_HYPE_YOYO_AMPLITUDE = 5f;     // 左右ヨーヨー振幅
        public const float SPOTLIGHT_HYPE_YOYO_HZ        = 1.2f;   // 左右ヨーヨー周波数 [回/秒]

        // ── キャラクターダンス ────────────────────
        public const float DANCE_BOUNCE_PX        = 14f;   // 上方向のバウンス量 (px)
        public const float DANCE_TILT_DEG         = 8f;    // 左右の傾き (度)
        public const int   DANCE_FLIP_INTERVAL    = 4;     // 何拍ごとに反転するか

        // ── 入力 UI ────────────────────────────────
        public const float BUTTON_PULSE_SCALE           = 1.08f;
        public const float BUTTON_PULSE_DURATION_RATIO  = 0.2f;
        public const int   BUTTON_GRID_COLUMNS          = 3;
        public const float ANSWER_NOW_PULSE_SCALE       = 1.16f;
        public const float ANSWER_NOW_PULSE_BEATS       = 0.7f;

        // ── 問題文表示演出 ─────────────────────────
        public const float QUESTION_FADE_IN_DURATION  = 0.35f;
        public const float QUESTION_FADE_IN_OFFSET_PX = -60f;

        // ── スコア ─────────────────────────────────
        public const int   RANK_S = 90;
        public const int   RANK_A = 70;
        public const int   RANK_B = 50;
        public const int   RANK_C = 30;
        public const float SCORE_COUNTUP_DURATION = 1.5f;
        public const float RANK_SCALE_PEAK        = 1.2f;
        public const float RANK_SCALE_DURATION    = 0.5f;
    }
}

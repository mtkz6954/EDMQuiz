namespace EDMQuiz
{
    public static class GameConstants
    {
        // ── ゲーム進行 ──────────────────────────────
        public const int   TOTAL_QUESTIONS      = 5;
        public const int   ANSWER_LENGTH        = 4;
        public const int   MIN_OPTIONS          = 5;
        public const int   MAX_OPTIONS          = 8;

        public const float QUESTION_PHASE_SEC   = 2.0f;
        public const float DROP_REVEAL_SEC      = 4.0f;
        public const float NEXT_TRANSITION_SEC  = 1.5f;

        // ── BPM 同期 ────────────────────────────────
        public const float BPM            = 128f;
        public const int   BEATS_PER_BAR  = 4;
        public const int   BUILDUP_BARS   = 16;

        public static float GetBeatDuration()       => 60f / BPM;
        public static float GetBuildUpDurationSec() => BUILDUP_BARS * BEATS_PER_BAR * GetBeatDuration();

        // ── VFX ────────────────────────────────────
        public const float SHAKE_STRENGTH               = 10f;
        public const int   SHAKE_VIBRATO                = 20;
        public const float SHAKE_DURATION               = 0.4f;
        public const float CORRECT_SCALE_PEAK           = 1.2f;
        public const float CORRECT_SCALE_DURATION       = 0.3f;
        public const float BEAT_PULSE_SCALE             = 1.03f;
        public const float BEAT_PULSE_DURATION_RATIO    = 0.2f;
        public const float BLUE_OVERLAY_ALPHA           = 0.6f;

        // ── 入力 UI ────────────────────────────────
        public const float BUTTON_PULSE_SCALE           = 1.08f;
        public const float BUTTON_PULSE_DURATION_RATIO  = 0.2f;
        public const int   BUTTON_GRID_COLUMNS          = 4;

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

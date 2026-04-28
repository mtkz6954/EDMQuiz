namespace EDMQuiz
{
    public static class GameConstants
    {
        public const int   TOTAL_QUESTIONS      = 5;
        public const int   MAX_INPUT_LENGTH     = 4;
        public const float QUESTION_PHASE_SEC   = 2.0f;
        public const float BUILDUP_PHASE_SEC    = 24.0f;
        public const float DROP_REVEAL_SEC      = 4.0f;
        public const float RESULT_REVEAL_SEC    = 2.0f;
        public const float NEXT_TRANSITION_SEC  = 1.5f;

        // BPM同期
        public const float BPM                  = 128f;
        public const float BUILDUP_START_SEC    = 8.0f;
        public const float DROP_START_SEC       = 32.0f;

        // VFX
        public const float SCREEN_SHAKE_STRENGTH      = 10f;
        public const float SCREEN_SHAKE_DURATION      = 0.3f;
        public const float BLUE_OVERLAY_ALPHA         = 0.4f;
        public const float FREEZE_DURATION            = 0.1f;
        public const float BUTTON_PULSE_SCALE         = 0.12f;
        public const float BUTTON_PULSE_DURATION_RATIO = 0.4f;

        // ランク閾値
        public const int RANK_S = 90;
        public const int RANK_A = 70;
        public const int RANK_B = 50;
        public const int RANK_C = 30;
    }
}

using R3;
using UnityEngine;

namespace EDMQuiz
{
    /// <summary>正解数の集計とランク判定。AnswerJudgment.OnJudged を購読して自動加算</summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private readonly ReactiveProperty<int> _correctCount = new(0);
        public ReadOnlyReactiveProperty<int> CorrectCount => _correctCount;

        public int ExcitementScore =>
            (_correctCount.Value * 100) / GameConstants.TOTAL_QUESTIONS;

        public string Rank => DetermineRank(ExcitementScore);
        public string RankLabel => GetRankLabel(Rank);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            AnswerJudgment.OnJudged
                .Where(b => b)
                .Subscribe(_ => _correctCount.Value++)
                .AddTo(this);

            // 1問目（QuestionIndex==0）に入るたびにリセット（リスタート時も対応）
            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.Question
                         && GameFlowManager.Instance != null
                         && GameFlowManager.Instance.QuestionIndex == 0)
                .Subscribe(_ => _correctCount.Value = 0)
                .AddTo(this);
        }

        public static string DetermineRank(int score) => score switch
        {
            >= GameConstants.RANK_S => "S",
            >= GameConstants.RANK_A => "A",
            >= GameConstants.RANK_B => "B",
            >= GameConstants.RANK_C => "C",
            _ => "D"
        };

        public static string GetRankLabel(string rank) => rank switch
        {
            "S" => "神",
            "A" => "最高",
            "B" => "いい感じ",
            "C" => "まあまあ",
            _ => "スベった"
        };
    }
}

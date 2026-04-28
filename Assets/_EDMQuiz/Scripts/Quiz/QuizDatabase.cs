using UnityEngine;

namespace EDMQuiz
{
    [CreateAssetMenu(fileName = "QuizDatabase", menuName = "EDMQuiz/QuizDatabase")]
    public class QuizDatabase : ScriptableObject
    {
        public QuizQuestion[] questions;

        public int Count => questions?.Length ?? 0;

        /// <summary>インデックスで問題を取得。範囲外は null</summary>
        public QuizQuestion Get(int index)
        {
            if (questions == null || index < 0 || index >= questions.Length)
            {
                Debug.LogError($"[QuizDatabase] インデックス {index} は範囲外（Count={Count}）");
                return null;
            }
            return questions[index];
        }

        void OnValidate()
        {
            if (questions != null && questions.Length != GameConstants.TOTAL_QUESTIONS)
                Debug.LogWarning($"[{name}] 問題数が {GameConstants.TOTAL_QUESTIONS} 問でない（現在: {questions.Length}）");
        }
    }
}

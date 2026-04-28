using UnityEngine;

namespace EDMQuiz
{
    [CreateAssetMenu(fileName = "QuizDatabase", menuName = "EDMQuiz/QuizDatabase")]
    public class QuizDatabase : ScriptableObject
    {
        public QuizQuestion[] questions;

        /// <summary>インデックスで問題を取得。範囲外は null を返す</summary>
        public QuizQuestion GetQuestion(int index)
        {
            if (index < 0 || index >= questions.Length)
            {
                Debug.LogError($"[QuizDatabase] インデックス {index} は範囲外です");
                return null;
            }
            return questions[index];
        }
    }
}

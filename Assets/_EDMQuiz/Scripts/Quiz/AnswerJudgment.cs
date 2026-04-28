using UnityEngine;

namespace EDMQuiz
{
    public class AnswerJudgment : MonoBehaviour
    {
        /// <summary>入力文字列と正解を完全一致で比較する</summary>
        public static bool Judge(string inputAnswer, QuizQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("[AnswerJudgment] question が null です");
                return false;
            }
            return inputAnswer == question.correctAnswer;
        }
    }
}

using System;
using UnityEngine;

namespace EDMQuiz
{
    public static class AnswerJudgment
    {
        public static event Action<bool> OnJudged;

        /// <summary>入力文字列と正解を完全一致で比較し、結果をイベントで通知する</summary>
        public static bool Judge(string inputAnswer, QuizQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("[AnswerJudgment] question が null です");
                OnJudged?.Invoke(false);
                return false;
            }
            bool isCorrect = inputAnswer == question.correctAnswer;
            OnJudged?.Invoke(isCorrect);
            return isCorrect;
        }
    }
}

using R3;
using UnityEngine;

namespace EDMQuiz
{
    /// <summary>入力文字列と正解を完全一致で比較し、結果を R3 Subject で通知する</summary>
    public static class AnswerJudgment
    {
        private static readonly Subject<bool> _onJudgedSubject = new();
        public static Observable<bool> OnJudged => _onJudgedSubject;

        public static bool Judge(string inputAnswer, QuizQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("[AnswerJudgment] question が null");
                _onJudgedSubject.OnNext(false);
                return false;
            }

            bool isCorrect = !string.IsNullOrEmpty(inputAnswer)
                          && inputAnswer.Length == GameConstants.ANSWER_LENGTH
                          && inputAnswer == question.correctAnswer;

            _onJudgedSubject.OnNext(isCorrect);
            return isCorrect;
        }
    }
}

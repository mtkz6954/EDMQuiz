using UnityEngine;

namespace EDMQuiz
{
    [CreateAssetMenu(fileName = "QuizQuestion", menuName = "EDMQuiz/QuizQuestion")]
    public class QuizQuestion : ScriptableObject
    {
        [Header("問題文")]
        [TextArea(2, 4)]
        public string questionText;

        [Header("ひらがな選択肢（5〜8文字）")]
        public string[] hiraganaOptions;

        [Header("正解（完全一致・ひらがな4文字）")]
        public string correctAnswer;
    }
}

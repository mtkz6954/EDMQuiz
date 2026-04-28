using NaughtyAttributes;
using UnityEngine;

namespace EDMQuiz
{
    [CreateAssetMenu(fileName = "Q_New", menuName = "EDMQuiz/QuizQuestion")]
    public class QuizQuestion : ScriptableObject
    {
        [BoxGroup("問題")]
        [TextArea(2, 4)]
        public string questionText;

        [BoxGroup("選択肢")]
        [InfoBox("5〜8文字。正解の文字をすべて含むこと（重複文字は重複数だけ含める）")]
        public string[] hiraganaOptions;

        [BoxGroup("正解")]
        [InfoBox("ひらがな4文字。hiraganaOptions の文字で組み立てられること")]
        public string correctAnswer;

        void OnValidate()
        {
            if (!string.IsNullOrEmpty(correctAnswer)
                && correctAnswer.Length != GameConstants.ANSWER_LENGTH)
            {
                Debug.LogError($"[{name}] correctAnswer は {GameConstants.ANSWER_LENGTH} 文字必須（現在: {correctAnswer.Length}）");
            }

            if (hiraganaOptions != null
                && (hiraganaOptions.Length < GameConstants.MIN_OPTIONS
                    || hiraganaOptions.Length > GameConstants.MAX_OPTIONS))
            {
                Debug.LogError($"[{name}] hiraganaOptions は {GameConstants.MIN_OPTIONS}〜{GameConstants.MAX_OPTIONS} 文字（現在: {hiraganaOptions.Length}）");
            }

            if (!string.IsNullOrEmpty(correctAnswer) && hiraganaOptions != null)
            {
                var pool = new System.Collections.Generic.List<string>(hiraganaOptions);
                foreach (var ch in correctAnswer)
                {
                    if (!pool.Remove(ch.ToString()))
                    {
                        Debug.LogError($"[{name}] correctAnswer の '{ch}' が hiraganaOptions に不足");
                        break;
                    }
                }
            }
        }
    }
}

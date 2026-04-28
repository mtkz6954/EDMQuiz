using UnityEngine;
using TMPro;

namespace EDMQuiz
{
    public class QuestionDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private QuizDatabase _quizDatabase;
        [SerializeField] private HiraganaInputUI _inputUI;

        void OnEnable()  => GameFlowManager.OnQuestionIndexChanged += ShowQuestion;
        void OnDisable() => GameFlowManager.OnQuestionIndexChanged -= ShowQuestion;

        private void ShowQuestion(int index)
        {
            var q = _quizDatabase.GetQuestion(index);
            if (q == null) return;

            _questionText.text = q.questionText;
            _inputUI.BuildButtons(q.hiraganaOptions);
        }
    }
}

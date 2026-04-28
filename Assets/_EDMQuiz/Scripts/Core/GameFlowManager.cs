using System;
using System.Collections;
using UnityEngine;

namespace EDMQuiz
{
    public class GameFlowManager : MonoBehaviour
    {
        [SerializeField] private QuizDatabase _quizDatabase;

        public static GameFlowManager Instance { get; private set; }

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;
        public int CurrentQuestionIndex { get; private set; }
        public int CorrectCount { get; private set; }

        public static event Action<GamePhase> OnPhaseChanged;
        public static event Action<int>       OnQuestionIndexChanged;
        public static event Action<bool>      OnAnswerSubmitted;
        public static event Action<int>       OnGameEnd;

        private bool _isAnswerSubmitted;
        private Coroutine _phaseCoroutine;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartGame()
        {
            if (CurrentPhase != GamePhase.Idle) return;

            CurrentQuestionIndex = 0;
            CorrectCount = 0;
            _isAnswerSubmitted = false;

            AudioManager.Instance.PlayBGM();
            StartNextQuestion();
        }

        public void SubmitAnswer(string inputAnswer)
        {
            if (CurrentPhase != GamePhase.BuildUp) return;
            if (_isAnswerSubmitted) return;
            _isAnswerSubmitted = true;

            if (_phaseCoroutine != null) StopCoroutine(_phaseCoroutine);

            var question = _quizDatabase.GetQuestion(CurrentQuestionIndex);
            bool isCorrect = AnswerJudgment.Judge(inputAnswer, question);

            if (isCorrect) CorrectCount++;
            OnAnswerSubmitted?.Invoke(isCorrect);

            _phaseCoroutine = StartCoroutine(DropSequence());
        }

        private void StartNextQuestion()
        {
            _isAnswerSubmitted = false;
            OnQuestionIndexChanged?.Invoke(CurrentQuestionIndex);
            _phaseCoroutine = StartCoroutine(QuestionSequence());
        }

        private IEnumerator QuestionSequence()
        {
            ChangePhase(GamePhase.Question);
            yield return new WaitForSeconds(GameConstants.QUESTION_PHASE_SEC);

            ChangePhase(GamePhase.BuildUp);
            yield return new WaitForSeconds(GameConstants.BUILDUP_PHASE_SEC);

            // タイムアウト：空文字で強制不正解
            SubmitAnswer("");
        }

        private IEnumerator DropSequence()
        {
            ChangePhase(GamePhase.Drop);
            yield return new WaitForSeconds(GameConstants.DROP_REVEAL_SEC);

            ChangePhase(GamePhase.ResultReveal);
            yield return new WaitForSeconds(GameConstants.RESULT_REVEAL_SEC);

            ChangePhase(GamePhase.Next);
            yield return new WaitForSeconds(GameConstants.NEXT_TRANSITION_SEC);

            CurrentQuestionIndex++;
            if (CurrentQuestionIndex >= GameConstants.TOTAL_QUESTIONS)
            {
                ChangePhase(GamePhase.GameEnd);
                OnGameEnd?.Invoke(CorrectCount);
            }
            else
            {
                StartNextQuestion();
            }
        }

        private void ChangePhase(GamePhase next)
        {
            CurrentPhase = next;
            OnPhaseChanged?.Invoke(next);
        }
    }
}

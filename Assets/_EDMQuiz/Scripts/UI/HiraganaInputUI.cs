using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>UI Toolkit でひらがな入力 UI を構築・管理する</summary>
    public class HiraganaInputUI : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private Label _questionText;
        private Label[] _answerCells;
        private VisualElement _buttonContainer;
        private Button _backspaceButton;
        private Button _confirmButton;

        private readonly List<string> _inputBuffer = new();
        private readonly List<Button> _hiraganaButtons = new();

        void OnEnable()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("[HiraganaInputUI] UIDocument 未設定");
                return;
            }
            _root = _uiDocument.rootVisualElement;
            _questionText    = _root.Q<Label>("question-text");
            _buttonContainer = _root.Q<VisualElement>("hiragana-buttons");
            _backspaceButton = _root.Q<Button>("backspace-button");
            _confirmButton   = _root.Q<Button>("confirm-button");

            var cellsContainer = _root.Q<VisualElement>("answer-cells");
            _answerCells = cellsContainer.Query<Label>(className: "answer-cell").ToList().ToArray();

            _backspaceButton.clicked += OnBackspacePressed;
            _confirmButton.clicked   += OnConfirmPressed;

            GameFlowManager.OnPhaseChanged
                .Subscribe(HandlePhaseChanged)
                .AddTo(this);

            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.BuildUp)
                .Subscribe(_ => PulseAllButtons())
                .AddTo(this);

            SetInputEnabled(false);
            UpdateAnswerDisplay();
            UpdateConfirmButton();
        }

        void OnDisable()
        {
            if (_backspaceButton != null) _backspaceButton.clicked -= OnBackspacePressed;
            if (_confirmButton   != null) _confirmButton.clicked   -= OnConfirmPressed;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Question:
                    LoadQuestion();
                    SetInputEnabled(false);
                    break;
                case GamePhase.BuildUp:
                    SetInputEnabled(true);
                    break;
                default:
                    SetInputEnabled(false);
                    break;
            }
        }

        private void LoadQuestion()
        {
            _inputBuffer.Clear();
            UpdateAnswerDisplay();
            UpdateConfirmButton();

            var q = GameFlowManager.Instance?.CurrentQuestion;
            if (q == null) return;

            _questionText.text = q.questionText;
            BuildHiraganaButtons(q.hiraganaOptions);
        }

        private void BuildHiraganaButtons(string[] options)
        {
            _buttonContainer.Clear();
            _hiraganaButtons.Clear();
            if (options == null) return;

            foreach (var ch in options)
            {
                string captured = ch;
                var btn = new Button(() => OnHiraganaPressed(captured)) { text = ch };
                btn.AddToClassList("hiragana-button");
                _buttonContainer.Add(btn);
                _hiraganaButtons.Add(btn);
            }
        }

        private void OnHiraganaPressed(string kana)
        {
            if (_inputBuffer.Count >= GameConstants.ANSWER_LENGTH) return;
            _inputBuffer.Add(kana);
            UpdateAnswerDisplay();
            UpdateConfirmButton();
            AudioManager.Instance?.PlayUiTapSE();
        }

        private void OnBackspacePressed()
        {
            if (_inputBuffer.Count == 0) return;
            _inputBuffer.RemoveAt(_inputBuffer.Count - 1);
            UpdateAnswerDisplay();
            UpdateConfirmButton();
        }

        private void OnConfirmPressed()
        {
            if (_inputBuffer.Count < GameConstants.ANSWER_LENGTH) return;
            string answer = string.Concat(_inputBuffer);
            GameFlowManager.Instance?.ConfirmAnswer(answer);
        }

        private void UpdateAnswerDisplay()
        {
            if (_answerCells == null) return;
            for (int i = 0; i < _answerCells.Length; i++)
                _answerCells[i].text = i < _inputBuffer.Count ? _inputBuffer[i] : "";
        }

        private void UpdateConfirmButton()
        {
            if (_confirmButton == null) return;
            bool ready = _inputBuffer.Count == GameConstants.ANSWER_LENGTH
                      && GameFlowManager.Instance?.CurrentPhase == GamePhase.BuildUp;
            _confirmButton.SetEnabled(ready);
        }

        private void SetInputEnabled(bool enabled)
        {
            foreach (var btn in _hiraganaButtons) btn.SetEnabled(enabled);
            if (_backspaceButton != null) _backspaceButton.SetEnabled(enabled);
            UpdateConfirmButton();
        }

        private void PulseAllButtons()
        {
            float duration = GameConstants.GetBeatDuration() * GameConstants.BUTTON_PULSE_DURATION_RATIO;
            foreach (var btn in _hiraganaButtons)
                btn.DOPulse(GameConstants.BUTTON_PULSE_SCALE, duration);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EDMQuiz
{
    public class HiraganaInputUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private GameObject _hiraganaButtonPrefab;
        [SerializeField] private TextMeshProUGUI[] _answerCells; // 4要素固定
        [SerializeField] private Button _backspaceButton;
        [SerializeField] private Button _confirmButton;

        private readonly List<string> _inputBuffer = new();
        private readonly List<Button> _hiraganaButtons = new();
        private float _beatInterval;

        void OnEnable()
        {
            GameFlowManager.OnPhaseChanged    += HandlePhaseChanged;
            GameFlowManager.OnQuestionIndexChanged += LoadQuestion;
            BpmClock.OnBeat                   += HandleBeat;
        }

        void OnDisable()
        {
            GameFlowManager.OnPhaseChanged    -= HandlePhaseChanged;
            GameFlowManager.OnQuestionIndexChanged -= LoadQuestion;
            BpmClock.OnBeat                   -= HandleBeat;
        }

        void Start()
        {
            _beatInterval = 60f / GameConstants.BPM;
            _backspaceButton.onClick.AddListener(OnBackspacePressed);
            _confirmButton.onClick.AddListener(OnConfirmPressed);
            SetInputEnabled(false);
        }

        private void LoadQuestion(int index)
        {
            _inputBuffer.Clear();
            UpdateAnswerDisplay();
            UpdateConfirmButton();

            // ボタンを動的生成
            foreach (Transform child in _buttonContainer) Destroy(child.gameObject);
            _hiraganaButtons.Clear();

            var question = GameFlowManager.Instance != null
                ? FindFirstObjectByType<GameFlowManager>()
                : null;
            // TODO: QuizDatabase への参照を GameFlowManager 経由で受け取る
        }

        public void BuildButtons(string[] options)
        {
            foreach (Transform child in _buttonContainer) Destroy(child.gameObject);
            _hiraganaButtons.Clear();

            foreach (var ch in options)
            {
                var go = Instantiate(_hiraganaButtonPrefab, _buttonContainer);
                var btn = go.GetComponent<Button>();
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                label.text = ch;
                string captured = ch;
                btn.onClick.AddListener(() => OnHiraganaButtonPressed(captured));
                _hiraganaButtons.Add(btn);
            }
        }

        private void OnHiraganaButtonPressed(string character)
        {
            if (_inputBuffer.Count >= GameConstants.MAX_INPUT_LENGTH) return;
            _inputBuffer.Add(character);
            UpdateAnswerDisplay();
            UpdateConfirmButton();
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
            if (_inputBuffer.Count < GameConstants.MAX_INPUT_LENGTH) return;
            string answer = string.Concat(_inputBuffer);
            GameFlowManager.Instance.SubmitAnswer(answer);
        }

        private void UpdateAnswerDisplay()
        {
            for (int i = 0; i < _answerCells.Length; i++)
                _answerCells[i].text = i < _inputBuffer.Count ? _inputBuffer[i] : "";
        }

        private void UpdateConfirmButton()
        {
            _confirmButton.interactable = _inputBuffer.Count == GameConstants.MAX_INPUT_LENGTH;
        }

        private void SetInputEnabled(bool enabled)
        {
            foreach (var btn in _hiraganaButtons) btn.interactable = enabled;
            _backspaceButton.interactable = enabled;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            SetInputEnabled(phase == GamePhase.BuildUp);
        }

        private void HandleBeat()
        {
            foreach (var btn in _hiraganaButtons)
                btn.transform.DOPunchScale(
                    Vector3.one * GameConstants.BUTTON_PULSE_SCALE,
                    _beatInterval * GameConstants.BUTTON_PULSE_DURATION_RATIO,
                    1, 0
                );
        }
    }
}

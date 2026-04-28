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

        /// <summary>下部キャラクターストリップに表示する Sprite (5体分を Inspector で割り当て)</summary>
        [SerializeField] private Sprite[] _characterSprites = new Sprite[5];

        // ひらがなボタン用のカラーバリエ（USS の .hiragana-button--c0〜c6 に対応）
        private const int HIRAGANA_COLOR_VARIANTS = 7;
        // 回答セル用のカラーバリエ（USS の .answer-cell--c0〜c3 に対応）
        private const int ANSWER_CELL_COLOR_VARIANTS = 4;

        private VisualElement _root;
        private Label _questionText;
        private Label[] _answerCells;
        private VisualElement _buttonContainer;
        private Button _backspaceButton;
        private Button _confirmButton;

        // HUD 要素
        private Label _progressCounter;
        private Label _questionNumberBadge;
        private VisualElement _buildupBarFill;
        private VisualElement[] _characterSlots;

        // ビート表示用ドット行
        private VisualElement _buildupDotsContainer;
        private readonly List<VisualElement> _beatDots = new();

        private readonly List<string> _inputBuffer = new();
        private readonly List<Button> _hiraganaButtons = new();

        private int _buildupBeatCount;
        private static readonly int TotalBuildupBeats =
            GameConstants.BUILDUP_BARS * GameConstants.BEATS_PER_BAR;

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

            // HUD 要素を取得
            _progressCounter      = _root.Q<Label>("progress-counter");
            _questionNumberBadge  = _root.Q<Label>("question-number-badge");
            _buildupBarFill       = _root.Q<VisualElement>("buildup-bar-fill");
            _buildupDotsContainer = _root.Q<VisualElement>("buildup-dots");

            _characterSlots = new VisualElement[5];
            for (int i = 0; i < 5; i++)
                _characterSlots[i] = _root.Q<VisualElement>($"character-slot-{i}");

            if (_backspaceButton != null) _backspaceButton.clicked += OnBackspacePressed;
            if (_confirmButton   != null) _confirmButton.clicked   += OnConfirmPressed;

            InitCharacterStrip();
            BuildBeatDots();

            GameFlowManager.OnPhaseChanged
                .Subscribe(HandlePhaseChanged)
                .AddTo(this);

            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.BuildUp)
                .Subscribe(_ =>
                {
                    PulseAllButtons();
                    _buildupBeatCount++;
                    UpdateBuildupBar();
                })
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
                    _buildupBeatCount = 0;
                    ResetBuildupBar();
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

            // 進捗カウンターと問題番号バッジを更新（モックアップ "Q03/5" / "Q03"）
            int displayNum = (GameFlowManager.Instance?.QuestionIndex ?? 0) + 1;
            if (_progressCounter != null)
                _progressCounter.text = $"Q{displayNum:D2}/{GameConstants.TOTAL_QUESTIONS}";
            if (_questionNumberBadge != null)
                _questionNumberBadge.text = $"Q{displayNum:D2}";
        }

        private void BuildHiraganaButtons(string[] options)
        {
            _buttonContainer.Clear();
            _hiraganaButtons.Clear();
            if (options == null) return;

            for (int i = 0; i < options.Length; i++)
            {
                string captured = options[i];
                var btn = new Button(() => OnHiraganaPressed(captured)) { text = captured };
                btn.AddToClassList("hiragana-button");
                btn.AddToClassList($"hiragana-button--c{i % HIRAGANA_COLOR_VARIANTS}");
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

            // 入力したセルをパルス（DOTween）
            int filledIdx = _inputBuffer.Count - 1;
            if (_answerCells != null && filledIdx >= 0 && filledIdx < _answerCells.Length)
            {
                _answerCells[filledIdx].DOPulse(
                    GameConstants.BUTTON_PULSE_SCALE,
                    GameConstants.GetBeatDuration() * GameConstants.BUTTON_PULSE_DURATION_RATIO);
            }

            // 4文字目で自動確定（モックアップ仕様: ドロップ手前の4拍に回答）
            if (_inputBuffer.Count == GameConstants.ANSWER_LENGTH
                && GameFlowManager.Instance?.CurrentPhase == GamePhase.BuildUp)
            {
                AutoConfirm();
            }
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

        private void AutoConfirm()
        {
            string answer = string.Concat(_inputBuffer);
            GameFlowManager.Instance?.ConfirmAnswer(answer);
        }

        private void UpdateAnswerDisplay()
        {
            if (_answerCells == null) return;
            for (int i = 0; i < _answerCells.Length; i++)
            {
                bool filled   = i < _inputBuffer.Count;
                bool isCursor = i == _inputBuffer.Count;
                _answerCells[i].text = filled ? _inputBuffer[i] : "";
                _answerCells[i].EnableInClassList("answer-cell--filled", filled);
                _answerCells[i].EnableInClassList("answer-cell--cursor", isCursor && !filled);
            }
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

        private void InitCharacterStrip()
        {
            if (_characterSlots == null) return;
            for (int i = 0; i < _characterSlots.Length; i++)
            {
                if (_characterSlots[i] == null) continue;
                if (i < _characterSprites.Length && _characterSprites[i] != null)
                    _characterSlots[i].style.backgroundImage =
                        new StyleBackground(_characterSprites[i]);
            }
        }

        /// <summary>BuildUp バーの下にビートを示すドット行を生成する（1小節 = 1ドット）</summary>
        private void BuildBeatDots()
        {
            if (_buildupDotsContainer == null) return;
            _buildupDotsContainer.Clear();
            _beatDots.Clear();
            for (int i = 0; i < GameConstants.BUILDUP_BARS; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList("beat-dot");
                _buildupDotsContainer.Add(dot);
                _beatDots.Add(dot);
            }
        }

        private void UpdateBuildupBar()
        {
            // ビート進捗に応じて埋まるドットを更新（1小節 = BEATS_PER_BAR ビート）
            int filledCount = Mathf.Clamp(
                _buildupBeatCount / GameConstants.BEATS_PER_BAR,
                0, _beatDots.Count);
            for (int i = 0; i < _beatDots.Count; i++)
                _beatDots[i].EnableInClassList("beat-dot--filled", i < filledCount);

            // 旧フィルバーは透明（レインボーバー導入で見た目は静止）が、
            // 互換性維持のため width だけ進捗で更新しておく。
            if (_buildupBarFill != null)
            {
                float progress = Mathf.Clamp01((float)_buildupBeatCount / TotalBuildupBeats);
                _buildupBarFill.style.width =
                    new StyleLength(new Length(progress * 100f, LengthUnit.Percent));
            }
        }

        private void ResetBuildupBar()
        {
            for (int i = 0; i < _beatDots.Count; i++)
                _beatDots[i].EnableInClassList("beat-dot--filled", false);

            if (_buildupBarFill != null)
            {
                _buildupBarFill.style.width =
                    new StyleLength(new Length(0f, LengthUnit.Percent));
            }
        }
    }
}

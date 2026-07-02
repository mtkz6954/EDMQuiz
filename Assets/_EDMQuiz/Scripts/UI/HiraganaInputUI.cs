using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>UI Toolkit でひらがな入力 UI を構築・管理する</summary>
    public class HiraganaInputUI : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        /// <summary>キャラクター表示に使う Sprite (10体分を Inspector で割り当て: 0-4 が character-slot、5-9 が cameo-slot)</summary>
        [SerializeField] private Sprite[] _characterSprites = new Sprite[10];

        // ひらがなボタン用のカラーバリエ（USS の .hiragana-button--c0〜c9 に対応）
        private const int HIRAGANA_COLOR_VARIANTS = 10;
        // 回答セル用のカラーバリエ（USS の .answer-cell--c0〜c3 に対応）
        private const int ANSWER_CELL_COLOR_VARIANTS = 4;

        private VisualElement _root;
        private VisualElement _questionPanel;
        private Label _questionText;
        private Label[] _answerCells;
        private VisualElement _buttonContainer;
        private Button _backspaceButton;
        private Button _confirmButton;

        // HUD 要素
        private Label _progressCounter;
        private Label _questionNumberBadge;
        private VisualElement _countdownWidget;
        private VisualElement _answerNowCallout;
        private VisualElement _buildupBarFill;
        private Label _buildupCountdown;
        private VisualElement[] _characterSlots;
        private VisualElement[] _cameoSlots;

        // ビート表示用ドット行
        private VisualElement _buildupDotsContainer;
        private readonly List<VisualElement> _beatDots = new();

        private readonly List<string> _inputBuffer = new();
        private readonly List<Button> _hiraganaButtons = new();
        // ボタンに対応するひらがな（キーボードショートカット呼び出し時に参照）
        private readonly List<string> _hiraganaKanas = new();

        private static readonly int[] KeypadNumbersByOptionIndex =
        {
            7, 8, 9,
            4, 5, 6,
            1, 2, 3,
            0,
        };

        private Tween _questionFadeTween;
        private Tween _questionSlideTween;

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
            _questionPanel   = _root.Q<VisualElement>("question-panel");
            _questionText    = _root.Q<Label>("question-text");
            _buttonContainer = _root.Q<VisualElement>("hiragana-buttons");
            _backspaceButton = _root.Q<Button>("backspace-button");
            _confirmButton   = _root.Q<Button>("confirm-button");

            var cellsContainer = _root.Q<VisualElement>("answer-cells");
            _answerCells = cellsContainer.Query<Label>(className: "answer-cell").ToList().ToArray();

            // HUD 要素を取得
            _progressCounter      = _root.Q<Label>("progress-counter");
            _questionNumberBadge  = _root.Q<Label>("question-number-badge");
            _countdownWidget      = _root.Q<VisualElement>("countdown-widget");
            _answerNowCallout     = _root.Q<VisualElement>("answer-now-callout");
            _buildupBarFill       = _root.Q<VisualElement>("buildup-bar-fill");
            _buildupCountdown     = _root.Q<Label>("buildup-countdown");
            _buildupDotsContainer = _root.Q<VisualElement>("buildup-dots");

            _characterSlots = new VisualElement[5];
            for (int i = 0; i < 5; i++)
                _characterSlots[i] = _root.Q<VisualElement>($"character-slot-{i}");

            _cameoSlots = new VisualElement[5];
            for (int i = 0; i < 5; i++)
                _cameoSlots[i] = _root.Q<VisualElement>($"cameo-slot-{i}");

            if (_backspaceButton != null) _backspaceButton.clicked += OnBackspacePressed;
            if (_confirmButton   != null) _confirmButton.clicked   += OnConfirmPressed;

            // ルートにキーボードイベントを登録（数字 1-8 でひらがなボタンを押す）
            // タッチ主体のデバイスでは登録しない（モバイル WebGL ブラウザ含む）
            if (!ResponsiveLayout.IsTouchPrimary())
            {
                _root.focusable = true;
                _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
                _root.Focus();
            }

            // ビューポートのアスペクト比に応じて .layout-portrait / .layout-landscape を切替
            // （PanelSettings も渡し、縦=幅基準 / 横=高さ基準のスケーリング切替を有効化）
            ResponsiveLayout.Attach(_root, _uiDocument.panelSettings);

            InitCharacterStrip();
            BuildBeatDots();

            GameFlowManager.OnPhaseChanged
                .Subscribe(HandlePhaseChanged)
                .AddTo(this);

            GameFlowManager.OnQuestionReveal
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.Question)
                .Subscribe(_ => PlayQuestionFadeIn())
                .AddTo(this);

            // BuildUp 中: ビート進捗で背景ドットを埋める
            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.BuildUp)
                .Subscribe(_ =>
                {
                    _buildupBeatCount++;
                    UpdateBuildupBar();
                })
                .AddTo(this);

            // BuildUp / AnswerWindow 中: UI をビート同期でパルス
            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null
                         && (GameFlowManager.Instance.CurrentPhase == GamePhase.BuildUp
                          || GameFlowManager.Instance.CurrentPhase == GamePhase.AnswerWindow))
                .Subscribe(_ => PulseInputArea())
                .AddTo(this);

            // BGM 経過秒に基づくカウントダウン表示
            GameFlowManager.BuildUpRemainingSec
                .Subscribe(UpdateBuildupCountdown)
                .AddTo(this);

            SetInputEnabled(false);
            SetCountdownVisible(false);
            SetAnswerNowVisible(false);
            ResetQuestionPanelToHidden();
            UpdateAnswerDisplay();
            UpdateConfirmButton();
        }

        void OnDisable()
        {
            if (_backspaceButton != null) _backspaceButton.clicked -= OnBackspacePressed;
            if (_confirmButton   != null) _confirmButton.clicked   -= OnConfirmPressed;
            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            _questionFadeTween?.Kill();
            _questionSlideTween?.Kill();
            _questionFadeTween  = null;
            _questionSlideTween = null;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Question:
                    _buildupBeatCount = 0;
                    ResetBuildupBar();
                    LoadQuestion();
                    ResetQuestionPanelToHidden();
                    SetInputEnabled(false);
                    SetCountdownVisible(false);
                    SetAnswerNowVisible(false);
                    break;
                case GamePhase.BuildUp:
                    CompleteQuestionReveal();
                    SetInputEnabled(false);     // 入力ロック
                    SetCountdownVisible(true);  // 残り秒数を表示
                    SetAnswerNowVisible(false);
                    break;
                case GamePhase.AnswerWindow:
                    SetQuestionVisible(true);
                    SetInputEnabled(true);      // 4 拍だけ入力 ON
                    SetCountdownVisible(false);
                    SetAnswerNowVisible(true);
                    break;
                case GamePhase.Drop:
                case GamePhase.FadeOut:
                    SetQuestionVisible(true);
                    SetInputEnabled(false);
                    SetCountdownVisible(false);
                    SetAnswerNowVisible(false);
                    break;
                default:
                    ResetQuestionPanelToHidden();
                    SetInputEnabled(false);
                    SetCountdownVisible(false);
                    SetAnswerNowVisible(false);
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

            // 進捗カウンターと問題番号バッジを更新（モックアップ "03/5" / "03"）
            int displayNum = (GameFlowManager.Instance?.QuestionIndex ?? 0) + 1;
            if (_progressCounter != null)
                _progressCounter.text = $"{displayNum:D2}/{GameConstants.TOTAL_QUESTIONS}";
            if (_questionNumberBadge != null)
                _questionNumberBadge.text = $"{displayNum:D2}";
        }

        private void BuildHiraganaButtons(string[] options)
        {
            _buttonContainer.Clear();
            _hiraganaButtons.Clear();
            _hiraganaKanas.Clear();
            if (options == null) return;

            var rowTop = CreateKeypadRow("hiragana-key-row--top");
            var rowMiddle = CreateKeypadRow("hiragana-key-row--middle");
            var rowBottom = CreateKeypadRow("hiragana-key-row--bottom");
            var rowWide = CreateKeypadRow("hiragana-key-row--wide");

            _buttonContainer.Add(rowTop);
            _buttonContainer.Add(rowMiddle);
            _buttonContainer.Add(rowBottom);
            _buttonContainer.Add(rowWide);

            for (int i = 0; i < options.Length && i < KeypadNumbersByOptionIndex.Length; i++)
            {
                string captured = options[i];
                var btn = new Button(() => OnHiraganaPressed(captured)) { text = captured };
                btn.AddToClassList("hiragana-button");
                btn.AddToClassList($"hiragana-button--c{i % HIRAGANA_COLOR_VARIANTS}");

                int keypadNumber = GetKeypadNumberForOptionIndex(i);
                if (keypadNumber == 0)
                    btn.AddToClassList("hiragana-button--wide");

                // キーボードショートカット番号バッジ（PC のみ表示）
                if (!ResponsiveLayout.IsTouchPrimary())
                {
                    var numberBadge = new Label(keypadNumber.ToString());
                    numberBadge.AddToClassList("hiragana-number-badge");
                    numberBadge.pickingMode = PickingMode.Ignore;
                    btn.Add(numberBadge);
                }

                GetKeypadRowForOptionIndex(i, rowTop, rowMiddle, rowBottom, rowWide).Add(btn);
                _hiraganaButtons.Add(btn);
                _hiraganaKanas.Add(captured);
            }
        }

        private static VisualElement CreateKeypadRow(string className)
        {
            var row = new VisualElement();
            row.AddToClassList("hiragana-key-row");
            row.AddToClassList(className);
            return row;
        }

        private static VisualElement GetKeypadRowForOptionIndex(
            int optionIndex,
            VisualElement rowTop,
            VisualElement rowMiddle,
            VisualElement rowBottom,
            VisualElement rowWide)
        {
            if (optionIndex <= 2) return rowTop;
            if (optionIndex <= 5) return rowMiddle;
            if (optionIndex <= 8) return rowBottom;
            return rowWide;
        }

        /// <summary>キーボード 1-8（メイン段／テンキー）で対応するひらがなボタンを押下扱いにする。</summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (GameFlowManager.Instance?.CurrentPhase != GamePhase.AnswerWindow) return;

            int idx = ResolveHiraganaIndex(evt.keyCode);
            if (idx < 0 || idx >= _hiraganaButtons.Count) return;

            var btn = _hiraganaButtons[idx];
            if (btn == null || !btn.enabledInHierarchy) return;

            OnHiraganaPressed(_hiraganaKanas[idx]);
            evt.StopPropagation();
        }

        private static int ResolveHiraganaIndex(KeyCode keyCode)
        {
            for (int i = 0; i < KeypadNumbersByOptionIndex.Length; i++)
            {
                if (MatchesNumberKey(keyCode, KeypadNumbersByOptionIndex[i]))
                    return i;
            }
            return -1;
        }

        private static int GetKeypadNumberForOptionIndex(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= KeypadNumbersByOptionIndex.Length) return -1;
            return KeypadNumbersByOptionIndex[optionIndex];
        }

        private static bool MatchesNumberKey(KeyCode keyCode, int number)
        {
            return number switch
            {
                0 => keyCode == KeyCode.Alpha0 || keyCode == KeyCode.Keypad0,
                1 => keyCode == KeyCode.Alpha1 || keyCode == KeyCode.Keypad1,
                2 => keyCode == KeyCode.Alpha2 || keyCode == KeyCode.Keypad2,
                3 => keyCode == KeyCode.Alpha3 || keyCode == KeyCode.Keypad3,
                4 => keyCode == KeyCode.Alpha4 || keyCode == KeyCode.Keypad4,
                5 => keyCode == KeyCode.Alpha5 || keyCode == KeyCode.Keypad5,
                6 => keyCode == KeyCode.Alpha6 || keyCode == KeyCode.Keypad6,
                7 => keyCode == KeyCode.Alpha7 || keyCode == KeyCode.Keypad7,
                8 => keyCode == KeyCode.Alpha8 || keyCode == KeyCode.Keypad8,
                9 => keyCode == KeyCode.Alpha9 || keyCode == KeyCode.Keypad9,
                _ => false,
            };
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
                && GameFlowManager.Instance?.CurrentPhase == GamePhase.AnswerWindow)
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
                      && GameFlowManager.Instance?.CurrentPhase == GamePhase.AnswerWindow;
            _confirmButton.SetEnabled(ready);
        }

        private void SetInputEnabled(bool enabled)
        {
            foreach (var btn in _hiraganaButtons) btn.SetEnabled(enabled);
            if (_backspaceButton != null) _backspaceButton.SetEnabled(enabled);
            UpdateConfirmButton();
        }

        private void SetQuestionVisible(bool visible)
        {
            if (_questionPanel == null) return;
            _questionPanel.style.visibility = visible
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        /// <summary>問題パネルを上方オフセット位置 + 不透明度0 + Hidden で初期化する。</summary>
        private void ResetQuestionPanelToHidden()
        {
            if (_questionPanel == null) return;
            _questionFadeTween?.Kill();
            _questionSlideTween?.Kill();
            _questionFadeTween  = null;
            _questionSlideTween = null;

            _questionPanel.style.opacity    = 0f;
            _questionPanel.style.translate  = new StyleTranslate(
                new Translate(0f, GameConstants.QUESTION_FADE_IN_OFFSET_PX, 0f));
            _questionPanel.style.visibility = Visibility.Hidden;
        }

        /// <summary>問題パネルを上方オフセットから所定位置へスライド + フェードインで表示する。</summary>
        private void PlayQuestionFadeIn()
        {
            if (_questionPanel == null) return;
            _questionFadeTween?.Kill();
            _questionSlideTween?.Kill();

            _questionPanel.style.opacity    = 0f;
            _questionPanel.style.translate  = new StyleTranslate(
                new Translate(0f, GameConstants.QUESTION_FADE_IN_OFFSET_PX, 0f));
            _questionPanel.style.visibility = Visibility.Visible;

            _questionFadeTween = _questionPanel
                .DOFade(1f, GameConstants.QUESTION_FADE_IN_DURATION)
                .SetEase(Ease.OutCubic);
            _questionSlideTween = _questionPanel
                .DOTranslateYTo(0f, GameConstants.QUESTION_FADE_IN_DURATION)
                .SetEase(Ease.OutCubic);
        }

        private void CompleteQuestionReveal()
        {
            if (_questionPanel == null) return;
            _questionFadeTween?.Kill();
            _questionSlideTween?.Kill();
            _questionFadeTween  = null;
            _questionSlideTween = null;

            _questionPanel.style.visibility = Visibility.Visible;
            _questionPanel.style.opacity    = 1f;
            _questionPanel.style.translate  = new StyleTranslate(new Translate(0f, 0f, 0f));
        }

        private void PulseAllButtons()
        {
            float duration = GameConstants.GetBeatDuration() * GameConstants.BUTTON_PULSE_DURATION_RATIO;
            foreach (var btn in _hiraganaButtons)
                btn.DOPulse(GameConstants.BUTTON_PULSE_SCALE, duration);
        }

        private void PulseInputArea()
        {
            PulseAllButtons();
            float duration = GameConstants.GetBeatDuration() * GameConstants.BUTTON_PULSE_DURATION_RATIO;
            if (_answerCells == null) return;
            for (int i = 0; i < _answerCells.Length; i++)
                _answerCells[i].DOPulse(GameConstants.BUTTON_PULSE_SCALE, duration);
        }

        private void InitCharacterStrip()
        {
            if (_characterSlots != null)
            {
                for (int i = 0; i < _characterSlots.Length; i++)
                {
                    if (_characterSlots[i] == null) continue;
                    if (i < _characterSprites.Length && _characterSprites[i] != null)
                        _characterSlots[i].style.backgroundImage = new StyleBackground(_characterSprites[i]);
                }
            }

            if (_cameoSlots != null)
            {
                for (int i = 0; i < _cameoSlots.Length; i++)
                {
                    if (_cameoSlots[i] == null) continue;
                    int spriteIndex = i + 5;
                    if (spriteIndex < _characterSprites.Length && _characterSprites[spriteIndex] != null)
                        _cameoSlots[i].style.backgroundImage = new StyleBackground(_characterSprites[spriteIndex]);
                }
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

        private void UpdateBuildupCountdown(float remainingSec)
        {
            if (_buildupCountdown == null) return;
            string value = remainingSec < 10f
                ? remainingSec.ToString("F1")
                : Mathf.CeilToInt(remainingSec).ToString();
            _buildupCountdown.text = $"回答まで\n{value}秒";
        }

        private void SetCountdownVisible(bool visible)
        {
            if (_countdownWidget == null) return;
            _countdownWidget.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetAnswerNowVisible(bool visible)
        {
            if (_answerNowCallout == null) return;
            _answerNowCallout.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _answerNowCallout.style.scale = visible
                ? new StyleScale(new Scale(Vector3.one))
                : new StyleScale(new Scale(Vector3.zero));

            if (visible)
            {
                _answerNowCallout.DOPulse(
                    GameConstants.ANSWER_NOW_PULSE_SCALE,
                    GameConstants.GetBeatDuration() * GameConstants.ANSWER_NOW_PULSE_BEATS);
            }
        }
    }
}

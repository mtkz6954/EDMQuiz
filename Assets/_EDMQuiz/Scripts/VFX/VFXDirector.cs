using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>正解・不正解・ビート同期の演出を統括</summary>
    public class VFXDirector : MonoBehaviour
    {
        [Header("World VFX")]
        [SerializeField] private ParticleSystem _confettiParticle;
        [SerializeField] private Transform      _mirrorBallTransform;
        [SerializeField] private Animator       _funnymonAnimator;
        [SerializeField] private MovingSpotlightController _spotlights;
        [SerializeField] private bool _autoCreateSpotlights = true;

        [Header("UI VFX (UI Toolkit)")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _blueOverlayName       = "blue-overlay";
        [SerializeField] private string _flashOverlayName      = "flash-overlay";
        [SerializeField] private string _correctLabelName      = "correct-label";
        [SerializeField] private string _incorrectLabelName    = "incorrect-label";
        [SerializeField] private string _questionPanelName     = "question-panel";
        [SerializeField] private string _backgroundPanelName   = "background-panel";
        [SerializeField] private string _lasersName            = "bg-lasers";
        [SerializeField] private string _instructionLabelName  = "instruction-label";
        [SerializeField] private string _progressCounterName   = "progress-counter";
        [SerializeField] private string _answerCellsName       = "answer-cells";
        [SerializeField] private string _hiraganaButtonsName   = "hiragana-buttons";
        [SerializeField] private string _charactersStripName   = "characters-strip";

        private const int CharacterSlotCount = 5;

        // コケる方向（スロットごと）— ±90° の左右倒れを混在させて笑い感を出す
        private static readonly float[] TumbleDirections = { -1f, 1f, -1f, 1f, -1f };

        private VisualElement _blueOverlay;
        private VisualElement _flashOverlay;
        private Label _correctLabel;
        private Label _incorrectLabel;
        private VisualElement _questionPanel;
        private VisualElement _backgroundPanel; // 画面シェイク対象
        private VisualElement _lasers;
        private VisualElement _instructionLabel;
        private VisualElement _progressCounter;
        private VisualElement _answerCells;
        private VisualElement _hiraganaButtons;
        private readonly VisualElement[] _characterSlots = new VisualElement[CharacterSlotCount];

        private CancellationTokenSource _vfxCts;
        private Tween _mirrorBallTween;
        private Tween _correctLabelTween;
        private Tween _incorrectLabelTween;
        private Tween _blueOverlayTween;
        private Tween _flashTween;
        private Tween _shakeTween;
        private Tween _laserSpinTween;

        // ダンス Tween キャッシュ — 不正解時に Kill してコケポーズへ滑らかに切替
        private readonly Tween[] _danceBounceTweens = new Tween[CharacterSlotCount];
        private readonly Tween[] _danceRotateTweens = new Tween[CharacterSlotCount];
        private readonly Tween[] _danceFlipTweens   = new Tween[CharacterSlotCount];
        // コケアニメ Tween — Cancel 時に Kill
        private readonly Tween[] _tumbleBounceTweens = new Tween[CharacterSlotCount];
        private readonly Tween[] _tumbleRotateTweens = new Tween[CharacterSlotCount];
        // 正解時のお祝いスピン／大ジャンプ Tween
        private readonly Tween[] _celebrateRotateTweens = new Tween[CharacterSlotCount];
        private readonly Tween[] _celebrateBounceTweens = new Tween[CharacterSlotCount];

        // 正解中の継続演出フラグ／状態
        private bool _isCorrectActive;
        private int  _dropBeatCounter;
        private bool _correctLabelDanceArmed;

        private int _beatCounter;

        void OnEnable()
        {
            EnsureSpotlights();

            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                _blueOverlay      = root.Q<VisualElement>(_blueOverlayName);
                _flashOverlay     = root.Q<VisualElement>(_flashOverlayName);
                _correctLabel     = root.Q<Label>(_correctLabelName);
                _incorrectLabel   = root.Q<Label>(_incorrectLabelName);
                _questionPanel    = root.Q<VisualElement>(_questionPanelName);
                _backgroundPanel  = root.Q<VisualElement>(_backgroundPanelName);
                _lasers           = root.Q<VisualElement>(_lasersName);
                _instructionLabel = root.Q<VisualElement>(_instructionLabelName);
                _progressCounter  = root.Q<VisualElement>(_progressCounterName);
                _answerCells      = root.Q<VisualElement>(_answerCellsName);
                _hiraganaButtons  = root.Q<VisualElement>(_hiraganaButtonsName);

                var charactersStrip = root.Q<VisualElement>(_charactersStripName);
                if (charactersStrip != null)
                {
                    for (int i = 0; i < CharacterSlotCount; i++)
                        _characterSlots[i] = charactersStrip.Q<VisualElement>($"character-slot-{i}");
                }
            }

            AnswerJudgment.OnJudged
                .Where(_ => GameFlowManager.Instance != null
                         && GameFlowManager.Instance.CurrentPhase == GamePhase.AnswerWindow)
                .Subscribe(HandleJudged)
                .AddTo(this);

            BpmClock.OnBeat
                .Where(_ => GameFlowManager.Instance != null)
                .Subscribe(_ => OnBeat())
                .AddTo(this);

            GameFlowManager.OnPhaseChanged
                .Subscribe(HandlePhaseVfx)
                .AddTo(this);

            GameFlowManager.OnPhaseChanged
                .Where(p => p == GamePhase.Next)
                .Subscribe(_ => CancelVfx())
                .AddTo(this);
        }

        void OnDisable()
        {
            CancelVfx();
        }

        private void HandleJudged(bool isCorrect)
        {
            CancelVfx();
            _vfxCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            if (isCorrect) PlayCorrectSequenceAsync(_vfxCts.Token).Forget();
            else PlayIncorrectSequenceAsync(_vfxCts.Token).Forget();
        }

        private async UniTaskVoid PlayCorrectSequenceAsync(CancellationToken ct)
        {
            try
            {
                if (_confettiParticle != null) _confettiParticle.Play();
                if (_funnymonAnimator != null) _funnymonAnimator.SetTrigger("CorrectDance");
                StartMirrorBall();
                AudioManager.Instance?.PlayCorrectSE();

                // ── 最高潮の盛り上がり演出 ──
                _isCorrectActive = true;
                _dropBeatCounter = 0;
                _correctLabelDanceArmed = false;

                FlashScreen();              // 白フラッシュ
                ShakeScreen();              // 画面シェイク (1 回)
                StartLaserSpin();           // UI レーザー回転
                CelebrateCharacters();      // キャラクター 720° スピン + 大ジャンプ
                _spotlights?.Activate();    // ムービングスポットライト (Light2D)

                if (_correctLabel != null)
                {
                    _correctLabelTween = _correctLabel
                        .DOScale(GameConstants.CORRECT_LABEL_BASE_SCALE, GameConstants.CORRECT_SCALE_DURATION)
                        .SetEase(Ease.OutBack);
                    await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.CORRECT_SCALE_DURATION), cancellationToken: ct);
                    _correctLabelDanceArmed = true; // pop-in 完了後はビート毎にラベルが踊る
                }

                // Drop 終了直前にフェードアウトし、次の Question 開始時には scale 0 に戻っているようにする
                const float fadeOutSec = 0.2f;
                float remain = GameConstants.DROP_REVEAL_SEC - GameConstants.CORRECT_SCALE_DURATION - fadeOutSec;
                if (remain > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(remain), cancellationToken: ct);

                _correctLabelDanceArmed = false; // フェードアウトと衝突しないようダンス停止
                _correctLabelTween?.Kill();
                _correctLabelTween = _correctLabel?.DOScale(0f, fadeOutSec);
                StopMirrorBall();
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid PlayIncorrectSequenceAsync(CancellationToken ct)
        {
            try
            {
                if (_funnymonAnimator != null) _funnymonAnimator.SetTrigger("FailDance");
                AudioManager.Instance?.PlayIncorrectSE();

                // 一瞬静止（スベり感演出）— ignoreTimeScale: true で UniTask はスケール外待機
                Time.timeScale = 0f;
                await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.INCORRECT_FREEZE_SEC),
                    ignoreTimeScale: true, cancellationToken: ct);
                Time.timeScale = 1f;

                // 5体まとめてコケる（左右ランダム方向に倒れて沈み込む）
                TumbleCharacters();

                // 青ざめオーバーレイ（既存）
                _blueOverlayTween = _blueOverlay?.DOFade(GameConstants.BLUE_OVERLAY_ALPHA, GameConstants.INCORRECT_OVERLAY_FADE_SEC);

                // 「ざんねん...」ラベル出現
                if (_incorrectLabel != null)
                {
                    _incorrectLabelTween = _incorrectLabel
                        .DOScale(GameConstants.INCORRECT_LABEL_SCALE_PEAK, GameConstants.INCORRECT_LABEL_FADE_DUR)
                        .SetEase(Ease.OutBack);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.INCORRECT_OVERLAY_DELAY_SEC), cancellationToken: ct);
                _blueOverlayTween = _blueOverlay?.DOFade(0f, GameConstants.INCORRECT_OVERLAY_FADE_SEC);

                // ラベルを暫く見せてからフェードアウト
                await UniTask.Delay(TimeSpan.FromSeconds(GameConstants.INCORRECT_LABEL_HOLD_SEC), cancellationToken: ct);
                _incorrectLabelTween = _incorrectLabel?.DOScale(0f, GameConstants.INCORRECT_LABEL_FADE_DUR);

                float remain = GameConstants.DROP_REVEAL_SEC
                             - GameConstants.INCORRECT_FREEZE_SEC
                             - GameConstants.INCORRECT_OVERLAY_DELAY_SEC
                             - GameConstants.INCORRECT_LABEL_HOLD_SEC;
                if (remain > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(remain), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                Time.timeScale = 1f; // キャンセル時も必ず復元
            }
        }

        private void OnBeat()
        {
            _beatCounter++;
            var phase = GameFlowManager.Instance.CurrentPhase;

            if (phase == GamePhase.BuildUp || phase == GamePhase.AnswerWindow || phase == GamePhase.Next)
            {
                PulseQuestionPanel();
                DanceCharacters();
                DanceBuildUpLights();
            }
            else if (phase == GamePhase.Drop && _isCorrectActive)
            {
                _dropBeatCounter++;
                // 初期 720° スピン (CHARACTER_SPIN_DUR=1.0s) と被らないよう 2 拍目以降から踊らせる
                if (_dropBeatCounter >= 2) DanceCharactersHype();
                DanceUIBeat();
                DanceCorrectLabel();
                _spotlights?.OnBeatTick(_dropBeatCounter);
            }
        }

        private void HandlePhaseVfx(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.BuildUp:
                    _spotlights?.Activate();
                    StartLaserSpin();
                    break;
                case GamePhase.AnswerWindow:
                    _spotlights?.Activate();
                    break;
                case GamePhase.Question:
                case GamePhase.FadeOut:
                case GamePhase.GameEnd:
                case GamePhase.Idle:
                    _spotlights?.Deactivate();
                    StopLaserSpin();
                    break;
            }
        }

        private void DanceBuildUpLights()
        {
            _spotlights?.OnBeatTick(_beatCounter);
        }

        private void EnsureSpotlights()
        {
            if (_spotlights != null) return;

            _spotlights = GetComponent<MovingSpotlightController>();
            if (_spotlights != null || !_autoCreateSpotlights) return;

            _spotlights = gameObject.AddComponent<MovingSpotlightController>();
        }

        private void PulseQuestionPanel()
        {
            if (_questionPanel == null) return;
            float duration = GameConstants.GetBeatDuration() * GameConstants.BEAT_PULSE_DURATION_RATIO;
            _questionPanel.DOPulse(GameConstants.QUESTION_PULSE_SCALE, duration);
        }

        private void DanceCharacters()
        {
            float beatDur  = GameConstants.GetBeatDuration();
            float danceDur = beatDur * 0.95f;

            for (int i = 0; i < _characterSlots.Length; i++)
            {
                var slot = _characterSlots[i];
                if (slot == null) continue;

                // 拍とスロット位置の偶奇でジャンプの大小を切り替え（ウェーブ感）
                bool bigBounce = ((_beatCounter + i) & 1) == 0;
                float bounce = bigBounce
                    ? -GameConstants.DANCE_BOUNCE_PX
                    : -GameConstants.DANCE_BOUNCE_PX * 0.4f;
                _danceBounceTweens[i]?.Kill();
                _danceBounceTweens[i] = slot.DOBounceY(bounce, danceDur);

                // 傾きはバウンス方向と逆位相にして「踊っている感」を強調
                float tilt = bigBounce
                    ? -GameConstants.DANCE_TILT_DEG
                    :  GameConstants.DANCE_TILT_DEG;
                _danceRotateTweens[i]?.Kill();
                _danceRotateTweens[i] = slot.DOSwingRotate(tilt, danceDur);

                // 反転: スロット毎にオフセットをかけて 1 拍に 1 体ずつ振り向く
                if ((_beatCounter + i) % GameConstants.DANCE_FLIP_INTERVAL == 0)
                {
                    _danceFlipTweens[i]?.Kill();
                    _danceFlipTweens[i] = slot.DOFlipX(beatDur * 0.6f);
                }
            }
        }

        private void DanceCharactersHype()
        {
            float beatDur  = GameConstants.GetBeatDuration();
            float danceDur = beatDur * 0.95f;

            for (int i = 0; i < _characterSlots.Length; i++)
            {
                var slot = _characterSlots[i];
                if (slot == null) continue;

                // celebrate spin (720°) を Kill して新しいダンスへ滑らかに切り替え
                _celebrateRotateTweens[i]?.Kill();
                _celebrateBounceTweens[i]?.Kill();

                bool big = ((_dropBeatCounter + i) & 1) == 0;
                float bounce = big ? -GameConstants.HYPE_DANCE_BOUNCE_PX
                                   : -GameConstants.HYPE_DANCE_BOUNCE_PX * 0.5f;
                _danceBounceTweens[i]?.Kill();
                _danceBounceTweens[i] = slot.DOBounceY(bounce, danceDur);

                float tilt = big ? -GameConstants.HYPE_DANCE_TILT_DEG
                                 :  GameConstants.HYPE_DANCE_TILT_DEG;
                _danceRotateTweens[i]?.Kill();
                _danceRotateTweens[i] = slot.DOSwingRotate(tilt, danceDur);

                if ((_dropBeatCounter + i) % GameConstants.HYPE_FLIP_INTERVAL == 0)
                {
                    _danceFlipTweens[i]?.Kill();
                    _danceFlipTweens[i] = slot.DOFlipX(beatDur * 0.5f);
                }
            }
        }

        private void DanceUIBeat()
        {
            float beatDur = GameConstants.GetBeatDuration();
            float dur     = beatDur * 0.9f;

            // staggered: 拍 + UI インデックスの偶奇でスケール大小を切り替え
            PulseUiAt(_questionPanel,    0, dur);
            PulseUiAt(_answerCells,      1, dur);
            PulseUiAt(_hiraganaButtons,  2, dur);
            PulseUiAt(_progressCounter,  3, dur);
            PulseUiAt(_instructionLabel, 4, dur);
        }

        private void PulseUiAt(VisualElement ve, int index, float dur)
        {
            if (ve == null) return;
            bool big = ((_dropBeatCounter + index) & 1) == 0;
            float scale = big ? GameConstants.HYPE_UI_SCALE_BIG : GameConstants.HYPE_UI_SCALE_SMALL;
            ve.DOPulse(scale, dur);
            // 偶数番目だけ軽く左右に揺らす
            if ((index & 1) == 0)
            {
                float tilt = ((_dropBeatCounter & 1) == 0) ? GameConstants.HYPE_UI_TILT_DEG : -GameConstants.HYPE_UI_TILT_DEG;
                ve.DOSwingRotate(tilt, dur);
            }
        }

        private void DanceCorrectLabel()
        {
            if (_correctLabel == null || !_correctLabelDanceArmed) return;
            float beatDur = GameConstants.GetBeatDuration();
            float dur     = beatDur * 0.9f;

            // base scale を中心にパルス（pop-in との衝突回避: pop-in 完了後にしか発火しない）
            _correctLabelTween?.Kill();
            _correctLabelTween = _correctLabel
                .DOScale(GameConstants.CORRECT_LABEL_DANCE_PEAK, dur / 2f)
                .OnComplete(() =>
                {
                    if (_correctLabel == null || !_correctLabelDanceArmed) return;
                    _correctLabelTween = _correctLabel.DOScale(GameConstants.CORRECT_LABEL_BASE_SCALE, dur / 2f);
                });

            float tilt = ((_dropBeatCounter & 1) == 0) ? -GameConstants.CORRECT_LABEL_TILT_DEG : GameConstants.CORRECT_LABEL_TILT_DEG;
            _correctLabel.DOSwingRotate(tilt, dur);
            _correctLabel.DOBounceY(-GameConstants.CORRECT_LABEL_BOUNCE_PX, dur);
        }

        private void TumbleCharacters()
        {
            for (int i = 0; i < _characterSlots.Length; i++)
            {
                var slot = _characterSlots[i];
                if (slot == null) continue;

                // ダンス tween を全 Kill しないとコケポーズが上書きされる
                _danceBounceTweens[i]?.Kill();
                _danceRotateTweens[i]?.Kill();
                _danceFlipTweens[i]?.Kill();
                _tumbleBounceTweens[i]?.Kill();
                _tumbleRotateTweens[i]?.Kill();

                float dir = TumbleDirections[i];
                _tumbleRotateTweens[i] = slot.DORotateTo(dir * GameConstants.TUMBLE_ROTATE_DEG, GameConstants.TUMBLE_DURATION)
                    .SetEase(Ease.InQuad);
                _tumbleBounceTweens[i] = slot.DOTranslateYTo(GameConstants.TUMBLE_FALL_PX, GameConstants.TUMBLE_DURATION)
                    .SetEase(Ease.InCubic);
            }
        }

        private void KillAllCharacterTweens()
        {
            for (int i = 0; i < CharacterSlotCount; i++)
            {
                _danceBounceTweens[i]?.Kill();     _danceBounceTweens[i]     = null;
                _danceRotateTweens[i]?.Kill();     _danceRotateTweens[i]     = null;
                _danceFlipTweens[i]?.Kill();       _danceFlipTweens[i]       = null;
                _tumbleBounceTweens[i]?.Kill();    _tumbleBounceTweens[i]    = null;
                _tumbleRotateTweens[i]?.Kill();    _tumbleRotateTweens[i]    = null;
                _celebrateRotateTweens[i]?.Kill(); _celebrateRotateTweens[i] = null;
                _celebrateBounceTweens[i]?.Kill(); _celebrateBounceTweens[i] = null;
            }
        }

        private void FlashScreen()
        {
            if (_flashOverlay == null) return;
            _flashTween?.Kill();
            _flashTween = _flashOverlay.DOFade(GameConstants.CORRECT_FLASH_ALPHA, GameConstants.CORRECT_FLASH_IN_SEC)
                .OnComplete(() =>
                {
                    if (_flashOverlay == null) return;
                    _flashTween = _flashOverlay.DOFade(0f, GameConstants.CORRECT_FLASH_OUT_SEC);
                });
        }

        private void ShakeScreen()
        {
            if (_backgroundPanel == null) return;
            _shakeTween?.Kill();
            _shakeTween = _backgroundPanel.DOShakeOnce(
                GameConstants.CORRECT_SHAKE_DUR,
                GameConstants.CORRECT_SHAKE_PX,
                GameConstants.CORRECT_SHAKE_VIBRATO);
        }

        private void StartLaserSpin()
        {
            if (_lasers == null) return;
            _laserSpinTween?.Kill();
            float dur = GameConstants.GetBeatDuration() * GameConstants.BEATS_PER_BAR * GameConstants.LASER_SPIN_BARS_PER_TURN;
            _laserSpinTween = _lasers.DORotateTo(360f, dur)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopLaserSpin()
        {
            _laserSpinTween?.Kill();
            _laserSpinTween = null;
            if (_lasers != null)
                _lasers.style.rotate = new StyleRotate(StyleKeyword.Null);
        }

        private void CelebrateCharacters()
        {
            for (int i = 0; i < _characterSlots.Length; i++)
            {
                var slot = _characterSlots[i];
                if (slot == null) continue;

                // ダンスやコケ Tween を一旦停止 — お祝いスピンへ滑らかに切り替え
                _danceBounceTweens[i]?.Kill();
                _danceRotateTweens[i]?.Kill();
                _danceFlipTweens[i]?.Kill();
                _tumbleBounceTweens[i]?.Kill();
                _tumbleRotateTweens[i]?.Kill();
                _celebrateRotateTweens[i]?.Kill();
                _celebrateBounceTweens[i]?.Kill();

                // 720° スピン（2 周）+ 大ジャンプ
                _celebrateRotateTweens[i] = slot.DORotateTo(GameConstants.CHARACTER_SPIN_DEG, GameConstants.CHARACTER_SPIN_DUR)
                    .SetEase(Ease.OutCubic);
                _celebrateBounceTweens[i] = slot.DOBounceY(-GameConstants.CHARACTER_HYPE_BOUNCE_PX, GameConstants.CHARACTER_SPIN_DUR);
            }
        }

        private void DanceUI()
        {
            float dur = GameConstants.UI_HYPE_DURATION;
            float peak = GameConstants.UI_HYPE_SCALE_PEAK;
            _questionPanel?.DOPulse(peak, dur);
            _answerCells?.DOPulse(peak, dur);
            _hiraganaButtons?.DOPulse(peak, dur);
            _progressCounter?.DOPulse(peak, dur);
            _instructionLabel?.DOPulse(peak, dur);
        }

        private void StartMirrorBall()
        {
            if (_mirrorBallTransform == null) return;
            _mirrorBallTween?.Kill();
            _mirrorBallTween = _mirrorBallTransform
                .DORotate(new Vector3(0, 360, 0),
                          GameConstants.GetBeatDuration() * 4f,
                          RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }

        private void StopMirrorBall()
        {
            _mirrorBallTween?.Kill();
            _mirrorBallTween = null;
        }

        private void CancelVfx()
        {
            _vfxCts?.Cancel();
            _vfxCts?.Dispose();
            _vfxCts = null;
            _correctLabelTween?.Kill();   _correctLabelTween   = null;
            _incorrectLabelTween?.Kill(); _incorrectLabelTween = null;
            _blueOverlayTween?.Kill();    _blueOverlayTween    = null;
            _flashTween?.Kill();          _flashTween          = null;
            _shakeTween?.Kill();          _shakeTween          = null;
            StopLaserSpin();

            // 継続演出フラグをリセット + スポットライト停止
            _isCorrectActive = false;
            _correctLabelDanceArmed = false;
            _spotlights?.Deactivate();

            // 正解ラベルの translate / rotate inline をクリア（ダンスで書き込んだ分）
            if (_correctLabel != null)
            {
                _correctLabel.style.translate = new StyleTranslate(StyleKeyword.Null);
                _correctLabel.style.rotate    = new StyleRotate(StyleKeyword.Null);
            }

            KillAllCharacterTweens();

            // ラベルを scale 0 に戻す（USS の `scale: 0 0` を復活させるため inline をクリア）
            if (_correctLabel   != null) _correctLabel.style.scale   = new StyleScale(StyleKeyword.Null);
            if (_incorrectLabel != null) _incorrectLabel.style.scale = new StyleScale(StyleKeyword.Null);

            // 画面シェイク・フラッシュ・レーザー回転の inline をクリア
            if (_backgroundPanel != null) _backgroundPanel.style.translate = new StyleTranslate(StyleKeyword.Null);
            if (_flashOverlay    != null) _flashOverlay.style.opacity      = new StyleFloat(StyleKeyword.Null);
            if (_lasers          != null) _lasers.style.rotate             = new StyleRotate(StyleKeyword.Null);

            // UI ダンス対象の scale inline をクリア
            if (_questionPanel    != null) _questionPanel.style.scale    = new StyleScale(StyleKeyword.Null);
            if (_answerCells      != null) _answerCells.style.scale      = new StyleScale(StyleKeyword.Null);
            if (_hiraganaButtons  != null) _hiraganaButtons.style.scale  = new StyleScale(StyleKeyword.Null);
            if (_progressCounter  != null) _progressCounter.style.scale  = new StyleScale(StyleKeyword.Null);
            if (_instructionLabel != null) _instructionLabel.style.scale = new StyleScale(StyleKeyword.Null);

            // キャラクター slot の inline style をクリアして USS の baseline rotate へ戻す
            foreach (var slot in _characterSlots)
            {
                if (slot == null) continue;
                slot.style.translate = new StyleTranslate(StyleKeyword.Null);
                slot.style.rotate    = new StyleRotate(StyleKeyword.Null);
                slot.style.scale     = new StyleScale(StyleKeyword.Null);
            }

            StopMirrorBall();
        }
    }
}

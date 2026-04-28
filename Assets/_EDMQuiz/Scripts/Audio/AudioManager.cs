using CriWare;
using NaughtyAttributes;
using UnityEngine;

namespace EDMQuiz
{
    /// <summary>CRI ADX による BGM/SE 再生 + BPM 同期用クロック提供
    /// <remarks>
    /// Asset Support Addon を使わない標準実装版。CueSheet 名 + Cue 名で参照する。
    /// CriAtomServer / CriWareInitializer 経由で ACB がロード済みであることが前提。
    /// シーンに `CriAtom` GameObject を配置し CueSheet を登録するか、Awake で AddCueSheet するか選択。
    /// </remarks>
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [BoxGroup("Cue Sheet")]
        [SerializeField] private string _bgmCueSheetName = "BGM";
        [BoxGroup("Cue Sheet")]
        [SerializeField] private string _seCueSheetName  = "SE";

        [BoxGroup("Cue Names")]
        [SerializeField] private string _bgmCueName        = "BGM_MAIN";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seCorrectCueName  = "SE_CORRECT";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seIncorrectCueName = "SE_INCORRECT";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seUiTapCueName    = "SE_UI_TAP";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seResultCueName   = "SE_RESULT";

        private CriAtomExPlayer _bgmPlayer;
        private CriAtomExPlayer _sePlayer;
        private CriAtomExPlayback _bgmPlayback;

        public bool IsBgmPlaying { get; private set; }

        private double _fallbackStartTime;
        private bool _useFallback;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                _bgmPlayer = new CriAtomExPlayer();
                _sePlayer  = new CriAtomExPlayer();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] CRI ADX 未初期化 — 音なしフォールバックで動作 ({e.Message})");
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                _bgmPlayer?.Dispose();
                _sePlayer?.Dispose();
                Instance = null;
            }
        }

        [Button("Play BGM (Editor Test)")]
        public void PlayBGM()
        {
            try
            {
                var acb = CriAtom.GetAcb(_bgmCueSheetName);
                if (acb == null)
                {
                    Debug.LogWarning($"[AudioManager] CueSheet '{_bgmCueSheetName}' 未ロード — フォールバック動作");
                    UseFallback();
                    return;
                }
                _bgmPlayer.SetCue(acb, _bgmCueName);
                _bgmPlayback = _bgmPlayer.Start();
                IsBgmPlaying = true;
                _useFallback = false;
                BpmClock.Instance?.StartClock();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] CRI ADX 未初期化 — フォールバック動作 ({e.Message})");
                UseFallback();
            }
        }

        private void UseFallback()
        {
            _useFallback = true;
            _fallbackStartTime = Time.unscaledTimeAsDouble;
            IsBgmPlaying = true;
            BpmClock.Instance?.StartClock();
        }

        public void StopBGM()
        {
            _bgmPlayer?.Stop();
            IsBgmPlaying = false;
        }

        public void PlaySE(string cueName)
        {
            if (string.IsNullOrEmpty(cueName)) return;
            try
            {
                var acb = CriAtom.GetAcb(_seCueSheetName);
                if (acb == null) return;
                _sePlayer.SetCue(acb, cueName);
                _sePlayer.Start();
            }
            catch (System.Exception) { /* CRI 未初期化時は無音で続行 */ }
        }

        public void PlayCorrectSE()   => PlaySE(_seCorrectCueName);
        public void PlayIncorrectSE() => PlaySE(_seIncorrectCueName);
        public void PlayUiTapSE()     => PlaySE(_seUiTapCueName);
        public void PlayResultSE()    => PlaySE(_seResultCueName);

        /// <summary>BGM 再生開始からの経過秒数（リズム同期用、サンプル精度）</summary>
        public double GetBGMElapsedSeconds()
        {
            if (!IsBgmPlaying) return 0.0;

            if (_useFallback)
                return Time.unscaledTimeAsDouble - _fallbackStartTime;

            if (_bgmPlayback.GetStatus() != CriAtomExPlayback.Status.Playing)
                return 0.0;

            long us = _bgmPlayback.GetTimeSyncedWithAudio();
            return us / 1_000_000.0;
        }
    }
}

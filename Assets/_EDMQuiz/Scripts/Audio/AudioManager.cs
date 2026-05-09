using System.Threading;
using Cysharp.Threading.Tasks;
using CriWare;
using NaughtyAttributes;
using UnityEngine;

namespace EDMQuiz
{
    /// <summary>CRI ADX による BGM/SE 再生 + BPM 同期用クロック提供</summary>
    /// <remarks>
    /// 暫定版（Asset Support Addon 未インストール）。CueSheet 名 + Cue 名で参照する。
    /// Asset Support Addon インストール後は CriAtomCueReference パターンへ移行すること。
    /// CueSheet が未ロードの場合は AudioSource フォールバックで動作継続。
    /// </remarks>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [BoxGroup("Cue Sheet")]
        [SerializeField] private string _bgmCueSheetName = "BGM";
        [BoxGroup("Cue Sheet")]
        [SerializeField] private string _seCueSheetName  = "SE";

        [BoxGroup("Cue Names")]
        [SerializeField] private string _bgmCueName         = "BGM_MAIN";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seCorrectCueName   = "SE_CORRECT";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seIncorrectCueName = "SE_INCORRECT";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seUiTapCueName     = "SE_UI_TAP";
        [BoxGroup("Cue Names")]
        [SerializeField] private string _seResultCueName    = "SE_RESULT";

        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip _bgmFallbackClip;
        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip _questionIntroClip;

        private CriAtomExPlayer _bgmPlayer;
        private CriAtomExPlayer _sePlayer;
        private CriAtomExPlayback _bgmPlayback;
        private AudioSource _audioSource;
        private AudioSource _questionIntroAudioSource;

        public bool IsBgmPlaying { get; private set; }

        private bool _useFallback;
        private bool _loopBgm;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Instance.ApplySceneSettingsFrom(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = CreateAudioSource(loop: true);

            try
            {
                _bgmPlayer = new CriAtomExPlayer();
                _sePlayer  = new CriAtomExPlayer();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] CRI ADX 未初期化 — AudioSource フォールバックで動作 ({e.Message})");
            }
        }

        private void ApplySceneSettingsFrom(AudioManager src)
        {
            if (src == null) return;
            if (!string.IsNullOrEmpty(src._bgmCueSheetName))    _bgmCueSheetName    = src._bgmCueSheetName;
            if (!string.IsNullOrEmpty(src._seCueSheetName))     _seCueSheetName     = src._seCueSheetName;
            if (!string.IsNullOrEmpty(src._bgmCueName))         _bgmCueName         = src._bgmCueName;
            if (!string.IsNullOrEmpty(src._seCorrectCueName))   _seCorrectCueName   = src._seCorrectCueName;
            if (!string.IsNullOrEmpty(src._seIncorrectCueName)) _seIncorrectCueName = src._seIncorrectCueName;
            if (!string.IsNullOrEmpty(src._seUiTapCueName))     _seUiTapCueName     = src._seUiTapCueName;
            if (!string.IsNullOrEmpty(src._seResultCueName))    _seResultCueName    = src._seResultCueName;
            if (src._bgmFallbackClip != null)    _bgmFallbackClip    = src._bgmFallbackClip;
            if (src._questionIntroClip != null)  _questionIntroClip  = src._questionIntroClip;
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
        public void PlayBGM(bool looped = false)
        {
            _loopBgm = looped;
            try
            {
                var acb = CriAtom.GetAcb(_bgmCueSheetName);
                if (acb == null)
                {
                    Debug.LogWarning($"[AudioManager] CueSheet '{_bgmCueSheetName}' 未ロード — フォールバック動作");
                    UseFallback(looped);
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
                Debug.LogWarning($"[AudioManager] CRI ADX 再生失敗 — フォールバック動作 ({e.Message})");
                UseFallback(looped);
            }
        }

        void Update()
        {
            if (!_loopBgm || _useFallback || _bgmPlayer == null) return;
            try
            {
                if (_bgmPlayback.GetStatus() == CriAtomExPlayback.Status.Removed)
                {
                    var acb = CriAtom.GetAcb(_bgmCueSheetName);
                    if (acb == null) return;
                    _bgmPlayer.SetCue(acb, _bgmCueName);
                    _bgmPlayback = _bgmPlayer.Start();
                }
            }
            catch (System.Exception) { }
        }

        private void UseFallback(bool looped = false)
        {
            _useFallback = true;
            IsBgmPlaying = true;
            if (_bgmFallbackClip != null)
            {
                if (_audioSource != null) Destroy(_audioSource);
                _audioSource = CreateAudioSource(loop: looped);
                _audioSource.clip = _bgmFallbackClip;
                _audioSource.Play();
            }
            BpmClock.Instance?.StartClock();
        }

        public void StopBGM()
        {
            _loopBgm = false;
            _bgmPlayer?.Stop();
            _audioSource?.Stop();
            IsBgmPlaying = false;
            BpmClock.Instance?.StopClock();
        }

        /// <summary>BGM をゆっくりフェードアウトさせる。完了後の Stop は呼び出し側の責務。</summary>
        public async UniTask FadeBgmOutAsync(float duration, CancellationToken ct)
        {
            if (!IsBgmPlaying || duration <= 0f) return;

            if (_useFallback)
            {
                if (_audioSource == null) return;
                float startVol = _audioSource.volume;
                float t = 0f;
                while (t < duration && !ct.IsCancellationRequested)
                {
                    t += Time.unscaledDeltaTime;
                    _audioSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / duration));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                _audioSource.volume = startVol;
                return;
            }

            if (_bgmPlayer == null) return;
            float tf = 0f;
            while (tf < duration && !ct.IsCancellationRequested)
            {
                tf += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(1f, 0f, Mathf.Clamp01(tf / duration));
                _bgmPlayer.SetVolume(v);
                _bgmPlayer.Update(_bgmPlayback);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            _bgmPlayer.SetVolume(1f);
        }

        public float PlayQuestionIntroSE()
        {
            if (_questionIntroClip == null) return 0f;
            if (_questionIntroAudioSource == null)
                _questionIntroAudioSource = CreateAudioSource(loop: false);
            _questionIntroAudioSource.Stop();
            _questionIntroAudioSource.clip = _questionIntroClip;
            _questionIntroAudioSource.Play();
            return _questionIntroClip.length;
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
            catch (System.Exception) { }
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
                return _audioSource != null && _audioSource.isPlaying ? _audioSource.time : 0.0;
            if (_bgmPlayback.GetStatus() != CriAtomExPlayback.Status.Playing) return 0.0;
            long us = _bgmPlayback.GetTimeSyncedWithAudio();
            return us / 1_000_000.0;
        }

        private AudioSource CreateAudioSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}

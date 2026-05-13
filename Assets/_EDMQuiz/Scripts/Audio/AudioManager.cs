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
        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip[] _incorrectBgmClips;
        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip _correctSeClip;
        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip _incorrectSeClip;
        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip _uiTapSeClip;
        [BoxGroup("Fallback")]
        [SerializeField] private AudioClip _resultSeClip;

        private CriAtomExPlayer _bgmPlayer;
        private CriAtomExPlayer _sePlayer;
        private CriAtomExPlayback _bgmPlayback;
        private AudioSource _audioSource;
        private AudioSource _questionIntroAudioSource;
        private AudioSource _incorrectAudioSource;
        private AudioSource _seAudioSource;

        private const float INCORRECT_BGM_CROSSFADE_SEC = 0.15f;
        private const float BGM_VOLUME = 0.6f;

        public bool IsBgmPlaying { get; private set; }

        private bool _useFallback;
        private bool _loopBgm;

        // BGM_MAIN ブロックシーケンス構成（AtomCraft 側と整合させる）
        // Block 0 = Main / Block 1 = Incorrect_A / Block 2 = Incorrect_B
        private const int BGM_BLOCK_INCORRECT_MIN = 1;  // 含む
        private const int BGM_BLOCK_INCORRECT_MAX = 3;  // 含まない

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
            if (src._incorrectBgmClips != null && src._incorrectBgmClips.Length > 0)
                _incorrectBgmClips = src._incorrectBgmClips;
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
            // 直前の不正解 BGM が残っていれば停止（次問題は通常 BGM 先頭から）
            if (_incorrectAudioSource != null)
            {
                _incorrectAudioSource.Stop();
                _incorrectAudioSource.volume = BGM_VOLUME;
            }
            if (_audioSource != null) _audioSource.volume = BGM_VOLUME;

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
                _bgmPlayer.SetVolume(BGM_VOLUME);
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
                // AudioSource は 1 個を使い回す（Destroy + Create はやらない）
                if (_audioSource == null) _audioSource = CreateAudioSource(loop: looped);
                _audioSource.Stop();
                _audioSource.loop = looped;
                _audioSource.clip = _bgmFallbackClip;
                _audioSource.volume = BGM_VOLUME;
                _audioSource.time = 0f;
                _audioSource.Play();
            }
            BpmClock.Instance?.StartClock();
        }

        public void StopBGM()
        {
            _loopBgm = false;
            _bgmPlayer?.Stop();
            _audioSource?.Stop();
            _incorrectAudioSource?.Stop();
            // 長尺 clip を PlayOneShot した場合の取りこぼし対策
            _seAudioSource?.Stop();
            IsBgmPlaying = false;
            BpmClock.Instance?.StopClock();
        }

        /// <summary>
        /// 不正解 BGM への切替を要求する。
        /// CRI ADX 動作中: BGM_MAIN ブロックシーケンスの不正解ブロック（A/B のランダム選択）へ次境界で遷移予約。
        /// AudioSource フォールバック動作中: _incorrectBgmClips からランダム選択した AudioClip を別 AudioSource でクロスフェード再生。
        /// BGM 停止中・素材未設定時は何もしない。
        /// </summary>
        public void RequestIncorrectBgmBlock()
        {
            if (!IsBgmPlaying) return;

            // CRI ADX パス（AtomCraft で BGM_MAIN がブロックシーケンス化されている場合）
            if (!_useFallback && _bgmPlayer != null)
            {
                try
                {
                    if (_bgmPlayback.GetStatus() == CriAtomExPlayback.Status.Playing)
                    {
                        int blockIndex = UnityEngine.Random.Range(BGM_BLOCK_INCORRECT_MIN, BGM_BLOCK_INCORRECT_MAX);
                        _bgmPlayback.SetNextBlockIndex(blockIndex);
                        return;
                    }
                }
                catch (System.Exception) { }
            }

            // AudioSource フォールバックパス（クロスフェード）
            if (_incorrectBgmClips == null || _incorrectBgmClips.Length == 0) return;
            var clip = _incorrectBgmClips[UnityEngine.Random.Range(0, _incorrectBgmClips.Length)];
            if (clip == null) return;

            if (_incorrectAudioSource == null)
                _incorrectAudioSource = CreateAudioSource(loop: false);

            _incorrectAudioSource.Stop();
            _incorrectAudioSource.clip = clip;
            _incorrectAudioSource.volume = 0f;
            _incorrectAudioSource.Play();

            CrossfadeToIncorrectBgmAsync(INCORRECT_BGM_CROSSFADE_SEC).Forget();
        }

        private async UniTaskVoid CrossfadeToIncorrectBgmAsync(float duration)
        {
            if (_audioSource == null || _incorrectAudioSource == null) return;
            float startMain = _audioSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                if (_audioSource != null)          _audioSource.volume          = Mathf.Lerp(startMain, 0f, k);
                if (_incorrectAudioSource != null) _incorrectAudioSource.volume = Mathf.Lerp(0f, BGM_VOLUME, k);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            if (_audioSource != null) _audioSource.Stop();
        }

        /// <summary>BGM をゆっくりフェードアウトさせる。完了後の Stop は呼び出し側の責務。</summary>
        public async UniTask FadeBgmOutAsync(float duration, CancellationToken ct)
        {
            if (!IsBgmPlaying || duration <= 0f) return;

            if (_useFallback)
            {
                float startMain = _audioSource != null ? _audioSource.volume : 0f;
                float startInc  = _incorrectAudioSource != null ? _incorrectAudioSource.volume : 0f;
                float t = 0f;
                while (t < duration && !ct.IsCancellationRequested)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / duration);
                    if (_audioSource != null)          _audioSource.volume          = Mathf.Lerp(startMain, 0f, k);
                    if (_incorrectAudioSource != null) _incorrectAudioSource.volume = Mathf.Lerp(startInc,  0f, k);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                // フェード完了後は 0 に固定（呼び出し側の StopBGM で停止される）
                if (_audioSource != null)          _audioSource.volume          = 0f;
                if (_incorrectAudioSource != null) _incorrectAudioSource.volume = 0f;
                return;
            }

            if (_bgmPlayer == null) return;
            float tf = 0f;
            while (tf < duration && !ct.IsCancellationRequested)
            {
                tf += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(BGM_VOLUME, 0f, Mathf.Clamp01(tf / duration));
                _bgmPlayer.SetVolume(v);
                _bgmPlayer.Update(_bgmPlayback);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            _bgmPlayer.SetVolume(BGM_VOLUME);
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
            PlaySE(cueName, fallbackClip: null);
        }

        private void PlaySE(string cueName, AudioClip fallbackClip)
        {
            // CRI ADX 経路（ACB がロード済みなら最優先）
            if (!string.IsNullOrEmpty(cueName) && _sePlayer != null)
            {
                try
                {
                    var acb = CriAtom.GetAcb(_seCueSheetName);
                    if (acb != null)
                    {
                        _sePlayer.SetCue(acb, cueName);
                        _sePlayer.Start();
                        return;
                    }
                }
                catch (System.Exception) { /* fall through to AudioSource fallback */ }
            }

            // AudioSource フォールバック（WebGL で CRI が初期化失敗していても鳴らす）
            if (fallbackClip == null)
            {
                Debug.LogWarning($"[AudioManager] SE '{cueName}' フォールバック未アサイン — 無音");
                return;
            }
            if (_seAudioSource == null) _seAudioSource = CreateAudioSource(loop: false);
            _seAudioSource.PlayOneShot(fallbackClip);
        }

        public void PlayCorrectSE()   => PlaySE(_seCorrectCueName,   _correctSeClip);
        public void PlayIncorrectSE() => PlaySE(_seIncorrectCueName, _incorrectSeClip);
        public void PlayUiTapSE()     => PlaySE(_seUiTapCueName,     _uiTapSeClip);
        public void PlayResultSE()    => PlaySE(_seResultCueName,    _resultSeClip);

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

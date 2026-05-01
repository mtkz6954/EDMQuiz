using System.Threading;
using Cysharp.Threading.Tasks;
using CriWare;
using NaughtyAttributes;
using UnityEngine;

namespace EDMQuiz
{
    /// <summary>CRI ADX による BGM/SE 再生 + BPM 同期用クロック提供</summary>
    /// <remarks>
    /// 標準実装版（Asset Support Addon なし）。CueSheet 名 + Cue 名で参照する。
    /// シーンの CriAtom コンポーネントに "BGM" / "SE" CueSheet が登録済みであることが前提。
    /// CueSheet が未ロードの場合は Time.unscaledTime ベースのフォールバックで動作継続（音なし）。
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

            // HotReload でスクリプトが再ロードされると private フィールドはリセットされるが
            // AudioSource コンポーネント自体は GameObject に残るので GetComponent で復元する
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = CreateAudioSource(loop: true);
            }

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

        private void ApplySceneSettingsFrom(AudioManager sceneAudioManager)
        {
            if (sceneAudioManager == null) return;

            if (!string.IsNullOrEmpty(sceneAudioManager._bgmCueSheetName))
                _bgmCueSheetName = sceneAudioManager._bgmCueSheetName;

            if (!string.IsNullOrEmpty(sceneAudioManager._seCueSheetName))
                _seCueSheetName = sceneAudioManager._seCueSheetName;

            if (!string.IsNullOrEmpty(sceneAudioManager._bgmCueName))
                _bgmCueName = sceneAudioManager._bgmCueName;

            if (!string.IsNullOrEmpty(sceneAudioManager._seCorrectCueName))
                _seCorrectCueName = sceneAudioManager._seCorrectCueName;

            if (!string.IsNullOrEmpty(sceneAudioManager._seIncorrectCueName))
                _seIncorrectCueName = sceneAudioManager._seIncorrectCueName;

            if (!string.IsNullOrEmpty(sceneAudioManager._seUiTapCueName))
                _seUiTapCueName = sceneAudioManager._seUiTapCueName;

            if (!string.IsNullOrEmpty(sceneAudioManager._seResultCueName))
                _seResultCueName = sceneAudioManager._seResultCueName;

            if (sceneAudioManager._bgmFallbackClip != null)
                _bgmFallbackClip = sceneAudioManager._bgmFallbackClip;

            if (sceneAudioManager._questionIntroClip != null)
                _questionIntroClip = sceneAudioManager._questionIntroClip;
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
                Debug.LogWarning($"[AudioManager] CRI ADX 再生失敗 — フォールバック動作 ({e.Message})");
                UseFallback();
            }
        }

        private void UseFallback()
        {
            _useFallback = true;
            IsBgmPlaying = true;
            if (_bgmFallbackClip != null)
            {
                // CriWareInitializer の初期化タイミングで既存の AudioSource が無効になる場合があるため
                // 再生直前に新しい AudioSource を生成して確実に鳴らす
                if (_audioSource != null) Destroy(_audioSource);
                _audioSource = CreateAudioSource(loop: true);
                _audioSource.clip = _bgmFallbackClip;
                _audioSource.Play();
            }
            BpmClock.Instance?.StartClock();
        }

        public void StopBGM()
        {
            _bgmPlayer?.Stop();
            _audioSource?.Stop();
            IsBgmPlaying = false;
            BpmClock.Instance?.StopClock();
        }

        /// <summary>BGM をゆっくりフェードアウトさせる。完了後の Stop は呼び出し側の責務。</summary>
        /// <remarks>
        /// CRI ADX: SetVolume + Update(playback) で再生中の音量を反映。
        /// フォールバック AudioSource: 毎フレーム volume を Lerp。
        /// </remarks>
        public async UniTask FadeBgmOutAsync(float duration, CancellationToken ct)
        {
            if (!IsBgmPlaying) return;
            if (duration <= 0f) return;

            if (_useFallback)
            {
                if (_audioSource == null) return;
                float startVolFallback = _audioSource.volume;
                float tFallback = 0f;
                while (tFallback < duration && !ct.IsCancellationRequested)
                {
                    tFallback += Time.unscaledDeltaTime;
                    _audioSource.volume = Mathf.Lerp(startVolFallback, 0f,
                        Mathf.Clamp01(tFallback / duration));
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                _audioSource.volume = startVolFallback; // 次回再生のために元のボリュームへ復帰
                return;
            }

            if (_bgmPlayer == null) return;

            float t = 0f;
            while (t < duration && !ct.IsCancellationRequested)
            {
                t += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / duration));
                _bgmPlayer.SetVolume(v);
                _bgmPlayer.Update(_bgmPlayback);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // 次回 PlayBGM のためにボリュームをリセット
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
                return _audioSource != null && _audioSource.isPlaying
                    ? _audioSource.time
                    : 0.0;

            if (_bgmPlayback.GetStatus() != CriAtomExPlayback.Status.Playing)
                return 0.0;

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

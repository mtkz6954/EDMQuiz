using UnityEngine;
// CRI ADX をインポート後に有効化：
// using CriWare;
// using CriWare.Assets;

namespace EDMQuiz
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // CRI ADX インポート後にコメントアウトを解除する
        // [Header("BGM CueReference")]
        // public CriAtomCueReference bgmCueRef;
        // [Header("SE CueReference")]
        // public CriAtomCueReference seCorrect;
        // public CriAtomCueReference seIncorrect;
        // public CriAtomCueReference seButtonTap;
        // public CriAtomCueReference seGameStart;
        // public CriAtomCueReference seGameEnd;

        // private CriAtomExPlayer _bgmPlayer;
        // private CriAtomExPlayer _sePlayer;

        /// <summary>BGM再生開始時刻（BpmClockが参照）</summary>
        public float BgmStartDspTime { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            // _bgmPlayer = new CriAtomExPlayer();
            // _sePlayer  = new CriAtomExPlayer();
        }

        public void PlayBGM()
        {
            BgmStartDspTime = (float)AudioSettings.dspTime;
            // _bgmPlayer.SetCue(bgmCueRef.AcbAsset.Handle, bgmCueRef.CueId);
            // _bgmPlayer.Start();
        }

        public void StopBGM()
        {
            // _bgmPlayer.Stop();
        }

        // public void PlaySE(CriAtomCueReference cueRef)
        // {
        //     _sePlayer.SetCue(cueRef.AcbAsset.Handle, cueRef.CueId);
        //     _sePlayer.Start();
        // }

        /// <summary>BGM先頭からの経過秒数（BpmClockが毎フレーム呼ぶ）</summary>
        public float GetCurrentDspTime()
        {
            return (float)AudioSettings.dspTime - BgmStartDspTime;
        }

        // void OnDestroy()
        // {
        //     _bgmPlayer?.Dispose();
        //     _sePlayer?.Dispose();
        // }
    }
}

using R3;
using UnityEngine;

namespace EDMQuiz
{
    /// <summary>BGM 再生時刻を基準にビート・小節境界を検出して R3 Subject で発火</summary>
    public class BpmClock : MonoBehaviour
    {
        public static BpmClock Instance { get; private set; }

        public double ElapsedSeconds { get; private set; }
        public double ElapsedBeats   { get; private set; }
        public int    CurrentBeat    { get; private set; }
        public int    CurrentBar     { get; private set; }

        private static readonly Subject<Unit> _onBeatSubject = new();
        private static readonly Subject<Unit> _onBarSubject  = new();
        public static Observable<Unit> OnBeat => _onBeatSubject;
        public static Observable<Unit> OnBar  => _onBarSubject;

        private bool _isRunning;
        private double _prevElapsedBeats;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartClock()
        {
            _isRunning = true;
            _prevElapsedBeats = 0.0;
            ElapsedSeconds = 0.0;
            ElapsedBeats   = 0.0;
            CurrentBeat    = 0;
            CurrentBar     = 0;
        }

        public void StopClock() => _isRunning = false;

        void Update()
        {
            if (!_isRunning || AudioManager.Instance == null) return;

            ElapsedSeconds = AudioManager.Instance.GetBGMElapsedSeconds();

            double beatDuration = GameConstants.GetBeatDuration();
            if (beatDuration <= 0.0) return;

            ElapsedBeats = ElapsedSeconds / beatDuration;
            CurrentBeat  = (int)ElapsedBeats % GameConstants.BEATS_PER_BAR;
            CurrentBar   = (int)(ElapsedBeats / GameConstants.BEATS_PER_BAR);

            DetectBoundaries();
            _prevElapsedBeats = ElapsedBeats;
        }

        private void DetectBoundaries()
        {
            int prevBeatInt = (int)_prevElapsedBeats;
            int currBeatInt = (int)ElapsedBeats;
            if (currBeatInt > prevBeatInt)
            {
                _onBeatSubject.OnNext(Unit.Default);
                if (currBeatInt % GameConstants.BEATS_PER_BAR == 0)
                    _onBarSubject.OnNext(Unit.Default);
            }
        }
    }
}

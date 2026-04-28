using System;
using UnityEngine;

namespace EDMQuiz
{
    public class BpmClock : MonoBehaviour
    {
        public static event Action OnBeat;
        public static event Action OnBuildUpStart;
        public static event Action OnDropStart;

        private float _beatInterval;
        private float _nextBeatTime;
        private bool  _buildUpFired;
        private bool  _dropFired;
        private bool  _isRunning;

        void Start()
        {
            _beatInterval = 60f / GameConstants.BPM;
        }

        /// <summary>GameFlowManager.StartGame() から呼ばれる</summary>
        public void StartClock()
        {
            _buildUpFired = false;
            _dropFired    = false;
            _isRunning    = true;
            _nextBeatTime = AudioManager.Instance.BgmStartDspTime + _beatInterval;
        }

        /// <summary>次の問題への遷移時にリセット</summary>
        public void ResetClock()
        {
            _buildUpFired = false;
            _dropFired    = false;
            _nextBeatTime = AudioManager.Instance.BgmStartDspTime + _beatInterval;
        }

        void Update()
        {
            if (!_isRunning || AudioManager.Instance == null) return;

            float currentTime = AudioManager.Instance.GetCurrentDspTime();

            while (currentTime >= _nextBeatTime - AudioManager.Instance.BgmStartDspTime)
            {
                OnBeat?.Invoke();
                _nextBeatTime += _beatInterval;
            }

            if (!_buildUpFired && currentTime >= GameConstants.BUILDUP_START_SEC)
            {
                OnBuildUpStart?.Invoke();
                _buildUpFired = true;
            }

            if (!_dropFired && currentTime >= GameConstants.DROP_START_SEC)
            {
                OnDropStart?.Invoke();
                _dropFired = true;
            }
        }
    }
}

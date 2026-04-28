using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EDMQuiz
{
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _rankLabelText;
        [SerializeField] private Button _replayButton;

        void OnEnable()
        {
            GameFlowManager.OnGameEnd += ShowResult;
            _replayButton.onClick.AddListener(OnReplayPressed);
        }

        void OnDisable()
        {
            GameFlowManager.OnGameEnd -= ShowResult;
            _replayButton.onClick.RemoveListener(OnReplayPressed);
        }

        private void ShowResult(int correctCount)
        {
            int score = CalcScore(correctCount);
            var (rank, label) = CalcRank(score);
            StartCoroutine(AnimateResult(score, rank, label));
        }

        private IEnumerator AnimateResult(int score, string rank, string label)
        {
            _resultPanel.SetActive(true);
            _replayButton.gameObject.SetActive(false);

            var canvasGroup = _resultPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.DOFade(1f, 0.5f);
            }

            yield return new WaitForSeconds(0.5f);

            _rankText.text      = rank;
            _rankLabelText.text = label;
            _rankText.transform.DOPunchScale(Vector3.one * 1.0f, 0.5f);

            yield return new WaitForSeconds(0.5f);

            // スコアカウントアップ
            int current = 0;
            float elapsed = 0f;
            float duration = 1.0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                current = Mathf.RoundToInt(Mathf.Lerp(0, score, elapsed / duration));
                _scoreText.text = $"盛り上がり度: {current}";
                yield return null;
            }
            _scoreText.text = $"盛り上がり度: {score}";

            yield return new WaitForSeconds(0.5f);
            _replayButton.gameObject.SetActive(true);
        }

        private void OnReplayPressed()
        {
            _resultPanel.SetActive(false);
            GameFlowManager.Instance.StartGame();
        }

        private static int CalcScore(int correctCount)
        {
            return Mathf.Clamp(correctCount, 0, GameConstants.TOTAL_QUESTIONS) * 100 / GameConstants.TOTAL_QUESTIONS;
        }

        private static (string rank, string label) CalcRank(int score)
        {
            if (score >= GameConstants.RANK_S) return ("S", "神");
            if (score >= GameConstants.RANK_A) return ("A", "最高");
            if (score >= GameConstants.RANK_B) return ("B", "いい感じ");
            if (score >= GameConstants.RANK_C) return ("C", "まあまあ");
            return ("D", "スベった");
        }
    }
}

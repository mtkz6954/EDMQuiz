using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz
{
    /// <summary>UI Toolkit (VisualElement) と DOTween を繋ぐヘルパー</summary>
    public static class UIToolkitTweenExtensions
    {
        public static Tween DOScale(this VisualElement ve, float to, float duration)
        {
            float from = ve.resolvedStyle.scale.value.x;
            return DOVirtual.Float(from, to, duration, v =>
            {
                if (ve == null) return;
                ve.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
            });
        }

        public static Tween DOFade(this VisualElement ve, float to, float duration)
        {
            float from = ve.resolvedStyle.opacity;
            return DOVirtual.Float(from, to, duration, v =>
            {
                if (ve == null) return;
                ve.style.opacity = v;
            });
        }

        public static Tween DOPulse(this VisualElement ve, float peak, float duration)
        {
            return ve.DOScale(peak, duration / 2f)
                     .OnComplete(() => ve.DOScale(1f, duration / 2f));
        }

        public static Tween DOCountUp(this Label label, int from, int to, float duration)
        {
            return DOVirtual.Int(from, to, duration, v =>
            {
                if (label == null) return;
                label.text = v.ToString();
            });
        }
    }
}

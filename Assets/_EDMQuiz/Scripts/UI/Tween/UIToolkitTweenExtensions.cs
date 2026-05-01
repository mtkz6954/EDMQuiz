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
            if (ve == null) return null;
            float from = ve.resolvedStyle.scale.value.x;
            if (Mathf.Approximately(from, 0f)) from = 1f;
            float half = duration / 2f;
            return DOTween.Sequence()
                .Append(DOVirtual.Float(from, peak, half, v =>
                {
                    if (ve == null) return;
                    ve.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
                }))
                .Append(DOVirtual.Float(peak, from, half, v =>
                {
                    if (ve == null) return;
                    ve.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1)));
                }));
        }

        /// <summary>style.translate.y を peak まで上げて 0 へ戻すバウンス。</summary>
        public static Tween DOBounceY(this VisualElement ve, float peak, float duration)
        {
            if (ve == null) return null;
            float half = duration / 2f;
            return DOTween.Sequence()
                .Append(DOVirtual.Float(0f, peak, half, v =>
                {
                    if (ve == null) return;
                    ve.style.translate = new StyleTranslate(new Translate(0f, v, 0f));
                }).SetEase(Ease.OutQuad))
                .Append(DOVirtual.Float(peak, 0f, half, v =>
                {
                    if (ve == null) return;
                    ve.style.translate = new StyleTranslate(new Translate(0f, v, 0f));
                }).SetEase(Ease.InQuad));
        }

        /// <summary>style.rotate を peakDeg まで振って 0 へ戻すスイング。USS の baseline rotate は inline で上書きされる。</summary>
        public static Tween DOSwingRotate(this VisualElement ve, float peakDeg, float duration)
        {
            if (ve == null) return null;
            float half = duration / 2f;
            return DOTween.Sequence()
                .Append(DOVirtual.Float(0f, peakDeg, half, v =>
                {
                    if (ve == null) return;
                    ve.style.rotate = new StyleRotate(new Rotate(new Angle(v, AngleUnit.Degree)));
                }).SetEase(Ease.OutQuad))
                .Append(DOVirtual.Float(peakDeg, 0f, half, v =>
                {
                    if (ve == null) return;
                    ve.style.rotate = new StyleRotate(new Rotate(new Angle(v, AngleUnit.Degree)));
                }).SetEase(Ease.InQuad));
        }

        /// <summary>style.translate.y を to へ向けて 1 回だけ動かす（戻さない）。コケアニメ用。</summary>
        public static Tween DOTranslateYTo(this VisualElement ve, float to, float duration)
        {
            if (ve == null) return null;
            float from = ve.resolvedStyle.translate.y;
            return DOVirtual.Float(from, to, duration, v =>
            {
                if (ve == null) return;
                ve.style.translate = new StyleTranslate(new Translate(0f, v, 0f));
            });
        }

        /// <summary>style.rotate を toDeg へ向けて 1 回だけ動かす（戻さない）。コケアニメ用。</summary>
        public static Tween DORotateTo(this VisualElement ve, float toDeg, float duration)
        {
            if (ve == null) return null;
            // USS / 自前 Tween は全て Degree 単位で書き込むため .value をそのまま度として扱う
            float from = ve.resolvedStyle.rotate.angle.value;
            return DOVirtual.Float(from, toDeg, duration, v =>
            {
                if (ve == null) return;
                ve.style.rotate = new StyleRotate(new Rotate(new Angle(v, AngleUnit.Degree)));
            });
        }

        /// <summary>style.scale.x を反転（1 ⇄ -1）。途中で 0 を経由するので「振り向き」風になる。</summary>
        public static Tween DOFlipX(this VisualElement ve, float duration)
        {
            if (ve == null) return null;
            float from = ve.resolvedStyle.scale.value.x;
            if (Mathf.Approximately(from, 0f)) from = 1f;
            float to = from > 0f ? -1f : 1f;
            return DOVirtual.Float(from, to, duration, v =>
            {
                if (ve == null) return;
                ve.style.scale = new StyleScale(new Scale(new Vector3(v, 1f, 1f)));
            });
        }

        /// <summary>VisualElement を style.translate で一度だけ揺らして 0 へ戻す（damp あり）。</summary>
        public static Tween DOShakeOnce(this VisualElement ve, float duration, float strength, int vibrato)
        {
            if (ve == null) return null;
            int steps = Mathf.Max(2, vibrato);
            var offsets = new Vector2[steps + 1];
            for (int i = 0; i < steps; i++)
                offsets[i] = Random.insideUnitCircle * strength;
            offsets[steps] = Vector2.zero;

            return DOVirtual.Float(0f, 1f, duration, t =>
            {
                if (ve == null) return;
                float damp = 1f - t;
                float pos = t * steps;
                int idx = Mathf.Min(steps - 1, (int)pos);
                float frac = pos - idx;
                Vector2 v = Vector2.Lerp(offsets[idx], offsets[idx + 1], frac) * damp;
                ve.style.translate = new StyleTranslate(new Translate(v.x, v.y, 0f));
            }).OnComplete(() =>
            {
                if (ve == null) return;
                ve.style.translate = new StyleTranslate(StyleKeyword.Null);
            });
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

using UnityEngine;

namespace EDMQuiz
{
    public static class MovingSpotlightMotion
    {
        public static Vector3 GetFigureEightPosition(
            float elapsedSeconds,
            int lightIndex,
            int lightCount,
            float beatDuration,
            int beatsPerBar,
            float barsPerLap,
            float scale)
        {
            if (lightCount <= 0 || beatDuration <= 0f || beatsPerBar <= 0 || barsPerLap <= 0f)
                return Vector3.zero;

            float lapDuration = beatDuration * beatsPerBar * barsPerLap;
            float omega = (Mathf.PI * 2f) / lapDuration;
            float phase = lightIndex * (Mathf.PI * 2f / lightCount);
            float angle = elapsedSeconds * omega + phase;
            float x = Mathf.Sin(angle) * scale;
            float y = Mathf.Sin(angle * 2f) * scale * 0.5f;
            return new Vector3(x, y, 0f);
        }

        public static int GetPaletteIndex(int beatIndex, int lightIndex, int paletteLength)
        {
            if (paletteLength <= 0) return 0;
            int index = (beatIndex + lightIndex) % paletteLength;
            return index < 0 ? index + paletteLength : index;
        }

        /// <summary>正解時の激しい軌道。
        /// 高速化した8の字を時間で回転させ、さらに全灯共通の左右ヨーヨーオフセットを加算する。</summary>
        public static Vector3 GetHypePosition(
            float elapsedSeconds,
            int lightIndex,
            int lightCount,
            float beatDuration,
            int beatsPerBar,
            float hypeBarsPerLap,
            float hypeScale,
            float rotateHz,
            float yoyoAmplitude,
            float yoyoHz)
        {
            Vector3 fig8 = GetFigureEightPosition(
                elapsedSeconds, lightIndex, lightCount,
                beatDuration, beatsPerBar, hypeBarsPerLap, hypeScale);

            // 軌道全体を回転させる (各灯が円弧を描きながら旋回)
            float rotRad = elapsedSeconds * rotateHz * Mathf.PI * 2f;
            float c = Mathf.Cos(rotRad);
            float s = Mathf.Sin(rotRad);
            Vector3 rotated = new Vector3(
                fig8.x * c - fig8.y * s,
                fig8.x * s + fig8.y * c,
                0f);

            // 左右ヨーヨー: 全灯共通の X オフセットを sin で振る
            float yoyoX = Mathf.Sin(elapsedSeconds * yoyoHz * Mathf.PI * 2f) * yoyoAmplitude;
            return new Vector3(rotated.x + yoyoX, rotated.y, 0f);
        }

        /// <summary>正解時のサイン波光量。min と max の間を pulseHz [回/秒] で振動。</summary>
        public static float GetHypeIntensity(float elapsedSeconds, float min, float max, float pulseHz)
        {
            // sin の出力 [-1, 1] を [0, 1] に正規化してから min..max へ補間
            float n = (Mathf.Sin(elapsedSeconds * pulseHz * Mathf.PI * 2f) + 1f) * 0.5f;
            return Mathf.Lerp(min, max, n);
        }
    }
}

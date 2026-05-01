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
    }
}

using NUnit.Framework;
using UnityEngine;

namespace EDMQuiz.Tests
{
    public class MovingSpotlightMotionTests
    {
        [Test]
        public void GetFigureEightPosition_OffsetsEachLightAroundSharedPath()
        {
            var first = MovingSpotlightMotion.GetFigureEightPosition(
                elapsedSeconds: 0f,
                lightIndex: 0,
                lightCount: 4,
                beatDuration: 0.5f,
                beatsPerBar: 4,
                barsPerLap: 2f,
                scale: 6f);

            var second = MovingSpotlightMotion.GetFigureEightPosition(
                elapsedSeconds: 0f,
                lightIndex: 1,
                lightCount: 4,
                beatDuration: 0.5f,
                beatsPerBar: 4,
                barsPerLap: 2f,
                scale: 6f);

            Assert.That(first, Is.EqualTo(Vector3.zero));
            Assert.That(second.x, Is.EqualTo(6f).Within(0.001f));
            Assert.That(second.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetPaletteIndex_RotatesColorsByBeatAndLightIndex()
        {
            Assert.That(MovingSpotlightMotion.GetPaletteIndex(beatIndex: 0, lightIndex: 0, paletteLength: 4), Is.EqualTo(0));
            Assert.That(MovingSpotlightMotion.GetPaletteIndex(beatIndex: 0, lightIndex: 3, paletteLength: 4), Is.EqualTo(3));
            Assert.That(MovingSpotlightMotion.GetPaletteIndex(beatIndex: 2, lightIndex: 3, paletteLength: 4), Is.EqualTo(1));
        }
    }
}

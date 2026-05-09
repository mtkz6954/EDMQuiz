using System.IO;
using NUnit.Framework;

namespace EDMQuiz.Tests
{
    public class GamePresentationPolishTests
    {
        private const string GamePanelLayoutPath = "Assets/_EDMQuiz/UI/Layouts/game-panel.uxml";
        private const string GamePanelDisplayALayoutPath = "Assets/_EDMQuiz/UI/Layouts/game-panel-display-a.uxml";
        private const string GamePanelDisplayBLayoutPath = "Assets/_EDMQuiz/UI/Layouts/game-panel-display-b.uxml";
        private const string GamePanelDisplayAStylePath = "Assets/_EDMQuiz/UI/Styles/game-panel-display-a.uss";
        private const string GamePanelDisplayBStylePath = "Assets/_EDMQuiz/UI/Styles/game-panel-display-b.uss";
        private const string GameplayMirrorScriptPath = "Assets/_EDMQuiz/Scripts/UI/GameplayRenderTextureMirror.cs";

        [Test]
        public void GamePanelLayout_DoesNotRenderBackgroundLaserLines()
        {
            string layout = File.ReadAllText(GamePanelLayoutPath);

            Assert.That(layout, Does.Not.Contain("bg-lasers"));
            Assert.That(layout, Does.Not.Contain("laser-line"));
        }

        [Test]
        public void AnswerWindow_AllowsEightBeatsForMouseInput()
        {
            Assert.That(GameConstants.ANSWER_WINDOW_BEATS, Is.EqualTo(8));
        }

        [Test]
        public void GameplayMirrorAssets_ExistForDisplayAAndDisplayB()
        {
            Assert.That(File.Exists(GamePanelDisplayALayoutPath), Is.True, "display A layout is missing");
            Assert.That(File.Exists(GamePanelDisplayBLayoutPath), Is.True, "display B layout is missing");
            Assert.That(File.Exists(GamePanelDisplayAStylePath), Is.True, "display A style is missing");
            Assert.That(File.Exists(GamePanelDisplayBStylePath), Is.True, "display B style is missing");
            Assert.That(File.Exists(GameplayMirrorScriptPath), Is.True, "RenderTexture mirror script is missing");
        }

        [Test]
        public void OverlayGamePanel_KeepsResultRootButNotDisplayPanels()
        {
            string layout = File.ReadAllText(GamePanelLayoutPath);

            Assert.That(layout, Does.Contain("result-root"));
            Assert.That(layout, Does.Not.Contain("display-a-root"));
            Assert.That(layout, Does.Not.Contain("display-b-root"));
        }
    }
}

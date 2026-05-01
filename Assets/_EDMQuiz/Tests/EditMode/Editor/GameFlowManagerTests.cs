using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

namespace EDMQuiz.Tests
{
    public class GameFlowManagerTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator StartGame_WaitsForQuestionIntroBeforeBuildUp()
        {
            ClearSingletons();

            var audioObject = new GameObject("AudioManager");
            var flowObject = new GameObject("GameFlowManager");
            var questionIntroClip = AudioClip.Create("QuestionIntro", 44100, 1, 44100, false);
            var quizDatabase = CreateQuizDatabase();

            audioObject.SetActive(false);
            flowObject.SetActive(false);

            try
            {
                var audioManager = audioObject.AddComponent<AudioManager>();
                SetPrivateField(audioManager, "_questionIntroClip", questionIntroClip);

                var gameFlowManager = flowObject.AddComponent<GameFlowManager>();
                SetPrivateField(gameFlowManager, "_quizDatabase", quizDatabase);

                SetSingleton(typeof(AudioManager), audioManager);
                SetSingleton(typeof(GameFlowManager), gameFlowManager);
                gameFlowManager.StartGame();

                yield return null;
                Assert.That(gameFlowManager.CurrentPhase, Is.EqualTo(GamePhase.Question));

                yield return new WaitForSecondsRealtime(questionIntroClip.length * 0.5f);
                Assert.That(gameFlowManager.CurrentPhase, Is.EqualTo(GamePhase.Question));

                yield return new WaitForSecondsRealtime(questionIntroClip.length * 0.75f);
                Assert.That(gameFlowManager.CurrentPhase, Is.EqualTo(GamePhase.BuildUp));
            }
            finally
            {
                Object.DestroyImmediate(questionIntroClip);
                Object.DestroyImmediate(quizDatabase);
                Object.DestroyImmediate(audioObject);
                Object.DestroyImmediate(flowObject);
                ClearSingletons();
            }
        }

        [UnityTest]
        public IEnumerator StartGame_RevealsQuestionNearQuestionIntroEnd()
        {
            ClearSingletons();

            var audioObject = new GameObject("AudioManager");
            var flowObject = new GameObject("GameFlowManager");
            var questionIntroClip = AudioClip.Create("QuestionIntro", 66150, 1, 44100, false);
            var quizDatabase = CreateQuizDatabase();
            var revealEvents = new List<GamePhase>();

            audioObject.SetActive(false);
            flowObject.SetActive(false);

            try
            {
                var audioManager = audioObject.AddComponent<AudioManager>();
                SetPrivateField(audioManager, "_questionIntroClip", questionIntroClip);

                var gameFlowManager = flowObject.AddComponent<GameFlowManager>();
                SetPrivateField(gameFlowManager, "_quizDatabase", quizDatabase);

                SetSingleton(typeof(AudioManager), audioManager);
                SetSingleton(typeof(GameFlowManager), gameFlowManager);

                var revealObservable = GetQuestionRevealObservable();
                using var subscription = revealObservable.Subscribe(_ => revealEvents.Add(gameFlowManager.CurrentPhase));

                gameFlowManager.StartGame();

                yield return null;
                Assert.That(revealEvents, Is.Empty);

                float beforeRevealDelay = questionIntroClip.length
                                        - GameConstants.QUESTION_FADE_IN_DURATION
                                        - 0.1f;
                yield return new WaitForSecondsRealtime(beforeRevealDelay);
                Assert.That(revealEvents, Is.Empty);

                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(revealEvents, Has.Count.EqualTo(1));
                Assert.That(revealEvents[0], Is.EqualTo(GamePhase.Question));
                Assert.That(gameFlowManager.CurrentPhase, Is.EqualTo(GamePhase.Question));
            }
            finally
            {
                Object.DestroyImmediate(questionIntroClip);
                Object.DestroyImmediate(quizDatabase);
                Object.DestroyImmediate(audioObject);
                Object.DestroyImmediate(flowObject);
                ClearSingletons();
            }
        }

        private static QuizDatabase CreateQuizDatabase()
        {
            var database = ScriptableObject.CreateInstance<QuizDatabase>();
            database.questions = new QuizQuestion[GameConstants.TOTAL_QUESTIONS];
            for (int i = 0; i < database.questions.Length; i++)
            {
                var question = ScriptableObject.CreateInstance<QuizQuestion>();
                question.questionText = $"Question {i + 1}";
                question.correctAnswer = "abcd";
                question.hiraganaOptions = new[] { "a", "b", "c", "d", "e" };
                database.questions[i] = question;
            }
            return database;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void SetSingleton(System.Type type, Object value)
        {
            type.GetField("<Instance>k__BackingField", PrivateStatic)
                ?.SetValue(null, value);
        }

        private static Observable<Unit> GetQuestionRevealObservable()
        {
            var property = typeof(GameFlowManager).GetProperty(
                "OnQuestionReveal",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(property, Is.Not.Null);
            return (Observable<Unit>)property.GetValue(null);
        }

        private static void ClearSingletons()
        {
            typeof(AudioManager)
                .GetField("<Instance>k__BackingField", PrivateStatic)
                ?.SetValue(null, null);

            typeof(GameFlowManager)
                .GetField("<Instance>k__BackingField", PrivateStatic)
                ?.SetValue(null, null);
        }
    }
}

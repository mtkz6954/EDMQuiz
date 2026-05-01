using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace EDMQuiz.Tests
{
    public class AudioManagerTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ApplySceneSettingsFrom_CopiesFallbackClipFromSceneDuplicate()
        {
            var targetObject = new GameObject("TargetAudioManager");
            var sourceObject = new GameObject("SourceAudioManager");
            var fallbackClip = AudioClip.Create("Fallback", 8, 1, 44100, false);

            targetObject.SetActive(false);
            sourceObject.SetActive(false);

            try
            {
                var target = targetObject.AddComponent<AudioManager>();
                var source = sourceObject.AddComponent<AudioManager>();
                SetFallbackClip(source, fallbackClip);

                InvokeApplySceneSettingsFrom(target, source);

                Assert.That(GetFallbackClip(target), Is.SameAs(fallbackClip));
            }
            finally
            {
                Object.DestroyImmediate(fallbackClip);
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ApplySceneSettingsFrom_CopiesQuestionIntroClipFromSceneDuplicate()
        {
            var targetObject = new GameObject("TargetAudioManager");
            var sourceObject = new GameObject("SourceAudioManager");
            var questionIntroClip = AudioClip.Create("QuestionIntro", 4410, 1, 44100, false);

            targetObject.SetActive(false);
            sourceObject.SetActive(false);

            try
            {
                var target = targetObject.AddComponent<AudioManager>();
                var source = sourceObject.AddComponent<AudioManager>();
                SetQuestionIntroClip(source, questionIntroClip);

                InvokeApplySceneSettingsFrom(target, source);

                Assert.That(GetQuestionIntroClip(target), Is.SameAs(questionIntroClip));
            }
            finally
            {
                Object.DestroyImmediate(questionIntroClip);
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void PlayQuestionIntroSE_ReturnsQuestionIntroClipLength()
        {
            var audioObject = new GameObject("AudioManager");
            var questionIntroClip = AudioClip.Create("QuestionIntro", 4410, 1, 44100, false);
            audioObject.SetActive(false);

            try
            {
                var audioManager = audioObject.AddComponent<AudioManager>();
                SetQuestionIntroClip(audioManager, questionIntroClip);

                float duration = InvokePlayQuestionIntroSE(audioManager);

                Assert.That(duration, Is.EqualTo(questionIntroClip.length).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(questionIntroClip);
                Object.DestroyImmediate(audioObject);
            }
        }

        private static void InvokeApplySceneSettingsFrom(AudioManager target, AudioManager source)
        {
            var method = typeof(AudioManager).GetMethod("ApplySceneSettingsFrom", PrivateInstance);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, new object[] { source });
        }

        private static AudioClip GetFallbackClip(AudioManager manager)
        {
            return (AudioClip)GetFallbackClipField().GetValue(manager);
        }

        private static void SetFallbackClip(AudioManager manager, AudioClip clip)
        {
            GetFallbackClipField().SetValue(manager, clip);
        }

        private static float InvokePlayQuestionIntroSE(AudioManager target)
        {
            var method = typeof(AudioManager).GetMethod("PlayQuestionIntroSE");
            Assert.That(method, Is.Not.Null);
            return (float)method.Invoke(target, null);
        }

        private static AudioClip GetQuestionIntroClip(AudioManager manager)
        {
            return (AudioClip)GetQuestionIntroClipField().GetValue(manager);
        }

        private static void SetQuestionIntroClip(AudioManager manager, AudioClip clip)
        {
            GetQuestionIntroClipField().SetValue(manager, clip);
        }

        private static FieldInfo GetFallbackClipField()
        {
            var field = typeof(AudioManager).GetField("_bgmFallbackClip", PrivateInstance);
            Assert.That(field, Is.Not.Null);
            return field;
        }

        private static FieldInfo GetQuestionIntroClipField()
        {
            var field = typeof(AudioManager).GetField("_questionIntroClip", PrivateInstance);
            Assert.That(field, Is.Not.Null);
            return field;
        }
    }
}

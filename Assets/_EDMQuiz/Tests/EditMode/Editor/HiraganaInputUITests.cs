using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDMQuiz.Tests
{
    public class HiraganaInputUITests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        [Test]
        public void SetQuestionVisible_TogglesQuestionPanelVisibility()
        {
            var uiObject = new GameObject("HiraganaInputUI");
            var questionPanel = new VisualElement();

            try
            {
                var ui = uiObject.AddComponent<HiraganaInputUI>();
                SetPrivateField(ui, "_questionPanel", questionPanel);

                InvokeSetQuestionVisible(ui, true);
                Assert.That(questionPanel.style.visibility.value, Is.EqualTo(Visibility.Visible));

                InvokeSetQuestionVisible(ui, false);
                Assert.That(questionPanel.style.visibility.value, Is.EqualTo(Visibility.Hidden));
            }
            finally
            {
                Object.DestroyImmediate(uiObject);
            }
        }

        [Test]
        public void ResetQuestionPanelToHidden_HidesPanelAboveFinalPosition()
        {
            var uiObject = new GameObject("HiraganaInputUI");
            var questionPanel = new VisualElement();

            try
            {
                var ui = uiObject.AddComponent<HiraganaInputUI>();
                SetPrivateField(ui, "_questionPanel", questionPanel);

                Invoke(ui, "ResetQuestionPanelToHidden");

                Assert.That(questionPanel.style.visibility.value, Is.EqualTo(Visibility.Hidden));
                Assert.That(questionPanel.style.opacity.value, Is.EqualTo(0f));
                Assert.That(questionPanel.style.translate.value.y.value, Is.EqualTo(GameConstants.QUESTION_FADE_IN_OFFSET_PX));
            }
            finally
            {
                Object.DestroyImmediate(uiObject);
            }
        }

        [Test]
        public void ResolveHiraganaIndex_UsesTenKeyDisplayOrder()
        {
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad7), Is.EqualTo(0));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad8), Is.EqualTo(1));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad9), Is.EqualTo(2));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad4), Is.EqualTo(3));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad5), Is.EqualTo(4));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad6), Is.EqualTo(5));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad1), Is.EqualTo(6));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad2), Is.EqualTo(7));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad3), Is.EqualTo(8));
            Assert.That(InvokeResolveHiraganaIndex(KeyCode.Keypad0), Is.EqualTo(9));
        }

        [Test]
        public void GetKeypadNumberForOptionIndex_ReturnsTenKeyBadgeNumber()
        {
            Assert.That(InvokeGetKeypadNumberForOptionIndex(0), Is.EqualTo(7));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(1), Is.EqualTo(8));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(2), Is.EqualTo(9));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(3), Is.EqualTo(4));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(4), Is.EqualTo(5));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(5), Is.EqualTo(6));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(6), Is.EqualTo(1));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(7), Is.EqualTo(2));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(8), Is.EqualTo(3));
            Assert.That(InvokeGetKeypadNumberForOptionIndex(9), Is.EqualTo(0));
        }

        private static void InvokeSetQuestionVisible(HiraganaInputUI ui, bool visible)
        {
            var method = typeof(HiraganaInputUI).GetMethod("SetQuestionVisible", PrivateInstance);
            Assert.That(method, Is.Not.Null);
            method.Invoke(ui, new object[] { visible });
        }

        private static int InvokeResolveHiraganaIndex(KeyCode keyCode)
        {
            var method = typeof(HiraganaInputUI).GetMethod("ResolveHiraganaIndex", PrivateStatic);
            Assert.That(method, Is.Not.Null);
            return (int)method.Invoke(null, new object[] { keyCode });
        }

        private static int InvokeGetKeypadNumberForOptionIndex(int optionIndex)
        {
            var method = typeof(HiraganaInputUI).GetMethod("GetKeypadNumberForOptionIndex", PrivateStatic);
            Assert.That(method, Is.Not.Null);
            return (int)method.Invoke(null, new object[] { optionIndex });
        }

        private static void Invoke(HiraganaInputUI ui, string methodName)
        {
            var method = typeof(HiraganaInputUI).GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null);
            method.Invoke(ui, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}

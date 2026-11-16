using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkillFramework.Editor
{
    [CustomPropertyDrawer(typeof(PlayAnimationSkillNodeConfig))]
    public sealed class PlayAnimationSkillNodeConfigPropertyDrawer : SkillNodeConfigPropertyDrawer
    {
        private const int PreviewFrameRate = 30;
        private const float AutoFitButtonWidth = 72f;

        protected override bool IsExtraProperty(SerializedProperty property)
        {
            return property.name == "AnimationName";
        }

        protected override float GetExtraHeight(SerializedProperty property)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (TryGetAnimationNames(out List<string> animationNames) && animationNames.Count > 0)
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        protected override void DrawExtraFields(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty animationNameProperty = property.FindPropertyRelative("AnimationName");
            Rect rowRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(rowRect, animationNameProperty, true);

            if (!TryGetAnimationNames(out List<string> animationNames) || animationNames.Count == 0)
            {
                return;
            }

            rowRect.y += rowRect.height + EditorGUIUtility.standardVerticalSpacing;
            int selectedIndex = Mathf.Max(0, animationNames.IndexOf(animationNameProperty.stringValue));
            Rect popupRect = rowRect;
            popupRect.width = Mathf.Max(
                0f,
                rowRect.width - AutoFitButtonWidth - EditorGUIUtility.standardVerticalSpacing);
            Rect autoFitButtonRect = new Rect(
                popupRect.xMax + EditorGUIUtility.standardVerticalSpacing,
                rowRect.y,
                AutoFitButtonWidth,
                rowRect.height);

            int newIndex = EditorGUI.Popup(popupRect, "Animation", selectedIndex, animationNames.ToArray());
            if (newIndex != selectedIndex)
            {
                animationNameProperty.stringValue = animationNames[newIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            TryGetAnimationClip(animationNameProperty.stringValue, out AnimationClip animationClip);
            using (new EditorGUI.DisabledScope(animationClip == null))
            {
                if (GUI.Button(autoFitButtonRect, "自动适配"))
                {
                    SerializedProperty durationFramesProperty =
                        property.FindPropertyRelative("DurationFrames");
                    if (durationFramesProperty != null)
                    {
                        durationFramesProperty.intValue = Mathf.Max(
                            1,
                            Mathf.CeilToInt(animationClip.length * PreviewFrameRate));
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        private static bool TryGetAnimationNames(out List<string> animationNames)
        {
            animationNames = new List<string>();
            GameObject previewPrefab = SkillEditorWindow.CurrentPreviewPrefab;
            if (previewPrefab == null)
            {
                return false;
            }

            Animator animator = previewPrefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            if (!TryGetAnimatorController(animator, out AnimatorController animatorController))
            {
                return false;
            }

            foreach (AnimatorControllerLayer layer in animatorController.layers)
            {
                CollectStateNames(layer.stateMachine, animationNames);
            }

            return animationNames.Count > 0;
        }

        private static bool TryGetAnimationClip(
            string animationName,
            out AnimationClip animationClip)
        {
            animationClip = null;
            GameObject previewPrefab = SkillEditorWindow.CurrentPreviewPrefab;
            if (previewPrefab == null || string.IsNullOrEmpty(animationName))
            {
                return false;
            }

            Animator animator = previewPrefab.GetComponentInChildren<Animator>(true);
            if (animator == null ||
                animator.runtimeAnimatorController == null ||
                !TryGetAnimatorController(animator, out AnimatorController animatorController))
            {
                return false;
            }

            foreach (AnimatorControllerLayer layer in animatorController.layers)
            {
                if (TryGetAnimationClip(layer.stateMachine, animationName, ref animationClip))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetAnimationClip(
            AnimatorStateMachine stateMachine,
            string animationName,
            ref AnimationClip animationClip)
        {
            if (stateMachine == null)
            {
                return false;
            }

            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state == null ||
                    !string.Equals(childState.state.name, animationName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                animationClip = GetAnimationClip(childState.state.motion);
                if (animationClip != null)
                {
                    return true;
                }
            }

            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                if (TryGetAnimationClip(childStateMachine.stateMachine, animationName, ref animationClip))
                {
                    return true;
                }
            }

            return false;
        }

        private static AnimationClip GetAnimationClip(Motion motion)
        {
            return motion switch
            {
                AnimationClip clip => clip,
                BlendTree blendTree => GetFirstAnimationClip(blendTree),
                _ => null
            };
        }

        private static AnimationClip GetFirstAnimationClip(BlendTree blendTree)
        {
            if (blendTree == null)
            {
                return null;
            }

            foreach (ChildMotion childMotion in blendTree.children)
            {
                AnimationClip clip = GetAnimationClip(childMotion.motion);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static bool TryGetAnimatorController(
            Animator animator,
            out AnimatorController animatorController)
        {
            animatorController = animator.runtimeAnimatorController as AnimatorController;
            if (animatorController != null)
            {
                return true;
            }

            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                animatorController = overrideController.runtimeAnimatorController as AnimatorController;
            }

            return animatorController != null;
        }

        private static void CollectStateNames(
            AnimatorStateMachine stateMachine,
            List<string> animationNames)
        {
            if (stateMachine == null)
            {
                return;
            }

            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state != null &&
                    !string.IsNullOrEmpty(childState.state.name) &&
                    !animationNames.Contains(childState.state.name))
                {
                    animationNames.Add(childState.state.name);
                }
            }

            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                CollectStateNames(childStateMachine.stateMachine, animationNames);
            }
        }
    }
}

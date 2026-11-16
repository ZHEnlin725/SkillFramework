using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework.Editor
{
    public sealed class SkillEditorPreviewAdapter :
        ISkillOwner,
        ISkillAnimationPlayer,
        ISkillAudioPlayer,
        ISkillEffectPlayer,
        ISkillMovementController,
        IDisposable
    {
        private readonly GameObject root;
        private readonly Action<GameObject> addGameObjectToPreviewScene;
        private readonly Func<string, GameObject> effectPrefabResolver;
        private readonly Func<int, AudioClip> audioClipResolver;
        private readonly List<GameObject> effectInstances = new();
        private readonly Vector3 initialPosition;
        private readonly Quaternion initialRotation;
        private bool rootMotionDisabled;

        public SkillEditorPreviewAdapter(
            GameObject root,
            Action<GameObject> addGameObjectToPreviewScene,
            Func<string, GameObject> effectPrefabResolver,
            Func<int, AudioClip> audioClipResolver = null)
        {
            this.root = root;
            this.addGameObjectToPreviewScene = addGameObjectToPreviewScene;
            this.effectPrefabResolver = effectPrefabResolver;
            this.audioClipResolver = audioClipResolver;

            if (root != null)
            {
                initialPosition = root.transform.position;
                initialRotation = root.transform.rotation;
            }
        }

        public Vector3 Position => root != null ? root.transform.position : Vector3.zero;

        public Vector3 Forward => root != null ? root.transform.forward : Vector3.forward;

        public Quaternion Rotation => root != null ? root.transform.rotation : Quaternion.identity;

        public void Play(SkillContext context, string animationName, bool loop)
        {
            if (root == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                int stateHash = Animator.StringToHash(animationName);
                if (animator.HasState(0, stateHash))
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    animator.CrossFadeInFixedTime(stateHash, 0.1f, 0, 0f);
                    return;
                }
            }

            Animation animation = root.GetComponentInChildren<Animation>();
            if (animation != null && animation[animationName] != null)
            {
                animation.cullingType = AnimationCullingType.AlwaysAnimate;
                AnimationState animationState = animation[animationName];
                animationState.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                animation.Play(animationName);
            }
        }

        public void Play(SkillContext context, int soundId)
        {
            AudioClip clip = audioClipResolver?.Invoke(soundId);
            if (clip == null || root == null)
            {
                return;
            }

            AudioSource audioSource = root.GetComponentInChildren<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void Play(SkillContext context, string effectPath, Vector3 position, Quaternion rotation)
        {
            if (root == null || string.IsNullOrEmpty(effectPath))
            {
                return;
            }

            GameObject prefab = effectPrefabResolver?.Invoke(effectPath);
            if (prefab == null)
            {
                return;
            }

            GameObject instance = GameObject.Instantiate(prefab, position, rotation);
            instance.hideFlags = HideFlags.HideAndDontSave;
            effectInstances.Add(instance);
            addGameObjectToPreviewScene?.Invoke(instance);
        }

        public void Move(SkillContext context, Vector3 displacement)
        {
            if (root != null)
            {
                root.transform.position += displacement;
            }
        }

        public void Tick(float deltaTime)
        {
            if (root == null)
            {
                return;
            }

            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Update(deltaTime);
            }
        }

        public void SetRootMotionDisabled(bool disabled)
        {
            rootMotionDisabled = disabled;
            ApplyRootMotionSettings();
        }

        public void Reset()
        {
            ClearEffects();

            if (root != null)
            {
                root.transform.position = initialPosition;
                root.transform.rotation = initialRotation;

                Animator animator = root.GetComponentInChildren<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                    ApplyRootMotionSettings();
                }
            }
        }

        public void Dispose()
        {
            ClearEffects();
        }

        private void ClearEffects()
        {
            for (int i = effectInstances.Count - 1; i >= 0; i--)
            {
                if (effectInstances[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(effectInstances[i]);
                }
            }

            effectInstances.Clear();
        }

        private void ApplyRootMotionSettings()
        {
            if (root == null)
            {
                return;
            }

            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            {
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.applyRootMotion = !rootMotionDisabled;
                }
            }
        }
    }
}

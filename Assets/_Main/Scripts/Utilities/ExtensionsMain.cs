using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Main.Scripts.Utilities
{
    /// <summary>
    /// Provides extension methods for common mathematical and utility operations.
    /// Includes value remapping and delayed execution helpers.
    /// </summary>
    public static class ExtensionsMain
    {
        /// <summary>
        /// Remaps a float value from one range to another.
        /// </summary>
        /// <param name="value">The input value to remap.</param>
        /// <param name="inMin">Minimum value of the input range.</param>
        /// <param name="inMax">Maximum value of the input range.</param>
        /// <param name="outMin">Minimum value of the output range.</param>
        /// <param name="outMax">Maximum value of the output range.</param>
        /// <param name="isClamp">If true, clamps the result within the output range.</param>
        /// <returns>The remapped value in the output range.</returns>
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax, bool isClamp = false)
        {
            float newValue = (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
            return isClamp ? Mathf.Clamp(newValue, outMin, outMax) : newValue;
        }
        public static Vector3 Remap(float value, float inMin, float inMax, Vector3 outMin, Vector3 outMax, bool isClamp = false)
        {
            float x = (value - inMin) / (inMax - inMin) * (outMax.x - outMin.x) + outMin.x;
            float y = (value - inMin) / (inMax - inMin) * (outMax.y - outMin.y) + outMin.y;
            float z = (value - inMin) / (inMax - inMin) * (outMax.z - outMin.z) + outMin.z;

            if (isClamp)
            {
                x = Mathf.Clamp(x, outMin.x, outMax.x);
                y = Mathf.Clamp(y, outMin.y, outMax.y);
                z = Mathf.Clamp(z, outMin.z, outMax.z);
            }

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Executes a callback after a specified delay using DOTween.
        /// This provides a simple delayed execution without using coroutines.
        /// </summary>
        /// <param name="delayInSeconds">The delay duration in seconds.</param>
        /// <param name="callback">The action to execute after the delay.</param>
        public static void Wait(float delayInSeconds, System.Action callback)
        {
            DOVirtual.DelayedCall(delayInSeconds, () =>
            {
                callback?.Invoke();
            });
        }
        
        /// <summary>
        /// Shuffles the list in place using the Fisher–Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
		public static void SetX(this Transform transform, float x)
        {
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
        public static void SetY(this Transform transform, float y)
        {
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
        public static void SetZ(this Transform transform, float z)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, z);
        }
        public static void SetLocalX(this Transform transform, float x)
        {
            transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
        }
        public static void SetLocalY(this Transform transform, float y)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
        }
        public static void SetLocalZ(this Transform transform, float z)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
        }
        public static void AddX(this Transform transform, float x)
        {
            transform.position += new Vector3(x, 0, 0);
        }
        public static void AddY(this Transform transform, float y)
        {
            transform.position += new Vector3(0, y, 0);
        }
        public static void AddZ(this Transform transform, float z)
        {
            transform.position += new Vector3(0, 0, z);
        }
        
        public static Tween DoBlendShapeWeight(this SkinnedMeshRenderer renderer, int blendShapeIndex, float endValue, float duration)
        {
            float startValue = renderer.GetBlendShapeWeight(blendShapeIndex);

            return DOTween.To(
                () => startValue,
                x =>
                {
                    startValue = x;
                    renderer.SetBlendShapeWeight(blendShapeIndex, x);
                },
                endValue,
                duration
            ).SetTarget(renderer.transform);
        }
        
        public static float ToDurationBySpeed(Vector3 start, Vector3 end, float speed)
        {
            float distance = Vector3.Distance(start, end);
            return distance / speed;
        }
        public static void ScaleUpThenZero(this Transform t, float upScale = 1.2f, float duration = 0.2f, Action onComplete = null)
        {
            t.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScale(upScale, duration * 0.6f));
            seq.Append(t.DOScale(0f, duration * 0.4f));
            seq.OnComplete(() => onComplete?.Invoke());
            seq.SetTarget(t);
        }
        public static void ScaleUpAndBack(this Transform t, Vector3 defaultScale, float upScale = 1.15f, float duration = 0.25f, Action onComplete = null)
        {
            t.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScale(defaultScale * upScale, duration * 0.5f));
            seq.Append(t.DOScale(defaultScale, duration * 0.5f));
            seq.OnComplete(() => onComplete?.Invoke());
        }

#if UNITY_EDITOR
        /// <summary>
        /// Finds the first ScriptableObject of type T in the project.
        /// </summary>
        public static T FindScriptableObjectInProject<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"No ScriptableObject of type {typeof(T).Name} found in the project.");
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif
        
    }
}
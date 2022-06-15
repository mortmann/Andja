using Andja.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.Utility {

    public static class ExtensionMethods {

        public static T ToEnum<T>(this string value, bool ignoreCase = true) {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        public static Vector2 FloorToInt(this Vector2 v) {
            return new Vector2(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        }

        public static Vector2 CeilToInt(this Vector2 v) {
            return new Vector2(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
        }

        public static Vector2 RoundToInt(this Vector2 v) {
            return new Vector2(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }

        public static Vector2 Rotate(this Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }
        
        public static bool IsFlooredVector(this Vector2 v, Vector2 other) {
            return FloorToInt(v) == FloorToInt(other);
        }

        public static bool IsInBounds(this Vector2 v, int x, int y, int width, int height) {
            return v.x >= x && v.y >= y && v.x < width && v.y < height;
        }

        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos) {
                if (pinfo.IsDefined(typeof(ObsoleteAttribute), true))
                    continue;
                if (pinfo.CanWrite) {
                    try {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos) {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static int GetIntValue(this XmlElement xmlElement) {
            return int.Parse(xmlElement.InnerXml);
        }

        public static int GetIntValue(this XmlNode xmlElement) {
            return int.Parse(xmlElement.InnerXml);
        }

        public static bool IsBitSet(this byte b, int pos) {
            return (b & (1 << pos)) != 0;
        }
        //Changing Color is bugged in Unity 2019.1.0f https://issuetracker.unity3d.com/issues/image-color-cannot-be-changed-via-script-when-image-type-is-set-to-simple
        //TODO: update to the 2021 Version when avaible as full release -> later versions have issues with dll's that are in this project
        public static void SetNormalColor(this Button button, Color color) {
            ColorBlock cb = button.colors;
            cb.normalColor = color;
            button.colors = cb;
        }
        public static Item[] CloneArray(this Item[] items) {
            Item[] newItems = new Item[items.Length];
            for (int i = 0; i < items.Length; i++) {
                newItems[i] = items[i].Clone();
            }
            return newItems;
        }
        public static Item[] CloneArrayWithCounts(this Item[] items, int multipleCount = 1) {
            Item[] newItems = new Item[items.Length];
            for (int i = 0; i < items.Length; i++) {
                newItems[i] = items[i].CloneWithCount();
                newItems[i].count *= multipleCount;
            }
            return newItems;
        }
        /// <summary>
        /// Only other Items will be returned with the existing counts in this Array.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Item[] ReplaceKeepCounts(this Item[] items, Item[] other) {
            if (items == null || other == null || other.Length == 0)
                return other;
            Item[] newItems = other.CloneArray();
            foreach (Item o in items) {
                Item i = Array.Find(newItems, n => n.ID == o.ID);
                if (i != null)
                    i.count = o.count;
            }
            return newItems;
        }


        /// <summary>
        /// Start a coroutine that might throw an exception. Call the callback with the exception if it
        /// does or null if it finishes without throwing an exception.
        /// </summary>
        /// <param name="monoBehaviour">MonoBehaviour to start the coroutine on</param>
        /// <param name="enumerator">Iterator function to run as the coroutine</param>
        /// <param name="exception">Callback to call when the coroutine has thrown an exception.
        /// The thrown exception or null is passed as the parameter.</param>
        /// <returns>The started coroutine</returns>
        public static Coroutine StartThrowingCoroutine(this MonoBehaviour monoBehaviour,
                            IEnumerator enumerator, Action<Exception> exception) {
            return monoBehaviour.StartCoroutine(RunThrowingIterator(enumerator, exception));
        }

        /// <summary>
        /// Run an iterator function that might throw an exception. Call the callback with the exception
        /// if it does or null if it finishes without throwing an exception.
        /// </summary>
        /// <param name="enumerator">Iterator function to run</param>
        /// <param name="exception">Callback to call when the iterator has thrown an exception.
        /// The thrown exception or null is passed as the parameter.</param>
        /// <returns>An enumerator that runs the given enumerator</returns>
        public static IEnumerator RunThrowingIterator(IEnumerator enumerator, Action<Exception> exception) {
            while (true) {
                object current;
                try {
                    if (enumerator.MoveNext() == false) {
                        break;
                    }
                    current = enumerator.Current;
                }
                catch (Exception ex) {
                    exception(ex);
                    Debug.LogException(ex);
                    yield break;
                }
                yield return current;
            }
        }
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
            return source.MinBy(selector, null);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer ??= Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator()) {
                if (!sourceIterator.MoveNext()) {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var min = sourceIterator.Current;
                var minKey = selector(min);
                while (sourceIterator.MoveNext()) {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0) {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) {
            return source.MaxBy(selector, null);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer) {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer ??= Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator()) {
                if (!sourceIterator.MoveNext()) {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var max = sourceIterator.Current;
                var maxKey = selector(max);
                while (sourceIterator.MoveNext()) {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, maxKey) > 0) {
                        max = candidate;
                        maxKey = candidateProjected;
                    }
                }
                return max;
            }
        }

        public static int ClampZero(this int i) {
            return Mathf.Clamp(i, 0, int.MaxValue);
        }

    }
}
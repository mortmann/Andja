﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
[JsonObject(MemberSerialization.OptIn)]
public class RotatingList<T> {
    [JsonPropertyAttribute] List<T> list;
    [JsonPropertyAttribute] int currentIndex;

    public RotatingList() {
        list = new List<T>();
    }

    /// <summary>
    /// Just returns the current first
    /// </summary>
    public T Peek => list[currentIndex];
    /// <summary>
    /// Gets currently first and changes this to the next!
    /// </summary>
    public T First { get { GoToNext(); return list[currentIndex]; } }

    public int Count => list.Count;

    public void Add(T item) {
        list.Add(item);
    }
    public void Remove(T item) {
        list.Remove(item);
    }
    public void RemoveAt(int index) {
        list.RemoveAt(index);
    }
    public void Clear() {
        list.Clear();
        currentIndex = 0;
    }
    public void GoToNext() {
        currentIndex++;
        currentIndex %= list.Count;
    }

    public List<T> GetListStartsCurrent() {
        List<T> temp = new List<T>();
        for (int i = currentIndex; i < list.Count; i++) {
            temp.Add(list[i]);
        }
        for (int i = 0; i < currentIndex; i++) {
            temp.Add(list[i]);
        }
        return temp;
    }
}

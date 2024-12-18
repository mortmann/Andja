﻿using System.Collections.Generic;
using System.Linq;

namespace Andja.Utility {

    public class WeightedRandomList<T> where T : IWeighted {

        private class Entry {
            public double accumulatedWeight;
            public T item;
        }

        private readonly List<Entry>  _entries = new List<Entry>();
        private float accumulatedWeight;
        private readonly List<T> _mustRandoms;
        public bool HasNoMustLeft => _mustRandoms.Count == 0;

        public WeightedRandomList(List<T> list) {
            _mustRandoms = new List<T>(list);
            foreach (T w in list) {
                AddEntry(w);
            }
        }

        public void AddEntry(T item) {
            accumulatedWeight += item.GetStartWeight();
            _entries.Add(new Entry { item = item, accumulatedWeight = accumulatedWeight });
        }

        public T GetRandom(ThreadRandom random, List<T> excluded, int maximumSelect) {
            List<T> exluded = _mustRandoms.Except(excluded).ToList();
            if (_mustRandoms != null && exluded.Count > 0) {
                T t = exluded[random.Next(exluded.Count)];
                _mustRandoms.Remove(t);
                return t;
            }
            double r = random.Float() * accumulatedWeight;
            foreach (Entry entry in _entries) {
                if (excluded.Contains(entry.item))
                    continue;
                if (entry.accumulatedWeight >= r) {
                    float difference = entry.item.Select(maximumSelect);
                    accumulatedWeight -= difference;
                    entry.accumulatedWeight -= difference;
                    return entry.item;
                }
            }
            return default;
        }
    }

    public interface IWeighted {

        float GetStartWeight();

        float Select(int maximumSelect);

        float GetCurrentWeight(int maximumSelect);
    }
}
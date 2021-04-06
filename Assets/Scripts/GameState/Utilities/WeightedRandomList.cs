using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class WeightedRandomList<T> where T : IWeighted {
    private class Entry {
        public double accumulatedWeight;
        public T item;
    }

    private List<Entry> entries = new List<Entry>();
    private float accumulatedWeight;
    private System.Random rand = new System.Random();
    List<T> mustRandoms;
    public bool hasNoMustLeft => mustRandoms.Count==0;
    public WeightedRandomList(List<T> list) {
        mustRandoms = new List<T>(list);
        foreach (T w in list) {
            AddEntry(w);
        }
    }

    public void AddEntry(T item) {
        accumulatedWeight += item.GetStartWeight();
        entries.Add(new Entry { item = item, accumulatedWeight = accumulatedWeight });
    }

    public T GetRandom(ThreadRandom random, List<T> excluded, int maximumSelect) {
        List<T> exluded = mustRandoms.Except(excluded).ToList();
        if (mustRandoms!=null&&exluded.Count>0) {
            T t = exluded[random.Next(exluded.Count)];
            mustRandoms.Remove(t);
            return t;
        }
        double r = random.Float() * accumulatedWeight;
        foreach (Entry entry in entries) {
            if (excluded.Contains(entry.item))
                continue;
            if (entry.accumulatedWeight >= r) {
                float difference = entry.item.Select(maximumSelect);
                accumulatedWeight -= difference;
                entry.accumulatedWeight -= difference;
                return entry.item;
            }
        }
        return default(T); 
    }
}

public interface IWeighted {
    float GetStartWeight();
    float Select(int maximumSelect);
    float GetCurrentWeight(int maximumSelect);
} 
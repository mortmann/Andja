using Andja.Model;
using Andja.Model.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Andja.Controller { 
    public class NeedCalculator {
        protected static int NumberOfPopulationLevels => PrototypController.Instance.NumberOfPopulationLevels;
        public static List<NeedPrototypeData>[] CalculateNeedStuff() {
            List<NeedPrototypeData>[] needsPerLevel = new List<NeedPrototypeData>[NumberOfPopulationLevels];
            foreach (var pair in PrototypController.Instance.NeedPrototypeDatas) {
                NeedPrototypeData need = pair.Value;
                if (need.structures != null) {
                    int startPopulationCount = int.MaxValue;
                    int populationLevel = int.MaxValue;
                    foreach (NeedStructure str in need.structures) {
                        startPopulationCount = Mathf.Min(startPopulationCount, str.PopulationCount);
                        populationLevel = Mathf.Min(populationLevel, str.PopulationLevel);
                        str.NeedStructureData.SatisfiesNeeds.Add(new Need(pair.Key));
                    }
                    if (need.startLevel < populationLevel
                        || need.startLevel == populationLevel && need.startPopulationCount < startPopulationCount) {
                        Debug.LogWarning("Need " + need.Name + " is misconfigured to start earlier than supposed. Fixed to unlock time." +
                            "\nCount " + need.startPopulationCount + "->" + startPopulationCount
                            + "\nLevel " + need.startLevel + "->" + populationLevel);
                        need.startPopulationCount = startPopulationCount;
                        need.startLevel = populationLevel;
                    }
                }
                if (need.item != null) {
                    need.item.Data.SatisfiesNeeds ??= new List<Need>();
                    need.item.Data.SatisfiesNeeds.Add(new Need(pair.Key));
                    if (need.item.Type != ItemType.Luxury) {
                        Debug.LogWarning("Item " + need.item.ID + " is not marked as luxury good. Fix it, but change it in file.");
                        need.item.Data.type = ItemType.Luxury;
                    }
                    need.produceForPeople = new Dictionary<Produce, int[]>();
                    if (PrototypController.Instance.ItemIDToProduce.ContainsKey(need.item.ID) == false) {
                        Debug.LogError("itemIDToProduce does not have any production for this need item " + need.item.ID);
                        continue;
                    }
                    int startPopulationCount = int.MaxValue;
                    int populationLevel = int.MaxValue;
                    foreach (Produce produce in PrototypController.Instance.ItemIDToProduce[need.item.ID]) {
                        StructurePrototypeData str = produce.ProducerStructure;
                        startPopulationCount = Mathf.Min(startPopulationCount, str.populationCount);
                        populationLevel = Mathf.Min(populationLevel, str.populationLevel);
                        need.produceForPeople[produce] = new int[NumberOfPopulationLevels];
                        for (int i = 0; i < NumberOfPopulationLevels; i++) {
                            need.produceForPeople[produce][i] = Mathf.FloorToInt(produce.ProducePerMinute / need.UsageAmounts[i]);
                        }
                    }
                    if (need.startLevel < populationLevel
                        || need.startLevel == populationLevel && need.startPopulationCount < startPopulationCount) {
                        Debug.LogWarning("Need " + need.Name + " is misconfigured to start earlier than supposed. Fixed to unlock time." +
                            "\nCount " + need.startPopulationCount + "->" + startPopulationCount
                            + "\nLevel " + need.startLevel + "->" + populationLevel);
                        need.startPopulationCount = startPopulationCount;
                        need.startLevel = populationLevel;
                    }
                }
                needsPerLevel[need.startLevel] ??= new List<NeedPrototypeData>();
                needsPerLevel[need.startLevel].Add(need);
            }
            foreach (Item item in PrototypController.Instance.AllItems.Values.Where(x => x.Type == ItemType.Luxury)) {
                item.Data.TotalUsagePerLevel = new float[NumberOfPopulationLevels];
                for (int i = 0; i < NumberOfPopulationLevels; i++) {
                    if (item.Data.SatisfiesNeeds != null)
                        item.Data.TotalUsagePerLevel[i] = item.Data.SatisfiesNeeds.Sum(x => x.Uses[i]);
                }
            }
            return needsPerLevel;
        }
    }
}
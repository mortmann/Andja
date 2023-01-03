using Andja.Model;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Andja.Controller {
    public class UnlockCalculator {
        public ConcurrentDictionary<int, Unlocks>[] LevelCountToUnlocks;
        public ConcurrentDictionary<string, float[]> BuildItemsNeeded;

        
        public List<int>[] AllUnlockPeoplePerLevel { get; private set; }
        public Dictionary<string, int[]> RecommandedBuildSupplyChains { get; private set; }
        public List<Fertility> OrderUnlockFertilities { get; private set; }

        protected int NumberOfPopulationLevels => PrototypController.Instance.NumberOfPopulationLevels;
        public UnlockCalculator() {
            CalculateUnlocks();
        }

        public void CalculateUnlocks() {
            LevelCountToUnlocks = new ConcurrentDictionary<int, Unlocks>[NumberOfPopulationLevels];
            BuildItemsNeeded = new ConcurrentDictionary<string, float[]>();
            AllUnlockPeoplePerLevel = new List<int>[NumberOfPopulationLevels];
            for (int i = 0; i < NumberOfPopulationLevels; i++) {
                LevelCountToUnlocks[i] = new ConcurrentDictionary<int, Unlocks>();
            }
            var one = Parallel.ForEach(PrototypController.Instance.StructurePrototypes.Values, structure => {
                if (LevelCountToUnlocks[structure.PopulationLevel].ContainsKey(structure.PopulationCount) == false) {
                    LevelCountToUnlocks[structure.PopulationLevel].TryAdd(structure.PopulationCount, new Unlocks(structure.PopulationCount, structure.PopulationLevel));
                }
                LevelCountToUnlocks[structure.PopulationLevel].TryGetValue(structure.PopulationCount, out Unlocks value);
                value.structures.Add(structure);
                if (structure is OutputStructure output) {
                    if (output.Output != null) {
                        foreach (Item item in output.Output) {
                            lock (item) {
                                if (item.Data.UnlockLevel <= structure.PopulationLevel) {
                                    item.Data.UnlockLevel = structure.PopulationLevel;
                                    item.Data.UnlockPopulationCount = Mathf.Max(item.Data.UnlockPopulationCount, structure.PopulationCount);
                                }
                            }
                        }
                    }
                    if (structure is GrowableStructure growable) {
                        if (growable.Fertility != null) {
                            Fertility f = growable.Fertility;
                            lock (f) {
                                if (f.Data.UnlockLevel <= structure.PopulationLevel) {
                                    f.Data.UnlockLevel = structure.PopulationLevel;
                                    f.Data.UnlockPopulationCount = Mathf.Max(f.Data.UnlockPopulationCount, structure.PopulationCount);
                                }
                            }
                        }
                    }
                }
                if (structure.BuildingItems != null) {
                    foreach (Item item in structure.BuildingItems) {
                        float[] array = new float[NumberOfPopulationLevels];
                        array[structure.PopulationLevel] = item.count;
                        BuildItemsNeeded.AddOrUpdate(item.ID, array, (id, oc) => { oc[structure.PopulationLevel] += item.count; return oc; });
                    }
                }
            });
            var two = Parallel.ForEach(PrototypController.Instance.UnitPrototypes.Values, unit => {
                if (LevelCountToUnlocks[unit.PopulationLevel].ContainsKey(unit.PopulationCount) == false) {
                    LevelCountToUnlocks[unit.PopulationLevel].TryAdd(unit.PopulationCount, new Unlocks(unit.PopulationCount, unit.PopulationLevel));
                }
                LevelCountToUnlocks[unit.PopulationLevel].TryGetValue(unit.PopulationCount, out Unlocks value);
                value.units.Add(unit);
                if (unit.BuildingItems != null) {
                    foreach (Item item in unit.BuildingItems) {
                        float[] array = new float[NumberOfPopulationLevels];
                        array[unit.PopulationLevel] = item.count;
                        BuildItemsNeeded.AddOrUpdate(item.ID, array, (id, oc) => { oc[unit.PopulationLevel] += item.count; return oc; });
                    }
                }
            });
            var three = Parallel.ForEach(PrototypController.Instance.GetAllNeeds(), need => {
                if (LevelCountToUnlocks[need.StartLevel].ContainsKey(need.StartPopulationCount) == false) {
                    LevelCountToUnlocks[need.StartLevel].TryAdd(need.StartPopulationCount, new Unlocks(need.StartPopulationCount, need.StartLevel));
                }
                LevelCountToUnlocks[need.StartLevel].TryGetValue(need.StartPopulationCount, out Unlocks value);
                value.needs.Add(need);
            });
            while ((one.IsCompleted && two.IsCompleted && three.IsCompleted) == false) {
            }
            for (int i = 0; i < NumberOfPopulationLevels; i++) {
                AllUnlockPeoplePerLevel[i] = new List<int>();
                foreach (int key in LevelCountToUnlocks[i].Keys) {
                    AllUnlockPeoplePerLevel[i].Add(key);
                }
                AllUnlockPeoplePerLevel[i].Sort();
            }
            foreach (FertilityPrototypeData fertilityPrototype in PrototypController.Instance.FertilityPrototypeDatas.Values) {
                if (fertilityPrototype.ItemsDependentOnThis.Count == 0) {
                    Debug.LogWarning("Fertility " + fertilityPrototype.ID + " is not required by anything! -- Wanted?");
                }
            }
            //TODO: make this make more sense :)
            RecommandedBuildSupplyChains = new Dictionary<string, int[]>();
            foreach (string item in BuildItemsNeeded.Keys) {
                if (PrototypController.Instance.ItemIDToProduce.ContainsKey(item) == false)
                    continue;
                RecommandedBuildSupplyChains[item] = new int[NumberOfPopulationLevels];
                for (int i = 0; i < NumberOfPopulationLevels; i++) {
                    RecommandedBuildSupplyChains[item][i] = Mathf.CeilToInt(BuildItemsNeeded[item][i]
                        / (PrototypController.Instance.ItemIDToProduce[item][0].producePerMinute * 60));
                }
            }
            OrderUnlockFertilities = new List<Fertility>(PrototypController.Instance.IdToFertilities.Values);
            OrderUnlockFertilities.RemoveAll(x => x.Data.ItemsDependentOnThis.Count == 0);
            OrderUnlockFertilities = OrderUnlockFertilities.OrderBy(x => x.Data.UnlockLevel).ThenBy(x => x.Data.UnlockPopulationCount).ToList();
        }
    }
}


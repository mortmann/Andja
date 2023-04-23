using Andja.Model;
using Andja.Model.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Andja.Controller {

    public static class SupplyChainCalculator {

        public static Dictionary<string, List<Produce>> CalculateOptimalProportions() {
            List<StructurePrototypeData> structures = new List<StructurePrototypeData>(PrototypController.Instance.StructurePrototypeDatas.Values);
            List<FarmPrototypeData> farms = new List<FarmPrototypeData>(structures.OfType<FarmPrototypeData>());
            List<MinePrototypeData> mines = new List<MinePrototypeData>(structures.OfType<MinePrototypeData>());
            List<ProductionPrototypeData> productions = new List<ProductionPrototypeData>(structures.OfType<ProductionPrototypeData>());
            List<Produce> productionsProduces = new List<Produce>();
            Dictionary<string, List<Produce>> ItemIdToProduce = new Dictionary<string, List<Produce>>();
            string produceDebug = "Produce Per Minute\n";
            produceDebug += "##############FARMS##############\n";
            foreach (FarmPrototypeData fpd in farms) {
                foreach (Item outItem in fpd.output) {
                    float ppm = 0;
                    int tileCount = fpd.RangeTileCount;
                    int numGrowablesPerTon = fpd.neededHarvestToProduce;
                    float growtime = fpd.growable != null ? fpd.growable.ProduceTime : 1;
                    float produceTime = fpd.produceTime;
                    float neededWorkerRatio = (float)fpd.maxNumberOfWorker / (float)fpd.neededHarvestToProduce;
                    float workPerWorker = (float)fpd.neededHarvestToProduce / (float)fpd.maxNumberOfWorker;
                    if (fpd.growable == null) {
                        ppm = 60f / (produceTime * fpd.efficiency);
                    }
                    else
                    if (produceTime * fpd.efficiency <= 0 || growtime <= 0) {
                        ppm = 0;
                    }
                    else if (fpd.maxNumberOfWorker * produceTime * fpd.efficiency >= growtime) {
                        ppm = neededWorkerRatio * (60f / produceTime);
                    }
                    else {
                        ppm = Mathf.Min(
                                60f / (workPerWorker * (produceTime * fpd.efficiency)),
                                //not sure if this is correct
                                ((float)tileCount / ((float)numGrowablesPerTon * fpd.maxNumberOfWorker)) * (60f / growtime)
                             );
                    }
                    ppm /= (float)outItem.count;
                    if (ppm == 0)
                        Debug.LogError("Farm " + fpd.ID + " does not produce anything per minute. FIX IT!");
                    produceDebug += fpd.ID + ": " + ppm + "\n";
                    fpd.ProducePerMinute = ppm;
                    Produce p = new Produce {
                        Item = outItem,
                        ProducePerMinute = ppm,
                        ProducerStructure = fpd
                    };
                    p.CalculateSupplyChains();
                    if (ItemIdToProduce.ContainsKey(outItem.ID)) {
                        ItemIdToProduce[outItem.ID].Add(p);
                    }
                    else {
                        ItemIdToProduce.Add(outItem.ID, new List<Produce> { p });
                    }
                    if (fpd.growable?.Fertility != null) {
                        PrototypController.Instance.FertilityPrototypeDatas[fpd.growable.Fertility.ID].ItemsDependentOnThis.Add(outItem.ID);
                    }
                }
            }
            produceDebug += "\n##############MINES##############\n";
            foreach (MinePrototypeData mpd in mines) {
                foreach (Item outItem in mpd.output) {
                    float ppm = mpd.produceTime == 0 ? float.MaxValue : outItem.count * (60f / mpd.produceTime);
                    mpd.ProducePerMinute = ppm;
                    Produce p = new Produce {
                        Item = outItem,
                        ProducePerMinute = ppm,
                        ProducerStructure = mpd
                    };
                    p.CalculateSupplyChains();
                    produceDebug += mpd.ID + ": " + ppm + "\n";
                    if (ItemIdToProduce.ContainsKey(outItem.ID)) {
                        ItemIdToProduce[outItem.ID].Add(p);
                    }
                    else {
                        ItemIdToProduce.Add(outItem.ID, new List<Produce> { p });
                    }
                }
            }
            produceDebug += "\n###########PRODUCTION############\n";
            foreach (ProductionPrototypeData ppd in productions) {
                if (ppd.output == null)
                    continue;
                foreach (Item outItem in ppd.output) {
                    float ppm = ppd.produceTime == 0 ? float.MaxValue : outItem.count * (60f / ppd.produceTime);
                    ppd.ProducePerMinute = ppm;
                    Produce p = new Produce {
                        Item = outItem,
                        ProducePerMinute = ppm,
                        ProducerStructure = ppd,
                        Needed = ppd.intake
                    };
                    produceDebug += ppd.ID + ": " + ppm + "\n";
                    productionsProduces.Add(p);
                    if (ItemIdToProduce.ContainsKey(outItem.ID)) {
                        ItemIdToProduce[outItem.ID].Add(p);
                    }
                    else {
                        ItemIdToProduce.Add(outItem.ID, new List<Produce> { p });
                    }
                }
            }
            Debug.Log(produceDebug);
            foreach (Produce currentProduce in productionsProduces) {
                if (currentProduce.Needed == null)
                    continue;
                foreach (Item need in currentProduce.Needed) {
                    if (ItemIdToProduce.ContainsKey(need.ID) == false) {
                        Debug.LogWarning("NEEDED ITEM CANNOT BE PRODUCED! -- Wanted beahviour? Item-ID:" + need.ID);
                        continue;
                    }
                    foreach (Produce itemProducer in ItemIdToProduce[need.ID]) {
                        float f1 = (((float)need.count * (60f / currentProduce.ProducerStructure.produceTime)));
                        float f2 = (((float)itemProducer.Item.count * itemProducer.ProducePerMinute));
                        if (f2 == 0)
                            continue;
                        float ratio = f1 / f2;
                        if (currentProduce.itemProduceRatios.ContainsKey(need.ID) == false) {
                            currentProduce.itemProduceRatios[need.ID] = new List<ProduceRatio>();
                        }
                        currentProduce.itemProduceRatios[need.ID].Add(new ProduceRatio {
                            Producer = itemProducer,
                            Ratio = ratio,
                        });
                    }
                }
            }
            foreach (Produce currentProduce in productionsProduces) {
                currentProduce.CalculateSupplyChains();
            }
            string proportionDebug = "Proportions";
            foreach (Produce currentProduce in productionsProduces) {
                proportionDebug += "\n" + currentProduce.ProducerStructure.ID + ":";
                foreach (string item in currentProduce.itemProduceRatios.Keys) {
                    proportionDebug += "\n ->" + item;
                    proportionDebug = currentProduce.itemProduceRatios[item].Aggregate(proportionDebug, (current, pr)
                                        => current + ("\n  # " + pr.Producer.ProducerStructure.ID + "= " + pr.Ratio));
                }
            }
            Debug.Log(proportionDebug);
            string supplyChains = "SupplyChains";
            foreach (Produce currentProduce in productionsProduces) {
                supplyChains += "\n" + currentProduce.ProducerStructure.ID + "(" + currentProduce.Item.ID + "): ";
                foreach (SupplyChain sc in currentProduce.SupplyChains) {
                    supplyChains += "[";
                    for (int i = 0; i < sc.tiers.Count; i++) {
                        supplyChains += (i + 1) + "| " + string.Join(", ", sc.tiers[i]);
                        if (i < sc.tiers.Count - 1)
                            supplyChains += " ";
                    }
                    supplyChains += "]";
                }
            }
            Debug.Log(supplyChains);
            string supplyChainsCosts = "SupplyChainsCosts";
            foreach (Produce currentProduce in productionsProduces) {
                supplyChainsCosts += "\n" + currentProduce.ProducerStructure.ID + "(" + currentProduce.Item.ID + "): ";
                foreach (SupplyChain sc in currentProduce.SupplyChains) {
                    supplyChainsCosts += "[";
                    supplyChainsCosts += "TBC " + sc.cost.TotalBuildCost;
                    supplyChainsCosts += " TMC " + sc.cost.TotalMaintenance;
                    supplyChainsCosts += " PL " + sc.cost.PopulationLevel;
                    supplyChainsCosts += " I " + string.Join(", ", sc.cost.TotalItemCost.Select(x => x.ToString()));
                    if (sc.cost.requiredFertilites != null)
                        supplyChainsCosts += " F " + string.Join(", ", sc.cost.requiredFertilites.Select(x => x.ToString()));
                    supplyChainsCosts += "]";
                }
            }
            Debug.Log(supplyChainsCosts);
            return ItemIdToProduce;
        }
    }

}
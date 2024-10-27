using Andja.Model;
using System.Collections.Concurrent;

namespace Andja.Controller {
    public class Unlocks {
        public Unlocks(int peopleCount, int level) {
            this.peopleCount = peopleCount;
            this.populationLevel = level;
            this.requiredFullHomes = peopleCount / PrototypController.Instance.PopulationLevelDatas[level].HomeStructure.People;
        }
        public int peopleCount;
        public int populationLevel;
        public int requiredFullHomes;
        public ConcurrentBag<Structure> structures = new ConcurrentBag<Structure>();
        public ConcurrentBag<Unit> units = new ConcurrentBag<Unit>();
        public ConcurrentBag<Need> needs = new ConcurrentBag<Need>();
    }
}
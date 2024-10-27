using Andja.Model;
using System.Linq;

namespace Andja.Model {

    public class AddRangeTilesToCity : Element {

        private Structure Structure;
        private ICity City => Structure.City;

        public AddRangeTilesToCity(Structure structure) : base(structure) {
            Structure = structure;
        }
        public override void OnStart(bool loading = false) {
            City.AddTiles(Structure.RangeTiles.Concat(Structure.Tiles));
        }

        public override void OnDestroy() {
            Structure.Tiles.ForEach(t => t.City = null);
            Structure.RangeTiles.ToList().ForEach(t => t.City = null);
        }

        public override void OnLoad() {
        }


        public override void OnUpdate(float deltaTime) {
        }
    }

}
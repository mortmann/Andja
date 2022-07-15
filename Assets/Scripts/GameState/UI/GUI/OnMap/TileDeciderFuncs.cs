using Andja.Model;

namespace Andja {

    public class TileDeciderFuncs {
        public static Structure Structure;

        public static TileMark StructureTileDecider(Tile t) {
            if (t.City != null && t.City.IsCurrentPlayerCity() &&
                Structure != null && Structure.StructureRange > 0 &&
                (Structure.RangeTiles.Contains(t) || Structure.Tiles.Contains(t))) {
                return TileMark.None;
            }
            else {
                return TileMark.Dark;
            }
        }
    }
}
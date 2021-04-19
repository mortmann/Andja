using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileDeciderFuncs  {
    public static Structure Structure;
    public static TileMark StructureTileDecider(Tile t) {
        if (t.City != null && t.City.IsCurrPlayerCity() &&
            Structure != null && Structure.StructureRange > 0 &&
            (Structure.RangeTiles.Contains(t) || Structure.Tiles.Contains(t))) {
            return TileMark.None;
        }
        else {
            return TileMark.Dark;
        }
    }
}

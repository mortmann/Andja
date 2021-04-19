using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureUI : InfoUI {
    private Structure currentStructure;

    public override void OnShow(object Info) {
        if (currentStructure == Info) {
            return;
        }
        currentStructure = Info as Structure;
        if (currentStructure == null)
            return;
        TileDeciderFuncs.Structure = currentStructure;
        TileSpriteController.Instance.RemoveDecider(TileDeciderFuncs.StructureTileDecider);
        if (currentStructure.StructureRange > 0) {
            TileSpriteController.Instance.AddDecider(TileDeciderFuncs.StructureTileDecider);
        }

    }

    public override void OnClose() {
        TileDeciderFuncs.Structure = null;
        TileSpriteController.Instance.RemoveDecider(TileDeciderFuncs.StructureTileDecider);
    }
}

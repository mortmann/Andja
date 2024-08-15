using System.Collections.Generic;

namespace Andja.Model {
    public interface IGrowableStructure {
        Fertility Fertility { get; }
        bool IsBeingWorked { get; }
        float LandGrowModifier { get; }
        string SortingLayer { get; }

        string GetSpriteName();
        void Harvest();
        void OnBuild(bool loading = false);
        bool SpecialCheckForBuild(List<Tile> tiles);
    }
}
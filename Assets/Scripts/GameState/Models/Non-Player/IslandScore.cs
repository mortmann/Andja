namespace Andja.Model {

    public struct IslandScore {
        public Island Island;

        public float EndScore => SizeScore * 0.5f + SizeSimilarIslandScore * 0.2f +
                                 ResourceScore * 0.1f + FertilityScore * 0.1f +
                                 DistanceScore * 0.3f;

        public float SizeScore;
        public float SizeSimilarIslandScore;
        public float ResourceScore;
        public float FertilityScore;
        public float CompetitionScore;
        public float DistanceScore;
        public float ShapeScore;//Not sure if this is feasible!

        public override string ToString() {
            return "SIZE:" + SizeScore + " SIMSIZE:" + SizeSimilarIslandScore + " RES:" + ResourceScore + " FER:" + FertilityScore
                + " COM:" + CompetitionScore + " DIST:" + DistanceScore + " SHA:" + ShapeScore;
        }
    }
}
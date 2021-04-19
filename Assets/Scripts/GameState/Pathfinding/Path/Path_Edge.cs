//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

namespace Andja.Pathfinding {

    public class Path_Edge<T> {
        public float cost;  // Cost to traverse this edge (i.e. cost to ENTER the tile)

        public Path_Node<T> node;
    }
}
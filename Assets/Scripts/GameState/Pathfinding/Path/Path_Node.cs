//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

namespace Andja.Pathfinding {

    public class Path_Node<T> {
        public T data;

        public Path_Edge<T>[] edges;    // Nodes leading OUT from this node.
    }
}
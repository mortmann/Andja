using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;
using System.Linq;

public class Path_Ocean {
    public Queue<Tile> path;
    bool[,] tiles;

    public Path_Ocean(Vector3 startPos, Vector3 endPos) {
        Calculate(World.Current.GetTileAt(startPos.x, startPos.y),
            World.Current.GetTileAt(endPos.x, endPos.y));
    }
    /// <summary>
    /// Calculate the specified nodes, tileStart, tileEnd and diag.
    /// We are going to go through the Map without any nodes list.
    /// Only a array of booleans
    /// </summary>
    /// <param name="nodes">Nodes.</param>
    /// <param name="tileStart">Tile start.</param>
    /// <param name="tileEnd">Tile end.</param>
    /// <param name="diag">If set to <c>true</c> diag.</param>
    private void Calculate(Tile tileStart, Tile tileEnd, bool diag = true) {
        tiles = null;// World.current.Tilesmap;

        // What we know about the ocean is that there are tiles where
        // we can go with the ship and tiles where they cant

        List<Tile> ClosedSet = new List<Tile>();

        SimplePriorityQueue<Tile> OpenSet = new SimplePriorityQueue<Tile>();
        OpenSet.Enqueue(tileStart, 0);

        Dictionary<Tile, Tile> Came_From = new Dictionary<Tile, Tile>();

        Dictionary<Tile, float> g_score = new Dictionary<Tile, float>();

        g_score[tileStart] = 0;

        Dictionary<Tile, float> f_score = new Dictionary<Tile, float>();

        f_score[tileStart] = dist_between_without_diag(tileStart, tileEnd);

        while (OpenSet.Count > 0) {
            Tile current = OpenSet.Dequeue();
            if (current == tileEnd) {
                // We have reached our goal! or one that is equally as good but closer
                // Let's convert this into an actual sequene of
                // tiles to walk on, then end this constructor function!
                reconstruct_path(Came_From, current);
                return;
            }

            ClosedSet.Add(current);

            foreach (Tile neigh in current.GetNeighbours()) {
                if (neigh == null) {
                    continue;
                }
                if (tiles[neigh.X, neigh.Y] == false) {
                    continue;
                }



                OpenSet.Enqueue(neigh, 0);


            }




        } // while


    }

    bool doesJumpHitMove(Vector3 pos) {
        if (pos.x >= World.Current.Width || pos.y >= World.Current.Height) {
            return false; //outside map
        }
        if (pos.x < 0 || pos.y < 0) {
            return false; //outside map
        }
        return tiles[(int)pos.x, (int)pos.y];
    }



    float heuristic_cost_estimate(Tile a, Tile b) {

        return Mathf.Sqrt(
            Mathf.Pow(a.X - b.X, 2) +
            Mathf.Pow(a.Y - b.Y, 2)
        );

    }

    float dist_between(Tile a, Tile b) {
        // We can make assumptions because we know we're working
        // on a grid at this point.

        // Hori/Vert neighbours have a distance of 1
        if (Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1) {
            return 1f;
        }

        // Diag neighbours have a distance of 1.41421356237	
        if (Mathf.Abs(a.X - b.X) == 1 && Mathf.Abs(a.Y - b.Y) == 1) {
            return 1.41421356237f;
        }

        // Otherwise, do the actual math.
        return Mathf.Sqrt(
            Mathf.Pow(a.X - b.X, 2) +
            Mathf.Pow(a.Y - b.Y, 2)
        );

    }
    float dist_between_without_diag(Tile a, Tile b) {
        // We can make assumptions because we know we're working
        // on a grid at this point.

        // Hori/Vert neighbours have a distance of 1
        if (Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1) {
            return 1f;
        }
        // Otherwise, do the actual math.
        return Mathf.Sqrt(
            Mathf.Pow(a.X - b.X, 2) +
            Mathf.Pow(a.Y - b.Y, 2)
        );

    }
    void reconstruct_path(
        Dictionary<Tile, Tile> Came_From, Tile current) {
        // So at this point, current IS the goal.
        // So what we want to do is walk backwards through the Came_From
        // map, until we reach the "end" of that map...which will be
        // our starting node!
        Queue<Tile> total_path = new Queue<Tile>();
        total_path.Enqueue(current); // This "final" step is the path is the goal!

        while (Came_From.ContainsKey(current)) {
            // Came_From is a map, where the
            //    key => value relation is real saying
            //    some_node => we_got_there_from_this_node

            current = Came_From[current];
            total_path.Enqueue(current);
        }

        // At this point, total_path is a queue that is running
        // backwards from the END tile to the START tile, so let's reverse it.

        path = new Queue<Tile>(total_path.Reverse());

    }

    public Tile Dequeue() {
        return path.Dequeue();
    }

    bool IsClippingCornerWater(Tile curr, Tile neigh) {
        // If the movement from curr to neigh is diagonal (e.g. N-E)
        // Then check to make sure we aren't clipping (e.g. N and E are both walkable)

        int dX = curr.X - neigh.X;
        int dY = curr.Y - neigh.Y;

        if (Mathf.Abs(dX) + Mathf.Abs(dY) == 2) {
            // We are diagonal

            if (World.Current.GetTileAt(curr.X - dX, curr.Y).Type != TileType.Ocean) {
                // East or West is unfloatable, therefore this would be a driving on ground movement.
                return true;
            }

            if (World.Current.GetTileAt(curr.X, curr.Y - dY).Type != TileType.Ocean) {
                // North or South is unfloatable, therefore this would be a driving on ground movement.
                return true;
            }

            // If we reach here, we are diagonal, but not driving on ground movement
        }

        // If we are here, we are either not clipping, or not diagonal
        return false;
    }

    public int Length() {
        if (path == null)
            return 0;

        return path.Count;
    }

}

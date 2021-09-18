using Andja.Model;
using UnityEngine;

namespace Andja.Utility {

    public class DirectionalRect {
        public Direction direction;
        public Rect rect;
        public Rect Island;
        public Vector2 Center => rect.center;

        public void UpdateRect(Rect over) {
            if (direction == Direction.None) {
                rect = Rect.zero; //its the first one so just set to zero
                return;
            }
            bool yFirst = direction == Direction.N || direction == Direction.S;
            bool xFirst = direction == Direction.E || direction == Direction.W;

            if (yFirst) {
                if (Island.xMin <= over.xMax && Island.xMax >= over.xMin) {
                    if (direction == Direction.N && rect.yMax >= over.yMin)
                        rect.yMax = over.yMin;
                    if (direction == Direction.S && rect.yMin <= over.yMax)
                        rect.yMin = over.yMax;
                }
                else {
                    if (Island.xMin >= over.xMax && rect.xMin <= over.xMax) {
                        rect.xMin = over.xMax;
                    }
                    if (Island.xMax <= over.xMin && rect.xMax >= over.xMin) {
                        rect.xMax = over.xMin;
                    }
                }
            }
            if (xFirst) {
                if (Island.yMin <= over.yMax && Island.yMax >= over.yMin) {
                    if (direction == Direction.E && rect.xMax >= over.xMin)
                        rect.xMax = over.xMin;
                    if (direction == Direction.W && rect.xMin <= over.xMax)
                        rect.xMin = over.xMax;
                }
                else {
                    if (Island.yMin >= over.yMax && rect.yMin <= over.yMax) {
                        rect.yMin = over.yMax;
                    }
                    if (Island.yMax <= over.yMin && rect.yMax >= over.yMin) {
                        rect.yMax = over.yMin;
                    }
                }
            }
        }

        internal bool Overlaps(Rect toTest) {
            return rect.Overlaps(toTest);
        }
    }
}
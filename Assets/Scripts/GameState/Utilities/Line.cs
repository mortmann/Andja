using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Utility {
    /// <summary>
    /// Line between two Vector2s.
    /// In combination of a ui line renderer and it's thickness is used to display 
    /// and interact with trade routes.  
    /// </summary>
    public class Line {
        public static float LineThickness;
        public TradeRoute.Stop startingStop;
        public Vector2 a;
        public Vector2 b;
        public float Angle => Vector2.SignedAngle(Vector2.up, b - a);
        public Line(Vector2 p1, Vector2 p2, TradeRoute.Stop startingStop) {
            a = p1;
            b = p2;
            this.startingStop = startingStop;
        }

        public bool IsPointInLine(Vector2 c) {
            var dotproduct = (c.x - a.x) * (b.x - a.x) + (c.y - a.y) * (b.y - a.y);
            if (DistancePointToLine(a,b,c) > LineThickness) return false;
            if (dotproduct < 0) return false;
            if (dotproduct > (a - b).sqrMagnitude) return false;
            return true;
        }

        protected float DistancePointToLine(Vector2 p1, Vector2 p2, Vector2 c) {
            float x2mx1 = p2.x - p1.x;
            float y2my1 = p2.y - p1.y;
            return Mathf.Abs(x2mx1 * (p1.y - c.y) - (p1.x - c.x) * y2my1)
                    / Mathf.Sqrt(x2mx1 * x2mx1 + y2my1 * y2my1);
        }
    }
}

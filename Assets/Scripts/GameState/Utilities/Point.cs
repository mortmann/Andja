using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Point {
    public int X;
    public int Y;

    public Point(int x, int y) : this() {
        X = x;
        Y = y;
    }

    public Point(float x, float y) : this() {
        X = Mathf.RoundToInt(x);
        Y = Mathf.RoundToInt(y);
    }
    public float Distance(Point point) {
        return Mathf.Sqrt((X - point.X) * (X - point.X) + (Y - point.Y) * (Y - point.Y));
    }
    public bool IsInBounds(int x, int y, int width, int height) {
        return X >= x && Y >= y && X < width && Y < height;
    }
    public override bool Equals(object obj) {
        //Check for null and compare run-time types.
        if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
            return false;
        }
        else {
            Point p = (Point)obj;
            return (X == p.X) && (Y == p.Y);
        }
    }

    public override int GetHashCode() {
        return (X) ^ Y;
    }
    public static bool operator ==(Point x, Point y) {
        return x.X == y.X && x.Y == y.Y;
    }
    public static bool operator !=(Point x, Point y) {
        return !(x == y);
    }
    public static Point operator +(Point x, Point y) {
        return new Point(x.X+y.X,x.Y+y.Y);
    }
    public static Point operator -(Point x, Point y) {
        return new Point(x.X - y.X, x.Y - y.Y);
    }
    public static Point operator *(Point x, Point y) {
        return new Point(x.X * y.X, x.Y * y.Y);
    }
    public static Point operator /(Point x, Point y) {
        return new Point(x.X / y.X, x.Y / y.Y);
    }
    public override string ToString() {
        return "("+X+":"+Y+")";
    }
    public static implicit operator Vector2(Point v) { return new Vector2(v.X,v.Y); }
    public static implicit operator Point(Vector2 v) { return new Point(v.x,v.y); }
    public static implicit operator Point(Vector3 v) { return new Point(v.x,v.y); }


    private static Tuple<Point, double> GetNearestPoint(Point toPoint, LinkedList<Point> points) {
        Point? nearestPoint = null;
        double minDist2 = double.MaxValue;
        foreach (Point p in points) {
            double dist2 = p.Distance(toPoint);
            if (dist2 < minDist2) {
                minDist2 = dist2;
                nearestPoint = p;
            }
        }
        return new Tuple<Point, double>(nearestPoint.Value, minDist2);
    }

    public static List<Point> OrderByDistance(List<Point> points, int gridNx, int gridNy) {
        if (points.Count == 0)
            return points;

        double minX = points[0].X;
        double maxX = minX;
        double minY = points[0].Y;
        double maxY = minY;

        // Find the entire space occupied by the points
        foreach (Point p in points) {
            double x = p.X;
            double y = p.Y;

            if (x < minX)
                minX = x;
            else if (x > maxX)
                maxX = x;

            if (y < minY)
                minY = y;
            else if (y > maxY)
                maxY = y;
        }

        // The trick to avoid out of range
        maxX += 0.0001;
        maxY += 0.0001;

        double minCellSize2 = Pow2(Math.Min((maxX - minX) / gridNx, (maxY - minY) / gridNy));

        // Create cells subsets
        LinkedList<Point>[,] cells = new LinkedList<Point>[gridNx, gridNy];

        for (int j = 0; j < gridNy; j++)
            for (int i = 0; i < gridNx; i++)
                cells[i, j] = new LinkedList<Point>();

        Func<Point, Tuple<int, int>> getPointIndices = p => {
            int i = (int)((p.X - minX) / (maxX - minX) * gridNx);
            int j = (int)((p.Y - minY) / (maxY - minY) * gridNy);
            return new Tuple<int, int>(i, j);
        };

        foreach (Point p in points) {
            var indices = getPointIndices(p);
            cells[indices.Item1, indices.Item2].AddLast(p);
        }

        List<Point> ordered = new List<Point>(points.Count);

        Point? nextPoint = points[0];
        while (ordered.Count != points.Count) {
            Point? p = nextPoint;
            if (p.HasValue == false)
                break;
            var indices = getPointIndices(p.Value);
            int pi = indices.Item1;
            int pj = indices.Item2;

            ordered.Add(p.Value);
            cells[pi, pj].Remove(p.Value);

            int radius = 1;
            int maxRadius = Math.Max(Math.Max(pi, cells.GetLength(0) - pi), Math.Max(pj, cells.GetLength(1) - pj));

            double[] minDist2 = { double.MaxValue };    // To avoid access to modified closure
            Point? nearestPoint = null;

            while ((nearestPoint == null || minDist2[0] > minCellSize2 * (radius - 1)) && radius < maxRadius) {
                int minI = Math.Max(pi - radius, 0);
                int minJ = Math.Max(pj - radius, 0);
                int maxI = Math.Min(pi + radius, cells.GetLength(0) - 1);
                int maxJ = Math.Min(pj + radius, cells.GetLength(1) - 1);

                // Find the nearest point in the (i, j)-subset action
                Action<int, int> findAction = (i, j) => {
                    if (cells[i, j].Count != 0) {
                        var areaNearestPoint = GetNearestPoint(p.Value, cells[i, j]);
                        if (areaNearestPoint.Item2 < minDist2[0]) {
                            minDist2[0] = areaNearestPoint.Item2;
                            nearestPoint = areaNearestPoint.Item1;
                        }
                    }
                };

                if (radius == 1) {
                    // Iterate through all indexes in the 3x3
                    for (int j = minJ; j <= maxJ; j++) {
                        for (int i = minI; i <= maxI; i++) {
                            findAction(i, j);
                        }
                    }
                }
                else {
                    // Iterate through border only
                    for (int i = minI; i < maxI; i++) {
                        findAction(i, minJ);
                    }
                    for (int j = minJ; j < maxJ; j++) {
                        findAction(maxI, j);
                    }
                    for (int i = minI + 1; i <= maxI; i++) {
                        findAction(i, maxJ);
                    }
                    for (int j = minJ + 1; j <= maxJ; j++) {
                        findAction(minI, j);
                    }
                }

                radius++;
            }
            nextPoint = nearestPoint;
        }
        return ordered;
    }
    private static double Pow2(double x) {
        return x * x;
    }
}

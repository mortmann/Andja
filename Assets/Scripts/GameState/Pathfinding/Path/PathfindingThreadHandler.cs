using Andja.Controller;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Andja.Pathfinding {
    public enum JobStatus { InQueue, Calculating, Done, NoPath, Error, Canceled }

    public class PathfindingThreadHandler {
        public static readonly int NumberOfThreads = SystemInfo.processorCount / 2;
        public static ConcurrentQueue<PathJob> queuedJobs = new ConcurrentQueue<PathJob>();
        public static float averageSearchTime;
        public static int TotalSearches;

        static Thread[] threads;
        static bool FindPaths = true;
        public PathfindingThreadHandler() {
            threads = new Thread[NumberOfThreads];
            FindPaths = true;
            for (int i = 0; i < NumberOfThreads; i++) {
                int t = i;
                threads[i] = new Thread(() => ThreadLoop(t));
                threads[i].Start();
            }
        }
        public static PathJob EnqueueJob(IPathfindAgent agent, PathGrid grid, Vector2 Start, Vector2 End, Action OnFinished) {
            PathJob job = new PathJob(agent, grid, Start, End);
            job.OnFinished += OnFinished;
            queuedJobs.Enqueue(job);
            return job;
        }

        internal static void EnqueueJob(PathJob job, Action OnFinished) {
            job.OnFinished += OnFinished;
            queuedJobs.Enqueue(job);
        }

        internal static void RemoveJob(PathJob job) {
            job.SetStatus(JobStatus.Canceled);
        }

        public static PathJob EnqueueJob(IPathfindAgent agent, PathGrid grid, Vector2 Start, Vector2 End,
                                      List<Vector2> StartTiles, List<Vector2> EndTiles, Action OnFinished) {
            PathJob job = new PathJob(agent, grid, Start, End, StartTiles, EndTiles);
            queuedJobs.Enqueue(job);
            job.OnFinished += OnFinished;
            return job;
        }

        void ThreadLoop(int threadNumber) {
            System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
            StopWatch.Start();
            Dictionary<string, PathGrid> idToGrid = new Dictionary<string, PathGrid>();
            //Debug.Log("Start Thread "+ threadNumber);
            WorldGraph worldGraph = null;
            int check = 1000;
            while (FindPaths) {
                if (queuedJobs.IsEmpty) {
                    Thread.Sleep(50);
                    continue;
                }
                check--;
                if (check == 0) {
                    //Get rid of unused grids from routes that got deleted
                    //no need to check before every job
                    foreach (PathGrid pg in idToGrid.Values.ToArray()) {
                        if (pg.Obsolete)
                            idToGrid.Remove(pg.ID);
                    }
                    check = 1000;
                }
                if (queuedJobs.TryDequeue(out PathJob job) == false) continue;
                if (job.agent.IsAlive == false || job.Status == JobStatus.Canceled) continue;
                double started = StopWatch.Elapsed.TotalSeconds;
                //Debug.Log("PathfinderThread"+threadNumber + " started job " 
                //        + StopWatch.ElapsedMilliseconds + "(" + StopWatch.Elapsed.TotalSeconds + "s)");
                job.SetStatus(JobStatus.Calculating);
                try {
                    switch (job.agent.PathingMode) {
                        case PathingMode.World:
                            if (worldGraph == null) {
                                lock (Model.World.Current.WorldGraph) lock (Model.World.Current.Tilesmap)
                                    worldGraph = Model.World.Current.WorldGraph.Clone();
                            } else {
                                worldGraph.Reset();
                            }
                            job.Path = Pathfinder.FindOceanPath(job.agent, worldGraph, job.Start, job.End);
                            break;
                        case PathingMode.IslandMultiplePoints:
                            job.Path = DoMultipleStartPositions(job, idToGrid);
                            break;
                        case PathingMode.IslandSinglePoint:
                            job.Path = Pathfinder.Find(job, GetGrid(idToGrid, job.Grid[0]), job.Start, job.End);
                            if(job.agent.PathDestination == PathDestination.Exact) {
                                job.Path.Enqueue(job.End);
                            }
                            job.PathUsedGrid = job.Grid[0];
                            break;
                        case PathingMode.Route:
                            job.Path = DoRouteFind(job, idToGrid);
                            break;
                    }
                } catch (Exception e) {
                    job.SetStatus(JobStatus.Error);
                    Debug.LogException(e);
                    continue;
                }
                if (job.IsCanceled)
                    continue;
                if(job.PathUsedGrid != null && idToGrid[job.PathUsedGrid.ID].IsDirty && job.CheckPath() == false) {
                    //Which means Grid changed while this was searching...
                    //Need to redo it 
                    queuedJobs.Enqueue(job); //for now we will queue it again and redo it then not immediatly
                    continue;
                }
                if (job.Path == null) {
                    job.SetStatus(JobStatus.NoPath);
                    continue;
                }
                if (job.QueueModifier != null)
                    job.Path = job.QueueModifier.Invoke(job.Path);
                job.SetStatus(JobStatus.Done);
                lock (this) {
                    TotalSearches++;
                    averageSearchTime += (float)(StopWatch.Elapsed.TotalSeconds - started) / TotalSearches;
                }
                //Debug.Log("PathfinderThread" + threadNumber + "finished "
                //         + job.agent.PathingMode + "-job-" + job.Path.Count + " @"
                //         + StopWatch.ElapsedMilliseconds + " took " + (StopWatch.Elapsed.TotalSeconds - started) + "s)");
            }
            StopWatch.Stop();
        }
        static PathGrid GetGrid(Dictionary<string, PathGrid> idToGrid, PathGrid grid) {
            if(idToGrid.ContainsKey(grid.ID) == false || idToGrid[grid.ID].IsDirty) {
                idToGrid[grid.ID] = grid.Clone();
            } else {
                idToGrid[grid.ID].Reset();
            }
            return idToGrid[grid.ID];
        }
        private Queue<Vector2> DoRouteFind(PathJob job, Dictionary<string, PathGrid> idToGrid) {
            Queue<Vector2> Current = null;
            for (int i = 0; i < job.Grid.Length; i++) {
                if (job.IsCanceled)
                    return null;
                Queue<Vector2> temp = Pathfinder.Find(job,
                                            GetGrid(idToGrid, job.Grid[i]),
                                            null,
                                            null,
                                            job.StartTiles[i],
                                            job.EndTiles[i]
                                            );
                if(Current != null || temp != null && temp.Count < Current.Count) {
                    job.PathUsedGrid = job.Grid[i];
                    Current = temp;
                }
            }
            return Current;
        }

        Queue<Vector2> DoMultipleStartPositions(PathJob job, Dictionary<string, PathGrid> idToGrid, int gridIndex = 0) {
            List<Vector2> StartTiles = job.StartTiles[gridIndex];
            List<Vector2> EndTiles = job.EndTiles[gridIndex];

            float minDist = float.MaxValue;
            for (int i = 0; i < StartTiles.Count; i++) {
                for (int j = 0; j < EndTiles.Count; j++) {
                    float currDist = Pathfinder.HeuristicCostEstimate(job.agent, StartTiles[i], EndTiles[j]);
                    if (minDist > currDist) {
                        minDist = currDist;
                    }
                }
            }
            Queue<Vector2> currentQueue = null;
            foreach (Vector2 st in StartTiles) {
                if (job.IsCanceled)
                    return null;
                Queue<Vector2> temp = Pathfinder.Find(job,
                                            GetGrid(idToGrid, job.Grid[gridIndex]),
                                            st,
                                            job.End,
                                            StartTiles,
                                            EndTiles
                                            );
                if (currentQueue == null || temp != null && temp.Count < currentQueue.Count) {
                    currentQueue = temp;
                    job.PathUsedGrid = job.Grid[gridIndex];
                    if (currentQueue.Count == 0) {
                        continue;
                    }
                    if (currentQueue.Count == Mathf.CeilToInt(minDist)) {
                        break;
                    }
                }
            }
            return currentQueue;
        }

        internal static void Stop() {
            FindPaths = false;
            //for (int i = 0; i < NumberOfThreads; i++) {
            //    if (threads[i].IsAlive == false)
            //        continue;
            //    if (threads[i].Join(2000) == false)
            //        threads[i].Abort();
            //}
        }
    }

    public class PathJob {
        public JobStatus Status { get; protected set; }
        public bool IsCanceled => Status == JobStatus.Canceled;

        public Action OnPathInvalidated { get; internal set; }

        public IPathfindAgent agent;
        public PathGrid[] Grid;
        public Vector2 Start;
        public Vector2 End;
        public Queue<Vector2> Path;
        public PathGrid PathUsedGrid;
        public List<Vector2>[] StartTiles;
        public List<Vector2>[] EndTiles;
        public Func<Queue<Vector2>, Queue<Vector2>> QueueModifier;
        public Action OnFinished { get; internal set; }

        public PathJob(IPathfindAgent agent, int count) {
            Status = JobStatus.InQueue;
            this.agent = agent;
            Grid = new PathGrid[count];
            this.StartTiles = new List<Vector2>[count];
            this.EndTiles = new List<Vector2>[count];
            OnFinished += Finished;
        }

        public PathJob(IPathfindAgent agent, PathGrid grid, Vector2 start, Vector2 end,
                        List<Vector2> StartTiles, List<Vector2> EndTiles) {
            Status = JobStatus.InQueue;
            this.agent = agent;
            Start = start;
            End = end;
            Grid = new PathGrid[] { grid };
            this.StartTiles = new List<Vector2>[] { StartTiles };
            this.EndTiles = new List<Vector2>[] { EndTiles };
        }

        public PathJob(IPathfindAgent agent, PathGrid grid, Vector2 start, Vector2 end) {
            Status = JobStatus.InQueue;
            this.agent = agent;
            Grid = new PathGrid[] { grid };
            Start = start;
            End = end;
        }
        private void Finished() {
            PathUsedGrid.Changed += OnGridChange;
        }
        public void SetStatus(JobStatus status) {
            if(status == JobStatus.Canceled) {
                return;
            }
            lock(this) {
                Status = status;
            }
        }
        public void OnGridChange() {
            if (CheckPath() == false) {
                OnPathInvalidated?.Invoke();
            }
        }
        /// <summary>
        /// True = Path is valid
        /// False = Path is invalid
        /// </summary>
        /// <returns></returns>
        public bool CheckPath() {
            foreach (Vector2 p in Path) {
                Node n = PathUsedGrid.GetNode(p);
                if (n == null || n.IsPassable(agent.CanEnterCities?.ToList()) == false) {
                    return false;
                }
            }
            return true;
        }
    }
}
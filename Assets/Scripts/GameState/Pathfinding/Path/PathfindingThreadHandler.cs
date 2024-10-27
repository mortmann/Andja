using Andja.Controller;
using Andja.Utility;
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
        public static PathfindingThreadHandler Instance;
        public static readonly int NumberOfThreads = SystemInfo.processorCount / 2;
        public static ConcurrentQueue<PathJob> queuedJobs = new ConcurrentQueue<PathJob>();
        public static ConcurrentQueue<PathJob> uiJobs = new ConcurrentQueue<PathJob>();
        public static float averageSearchTime;
        public static int TotalSearches;
        private static readonly Dictionary<string, PathGrid> MainThreadIdToGrid = new Dictionary<string, PathGrid>();
        private static readonly WorldGraph MainThreadWorldGraph = null;
        public static bool FindPaths = true;
        public PathfindingThreadHandler() {
            Instance = this;
        }
        public void Start() {
            var threads = new Thread[NumberOfThreads];
            FindPaths = true;
            for (int i = 0; i < NumberOfThreads; i++) {
                int t = i;
                threads[i] = new Thread(() => ThreadLoop(t));
                threads[i].Start();
            }
        }
        public static PathJob EnqueueJob(IPathfindAgent agent, PathGrid grid, Vector2 start, Vector2 end,
            Action onFinished, Func<Queue<Vector2>, Queue<Vector2>> queueModifier = null) {
            PathJob job = new PathJob(agent, grid, start, end);
            job.OnFinished += onFinished;
            job.QueueModifier += queueModifier;
            return Instance.EnqueueJob(job);
        }
        public virtual PathJob EnqueueJob(PathJob job) {
            queuedJobs.Enqueue(job);
            return job;
        }
        internal static void EnqueueJob(PathJob job, Action onFinished, bool mainThread = false) {
            job.OnFinished += onFinished;
            if (mainThread) {
                DoJob(job, MainThreadWorldGraph, MainThreadIdToGrid);
            }
            else {
                Instance.EnqueueJob(job);
            }
        }

        internal static void RemoveJob(PathJob job) {
            job?.SetStatus(JobStatus.Canceled);
        }

        public static PathJob EnqueueJob(IPathfindAgent agent, PathGrid grid, Vector2 start, Vector2 end,
                                      List<Vector2> startTiles, List<Vector2> endTiles, Action onFinished) {
            PathJob job = new PathJob(agent, grid, start, end, startTiles, endTiles);
            job.OnFinished += onFinished;
            return Instance.EnqueueJob(job);
        }

        private void ThreadLoop(int threadNumber) {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
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
                double started = stopWatch.Elapsed.TotalSeconds;
                //Debug.Log("PathfinderThread" + threadNumber + " started job "
                //        + StopWatch.ElapsedMilliseconds + "(" + StopWatch.Elapsed.TotalSeconds + "s)");
                if (DoJob(job, worldGraph, idToGrid) == false)
                    continue;
                lock (this) {
                    TotalSearches++;
                    averageSearchTime += (float)((stopWatch.Elapsed.TotalSeconds - started) - averageSearchTime) / TotalSearches;
                }
                //Debug.Log("PathfinderThread" + threadNumber + "finished "
                //         + job.agent.PathingMode + "-job-" + job.Path.Count + " @"
                //         + StopWatch.ElapsedMilliseconds + " took " + (StopWatch.Elapsed.TotalSeconds - started) + "s)");
            }
            Log.GAME_INFO("Shutting down Pathfinding Thread: " + (threadNumber + 1) + "/" + NumberOfThreads);
            stopWatch.Stop();
        }

        private static bool DoJob(PathJob job, WorldGraph worldGraph, Dictionary<string, PathGrid> idToGrid) {
            job.SetStatus(JobStatus.Calculating);
            try {
                switch (job.agent.PathingMode) {
                    case PathingMode.World:
                        if (worldGraph == null) {
                            lock (Model.World.Current.WorldGraph) lock (Model.World.Current.TilesMap)
                                    worldGraph = Model.World.Current.WorldGraph.Clone();
                        }
                        else {
                            worldGraph.Reset();
                        }
                        job.SetResult(Pathfinder.FindOceanPath(job, job.agent, worldGraph, job.Start, job.End));
                        break;
                    case PathingMode.IslandMultiplePoints:
                        job.SetResult(DoMultipleStartPositions(job, idToGrid));
                        break;
                    case PathingMode.IslandSinglePoint:
                        job.SetResult(Pathfinder.Find(job, GetGrid(idToGrid, job.Grid[0]), job.Start, job.End));
                        if (job.agent.PathDestination == PathDestination.Exact && job.Path != null) {
                            job.Path.Enqueue(job.End);
                            job.Time += Vector2.Distance(job.Path.Last(), job.End);
                        }
                        job.PathUsedGrid = job.Grid[0];
                        break;
                    case PathingMode.Route:
                        job.SetResult(DoRouteFind(job, idToGrid));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e) {
                job.SetStatus(JobStatus.Error);
                Debug.LogException(e);
            }
            if (job.Status == JobStatus.Error)
                return false;
            if (job.IsCanceled)
                return true;
            if (job.PathUsedGrid != null && idToGrid[job.PathUsedGrid.ID].IsDirty && job.CheckPath() == false) {
                //Which means Grid changed while this was searching...
                //Need to redo it 
                queuedJobs.Enqueue(job); //for now we will queue it again and redo it then not immediatly
                return true;
            }
            if (job.Path == null) {
                job.SetStatus(JobStatus.NoPath);
                return true;
            }
            if (job.QueueModifier != null)
                job.Path = job.QueueModifier.Invoke(job.Path);
            job.SetStatus(JobStatus.Done);
            return true;
        }

        private static PathGrid GetGrid(IDictionary<string, PathGrid> idToGrid, PathGrid grid) {
            if (idToGrid.ContainsKey(grid.ID) == false) {
                idToGrid[grid.ID] = grid.Clone();
            }
            else {
                idToGrid[grid.ID].Reset();
            }
            return idToGrid[grid.ID];
        }
        private static Pathfinder.Result DoRouteFind(PathJob job, IDictionary<string, PathGrid> idToGrid) {
            Pathfinder.Result current = null;
            for (int i = 0; i < job.Grid.Length; i++) {
                if (job.IsCanceled)
                    return null;
                Pathfinder.Result result = Pathfinder.Find(job,
                                            GetGrid(idToGrid, job.Grid[i]),
                                            null,
                                            null,
                                            job.StartTiles[i],
                                            job.EndTiles[i]
                                            );
                if (current == null || result != null && result.Time < current.Time) {
                    job.PathUsedGrid = job.Grid[i];
                    current = result;
                }
            }
            return current;
        }

        public static Pathfinder.Result DoMultipleStartPositions(PathJob job, Dictionary<string, PathGrid> idToGrid) {
            Pathfinder.Result currentResult = null;
            for (int i = 0; i < job.Grid.Length; i++) {
                Pathfinder.Result temp = DoGridMultipleStartPositions(job, idToGrid, i);
                if (currentResult != null && (temp == null || temp.Time >= currentResult.Time)) continue;
                currentResult = temp;
                job.PathUsedGrid = job.Grid[i];
                if (currentResult == null || currentResult.Path.Count == 0) {
                    continue;
                }
            }
            return currentResult;
        }

        public static Pathfinder.Result DoGridMultipleStartPositions(PathJob job, Dictionary<string, PathGrid> idToGrid, int gridIndex) {
            List<Vector2> startTiles = job.StartTiles[gridIndex];
            List<Vector2> endTiles = job.EndTiles[gridIndex];
            float minDist = (from start in startTiles 
                             from end in endTiles 
                             select Pathfinder.HeuristicCostEstimate(job.agent, start, end))
                                              .Prepend(float.MaxValue)
                                              .Min();
            Pathfinder.Result current = null;
            foreach (Vector2 st in startTiles) {
                if (job.IsCanceled)
                    return null;
                if (current?.Path?.Contains(st) == true) {
                    continue;
                }
                Pathfinder.Result temp = Pathfinder.Find(job,
                                            GetGrid(idToGrid, job.Grid[gridIndex]),
                                            st,
                                            job.End,
                                            startTiles,
                                            endTiles
                                            );
                if (current == null || temp != null && temp.Time < current.Time) {
                    current = temp;
                    if (current == null || current.Path.Count == 0) {
                        continue;
                    }
                }
            }
            return current;
        }

        internal static void Stop() {
            Instance = null;
            FindPaths = false;
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
        public float Time;

        public PathJob(IPathfindAgent agent, int count) {
            Status = JobStatus.InQueue;
            this.agent = agent;
            Grid = new PathGrid[count];
            this.StartTiles = new List<Vector2>[count];
            this.EndTiles = new List<Vector2>[count];
            OnFinished += Finished;
        }

        public PathJob(IPathfindAgent agent, PathGrid grid, Vector2 start, Vector2 end,
                        List<Vector2> startTiles, List<Vector2> endTiles) {
            Status = JobStatus.InQueue;
            this.agent = agent;
            Start = start;
            End = end;
            Grid = new[] { grid };
            this.StartTiles = new[] { startTiles };
            this.EndTiles = new[] { endTiles };
        }

        public PathJob(IPathfindAgent agent, PathGrid grid, Vector2 start, Vector2 end) {
            Status = JobStatus.InQueue;
            this.agent = agent;
            Grid = new[] { grid };
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
        public void OnGridChange(Model.Tile t) {
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
            return Path.Select(p => PathUsedGrid.GetNode(p)).All(n => n != null && n.IsPassable(agent.CanEnterCities?.ToList()));
        }

        internal void SetResult(Pathfinder.Result result) {
            Path = result.Path;
            Time = result.Time;
        }
    }
}
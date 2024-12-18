using Andja.Controller;
using Andja.Editor;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using Range = Andja.Utility.Range;

namespace Andja.Model.Generator {

    public partial class MapGenerator : MonoBehaviour {
        private Rect ColdIslandSpace;
        private Rect MiddleIslandSpace;
        private Rect WarmIslandSpace;
        private Dictionary<Climate, Rect> climateRectangles;
        public static MapGenerator Instance;
        public readonly int MinTilesAroundIsland = 0;
        private bool startedGenerating = false;
        private bool tilesPopulated = false;
        private Tile[] tiles;
        private List<IslandData> toPlaceIslands;
        private List<IslandData> doneIslands;
        private ThreadRandom mapThreadRandom;
        private ThreadRandom placeIslandThreadRandom;

        public int MapSeed;
        public int Width;
        public int Height;
        private List<Task> generatorsTasks;

        private int completedIslands = 0;
        public Dictionary<Tile, Structure> tileToStructure;
        public List<DirectionalRect> recantgleEmptySpaces;
        private Dictionary<Rect, IslandData> placeToIsland;
        private Dictionary<DirectionalRect, Color> rectToColor;

        private List<IslandGenerator> islandGenerators;
        private ConcurrentBag<SaveIsland> loadedIslandsList;
        private float placeProgress;

        public float GeneratedProgressPercentage {
            get {
                //if (started == false && EditorController.IsEditor ==false)
                //    return 0;
                float percentage = 0;

                int islandamount = toLoadIslands + toGeneratorIslands;
                if (islandGenerators != null) {
                    foreach (IslandGenerator islandGenerator in islandGenerators) {
                        percentage += islandGenerator.Progress;
                    }
                    percentage /= (float)islandGenerators.Count;
                    percentage *= toGeneratorIslands / islandamount;
                }
                if (toLoadIslands > 0) {
                    percentage += (float)loadedIslands / (float)toLoadIslands;
                    percentage *= toLoadIslands / islandamount;
                }

                if (EditorController.IsEditor == false) {
                    percentage *= 0.9f;
                    percentage += placeProgress * 0.1f;
                }
                return percentage;
            }
        }

        public bool IsDone {
            get {
                return IsFinished;
            }
        }

        private bool ReadyToPlace {
            get {
                return started &&
                  toGeneratorIslands + toLoadIslands > 0
                  && completedIslands == toGeneratorIslands
                  && loadedIslands == toLoadIslands;
            }
        }

        private bool ReadyToFillWorld {
            get {
                return tilesPopulated &&
                  ReadyToPlace && IsFinished == false;
            }
        }

        public static int EditorSeed { get; private set; }

        private bool IsFinished; //Internal use only
        private bool IsPlacing;
        private int toGeneratorIslands = 0;
        private int toLoadIslands = 0;
        private int islandPlaceSeed = 0;
        private int loadedIslands = 0;

        private List<IslandGenInfo> islandsToGenerate;
        private Task loadTask;
        private World world;
        private bool started;
        private Dictionary<KeyValuePair<Climate, Size>, List<string>> islands;
        private System.Diagnostics.Stopwatch stopwatch;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two MapController.");
            }
            Instance = this;
            this.gameObject.transform.parent = null;
            TileSpriteController.LoadSprites();
            if (EditorController.Generate) {
                EditorGenerate(EditorController.Width, EditorController.Height, EditorController.GetEditorGenInfo());
            }
            if (EditorController.IsEditor && EditorController.Generate == false) {
                Destroy();
            }
            DontDestroyOnLoad(this.gameObject);

            toPlaceIslands = new List<IslandData>();
            tileToStructure = new Dictionary<Tile, Structure>();
            islands = SaveController.GetIslands();
            if (WorldController.Instance != null && WorldController.Instance.World != null)
                Destroy();
        }

        internal Dictionary<Tile, Structure> GetStructures() {
            return tileToStructure;
        }

        public void DefineParameters(int seed, int height, int width, Dictionary<IslandGenInfo, Range> numberRangeOfIslandsSizes,
                                                                        List<string> hasToUseIslands) {
            started = true;
            MapSeed = seed;
            mapThreadRandom = new ThreadRandom(seed);
            Log.GENERATION_WARNING("GENERATING MAP with Seed: " + seed + "" 
                + Environment.NewLine 
                + (hasToUseIslands == null || hasToUseIslands.Count==0? "" : "Islands: " + string.Join(",", hasToUseIslands)));
            //THIS MUST BE THE FIRST RANDOM NUMBER!
            //TO make sure that there is no change of the placement
            islandPlaceSeed = mapThreadRandom.Range(0, int.MaxValue);
            Width = width;
            Height = height;
            tiles = new Tile[Width * Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    SetTileAt(x, y, new Tile(x, y));
                }
            }
            List<string> newChoosenIslands = new List<string>();
            islandsToGenerate = new List<IslandGenInfo>();
            foreach (IslandGenInfo genInfo in numberRangeOfIslandsSizes.Keys) {
                if (genInfo.generate) {
                    Log.GENERATION_WARNING("Generating Island still in alpha! Use with absolute caution. Note: it is still not guaranteed to have usable islands.");
                    Range range = numberRangeOfIslandsSizes[genInfo];
                    int numberOfIslands = range.GetRandomCount(mapThreadRandom);
                    for (int i = 0; i < numberOfIslands; i++) {
                        islandsToGenerate.Add(new IslandGenInfo(genInfo)); // TODO: rethink this !
                    }
                }
                else {
                    Range range = numberRangeOfIslandsSizes[genInfo];
                    int numberOfIslands = range.GetRandomCount(mapThreadRandom);
                    for (int i = 0; i < numberOfIslands; i++) {
                        string island = GetRandomIslandFileName(
                            Island.GetSizeTyp(genInfo.Width.Middle, genInfo.Height.Middle), genInfo.climate);
                        if (hasToUseIslands == null) //TODO: stop this fix for loading! -- Needed for now for correct random Values
                            newChoosenIslands.Add(island);
                    }
                }
            }
            if (hasToUseIslands == null)
                hasToUseIslands = newChoosenIslands;
            float Percentage = 0.225f;
            float overlap = 0.025f;
            float PercentageOverlap = Percentage - overlap;
            float smallHeight = Percentage * Height;
            WarmIslandSpace = new Rect {
                x = 0,
                y = 0,
                width = Width,
                height = smallHeight
            };
            MiddleIslandSpace = new Rect {
                x = 0,
                y = PercentageOverlap * Height,
                width = Width,
                yMax = (1 - Percentage) * Height
            };
            ColdIslandSpace = new Rect {
                x = 0,
                y = Height - smallHeight,
                width = Width,
                height = smallHeight
            };
            climateRectangles = new Dictionary<Climate, Rect>();
            climateRectangles.Add(Climate.Cold, ColdIslandSpace);
            climateRectangles.Add(Climate.Middle, MiddleIslandSpace);
            climateRectangles.Add(Climate.Warm, WarmIslandSpace);

            //Load the premade island if there are any
            LoadIslands(hasToUseIslands);
            //THIS IS TEMPORARY -- replace with an better solution that doesnt require a secondary thing besides seed for a map
            GameData.Instance.usedIslands = hasToUseIslands.ToArray();
            //generate new islands procedurally
            Generate();
            //Let the Update Function do its Job
            startedGenerating = true;
        }

        private string GetRandomIslandFileName(Size size, Climate climate) {
            KeyValuePair<Climate, Size> key = new KeyValuePair<Climate, Size>(climate, size);
            if (islands.ContainsKey(key) == false) {
                Debug.LogError("We do not have islands for " + climate + "-" + size + " and needs to be created");
                return null;
            }
            List<string> keyIslands = islands[key];
            return keyIslands[mapThreadRandom.Range(0, keyIslands.Count)];
        }

        public List<IslandData> GetIslandStructs() {
            return doneIslands;
        }

        private void LoadIslands(List<string> toLoad) {
            toLoadIslands = toLoad.Count;
            loadedIslandsList = new ConcurrentBag<SaveIsland>();
            //create as thread to be safe this isnt going to slow down
            loadTask = Task.Factory.StartNew(() => {
                foreach (string name in toLoad) {
                    loadedIslandsList.Add(SaveController.Instance.GetIslandSave(name));
                    loadedIslands++;
                }
            });
            loadTask.ConfigureAwait(false);
        }

        private void SetToGenerate(IEnumerable<IslandGenInfo> toGenerate) {
            islandsToGenerate = new List<IslandGenInfo>(toGenerate);
        }

        public void EditorGenerate(int height, int width, params IslandGenInfo[] toGenerate) {
            started = true;
            Width = width;
            Height = height;
            toGeneratorIslands = 1;
            islandsToGenerate = new List<IslandGenInfo>(toGenerate);
            mapThreadRandom = new ThreadRandom();
            Generate();
        }

        private void Generate() {
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            if (islandsToGenerate == null) {
                return;
            }

            startedGenerating = true;

            int islandCount = islandsToGenerate.Count;
            islandGenerators = new List<IslandGenerator>();
            for (int i = 0; i < islandCount; i++) {
                IslandGenInfo igi = islandsToGenerate[i];
                int seed = mapThreadRandom.Range(0, int.MaxValue);
                IslandGenerator isg = new IslandGenerator(
                    mapThreadRandom.Range(igi.Width.lower, igi.Width.upper),
                    mapThreadRandom.Range(igi.Height.lower, igi.Height.upper),
                    seed,
                    igi.climate
                );
                if (EditorController.IsEditor) {
                    MapGenerator.EditorSeed = seed;
                }
                islandGenerators.Add(isg);
                //for now we just put them at fix spots
            }

            generatorsTasks = new List<Task>();
            for (int i = 0; i < islandGenerators.Count; i++) {
                IslandGenerator isg = islandGenerators[i];
                Task t = Task.Factory.StartNew(() => {
                    isg.Start();
                });
                t.ConfigureAwait(false);
                generatorsTasks.Add(t);
            }
            toGeneratorIslands = islandGenerators.Count;
        }

        public void Update() {
            if (startedGenerating == false) {
                return;
            }
            if (IsDone) {
                return;
            }
            if (generatorsTasks != null) {
                for (int i = 0; i < generatorsTasks.Count; i++) {
                    Task t = generatorsTasks[i];
                    if (t.IsCompleted && IsDone == false) {
                        //Debug.Log("TASK Number " + i + " is finished! ");
                        if (t.IsFaulted) {
                            Debug.LogError("TASK Number " + i + " had an " + t.Exception.InnerException.Message + " exception \n" + t.Exception.InnerException.StackTrace);
                        }
                        completedIslands++;
                        generatorsTasks.Remove(t);
                    }
                }
            }
            if (loadTask != null && loadTask.IsFaulted)
                Debug.Log(loadTask.Exception);

            if (ReadyToPlace) {
                if (IsPlacing == false) {
                    IsPlacing = true;

                    //if any island has been generated add them
                    if (generatorsTasks != null) {
                        foreach (IslandGenerator gen in islandGenerators) {
                            toPlaceIslands.Add(gen.GetIslandData());
                        }
                    }
                    if (loadedIslandsList != null) {
                        foreach (SaveIsland save in loadedIslandsList) {
                            toPlaceIslands.Add(new IslandData(save));
                        }
                    }
                    GenerateIslandDataThings();
                    if (EditorController.IsEditor == false) {
                        //we have multiple islands to place!
                        StartCoroutine(PlaceIslandOnMap(toPlaceIslands));
                    }
                    else {
                        //the islands is the whole map! so just set the tiles to the map tiles
                        tiles = islandGenerators[0].Tiles;
                        tileToStructure = islandGenerators[0].tileToStructure;
                        tilesPopulated = true;
                    }
                    if (tilesPopulated == false)
                        return;
                }
            }
            if (ReadyToFillWorld) {
                stopwatch.Stop();
                Log.GENERATION_INFO("Generated map (Seed:" + MapSeed + ")  with island number " + toPlaceIslands.Count + " in a Map " + Width + " : " +
                    Height  + " in " + stopwatch.ElapsedMilliseconds + "ms (" + stopwatch.Elapsed.TotalSeconds + "s)! ");
                if (SaveController.IsLoadingSave == false && WorldController.Instance != null) {
                    //Find the "spawn" of the ships
                    Vector2 center = new Vector2(Width / 2, Height / 2);
                    recantgleEmptySpaces = recantgleEmptySpaces.OrderBy((x) => Vector2.Distance(x.Center, center)).ToList();
                    Vector2[] spawnPoints = new Vector2[PlayerController.Instance.PlayerCount];
                    Rect spawnRect = recantgleEmptySpaces[0].rect;
                    float radius = Mathf.Min(mapThreadRandom.Range(spawnRect.width / 6, spawnRect.width / 4), 15, mapThreadRandom.Range(spawnRect.height / 6, spawnRect.height / 4));
                    float degreesPerPlayer = (360 / (float)PlayerController.Instance.PlayerCount) * Mathf.Deg2Rad;
                    for (int i = 0; i < PlayerController.Instance.PlayerCount; i++) {
                        Vector2 vec2 = new Vector2(Mathf.RoundToInt(spawnRect.center.x + radius * Mathf.Cos(degreesPerPlayer * i)),
                                                   Mathf.RoundToInt(spawnRect.center.y + radius * Mathf.Sin(degreesPerPlayer * i))
                                                  );
                        spawnPoints[i] = vec2;
                    }
                    WorldController.Instance.SetGeneratedWorld(GetWorld(false), tileToStructure, spawnRect, spawnPoints);
                    MakeOnLoadDestroy();
                }
                else if (SaveController.IsLoadingSave == true && WorldController.Instance != null) {
                    World.Current.LoadTiles(GetTiles(), Width, Height);
                }

                IsFinished = true;
                transform.SetParent(null);
            }
        }

        private Dictionary<Climate, WeightedRandomList<FertilityPrototypeData>> fertilityRandomListPerClimate;
        private Dictionary<Climate, WeightedRandomList<ResourceGenerationInfo>> resourcesRandomListPerClimate;

        private void GenerateIslandDataThings() {
            System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            s.Start();
            Dictionary<Climate, List<FertilityPrototypeData>> toBeAllocatedFertilities = new Dictionary<Climate, List<FertilityPrototypeData>>();
            fertilityRandomListPerClimate = new Dictionary<Climate, WeightedRandomList<FertilityPrototypeData>>();
            resourcesRandomListPerClimate = new Dictionary<Climate, WeightedRandomList<ResourceGenerationInfo>>();

            Dictionary<Climate, List<ResourceGenerationInfo>> toBeAllocatedResources = new Dictionary<Climate, List<ResourceGenerationInfo>>();
            foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
                toBeAllocatedFertilities[climate] = new List<FertilityPrototypeData>();
                toBeAllocatedFertilities[climate].AddRange(PrototypController.Instance.AllFertilitiesDatasPerClimate[climate]);
                toBeAllocatedResources[climate] = new List<ResourceGenerationInfo>();
                toBeAllocatedResources[climate].AddRange(PrototypController.Instance.ClimateToResourceGeneration[climate]);
            }
            Dictionary<Climate, int> climateNeededFertilities = new Dictionary<Climate, int>();
            Dictionary<Climate, int> climateNeededResources = new Dictionary<Climate, int>();
            foreach (IslandData data in toPlaceIslands) {
                if (data.fertilities != null) {
                    foreach (FertilityPrototypeData fer in data.fertilities) {
                        foreach (Climate c in fer.climates) {
                            if (toBeAllocatedFertilities[c].Remove(fer) == false)
                                fer.Select(1);
                        }
                    }
                }
                if (data.Resources != null) {
                    foreach (string id in data.Resources.Keys) {
                        if (data.Resources[id] > 0) {
                            toBeAllocatedResources[data.Climate].RemoveAll(x => x.ID == id);
                        }
                    }
                }
            }

            foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
                fertilityRandomListPerClimate[climate] =
                    new WeightedRandomList<FertilityPrototypeData>(PrototypController.Instance.AllFertilitiesDatasPerClimate[climate]);
                resourcesRandomListPerClimate[climate] =
                    new WeightedRandomList<ResourceGenerationInfo>(PrototypController.Instance.ClimateToResourceGeneration[climate]);
                climateNeededFertilities[climate] = 0;
                climateNeededResources[climate] = 0;
            }
            List<ResourceGenerationInfo> unlimitedResources = PrototypController.Instance.ResourceGenerations.Where(r => r.unlimited).ToList();
            List<IslandData> datas = toPlaceIslands.ToList();
            foreach (IslandData data in datas) {
                if (data.NeedsFertility) {
                    for (int i = 0; i < data.FertilityCount; i++) {
                        data.AddFertility(fertilityRandomListPerClimate[data.Climate].GetRandom(mapThreadRandom, data.fertilities, toPlaceIslands.Count));
                    }
                }
                if (data.NeedsResources) {
                    for (int i = 0; i < data.ResourcesCount; i++) {
                        List<ResourceGenerationInfo> exclude = new List<ResourceGenerationInfo>(data.resources);
                        exclude.AddRange(data.ExcludedResources);
                        exclude.AddRange(unlimitedResources);
                        data.AddResources(resourcesRandomListPerClimate[data.Climate].GetRandom(mapThreadRandom, data.resources, toPlaceIslands.Count), mapThreadRandom);
                    }
                }
                unlimitedResources.ForEach(unlimited => data.AddUnlimitedResources(unlimited));
            }

            foreach (Climate climate in Enum.GetValues(typeof(Climate))) {
                if (fertilityRandomListPerClimate[climate].HasNoMustLeft == false) {
                    List<IslandData> sorted = toPlaceIslands.Where(x => x.Climate == climate).OrderBy(x => x.Tiles.Length).ToList();
                    int i = 0;
                    while (fertilityRandomListPerClimate[climate].HasNoMustLeft == false && i < sorted.Count * 2) {
                        sorted[i % sorted.Count].AddFertility
                            (fertilityRandomListPerClimate[climate].GetRandom(mapThreadRandom, sorted[i % sorted.Count].fertilities, toPlaceIslands.Count));
                        i++;
                    }
                }
                if (resourcesRandomListPerClimate[climate].HasNoMustLeft == false) {
                    List<IslandData> sorted = toPlaceIslands.Where(x => x.Climate == climate).OrderBy(x => x.Tiles.Length).ToList();
                    int maxRes = sorted.Max(x => x.ResourcesCount);
                    int i = 0;
                    while (resourcesRandomListPerClimate[climate].HasNoMustLeft == false 
                                && i < sorted.Count * (maxRes * 2)) {
                        List<ResourceGenerationInfo> exclude = new List<ResourceGenerationInfo>(sorted[i % sorted.Count].resources);
                        exclude.AddRange(sorted[i % sorted.Count].ExcludedResources);
                        sorted[i % sorted.Count].AddResources(resourcesRandomListPerClimate[climate]
                                .GetRandom(mapThreadRandom, exclude, toPlaceIslands.Count), mapThreadRandom);
                        i++;
                    }
                }
            }
            Log.GENERATION_INFO("RANDOM RESOURCES DONE " + s.Elapsed.TotalSeconds);
            
        }

        private Dictionary<string, int> GetResourcesFromRange(Dictionary<string, Range> resourcesRanges) {
            Dictionary<string, int> resources = new Dictionary<string, int>();
            foreach (string id in resourcesRanges.Keys) {
                resources[id] = mapThreadRandom.Range(resourcesRanges[id].lower, resourcesRanges[id].upper);
            }
            return resources;
        }

        private void MakeOnLoadDestroy() {
            GameObject go = new GameObject("DestroyLoad");
            transform.SetParent(go.transform);
        }

        private IEnumerator PlaceIslandOnMap(List<IslandData> islandStructs) {
            placeProgress = 0;
            //Makes it easier to have the same placement of the islands
            placeIslandThreadRandom = new ThreadRandom(islandPlaceSeed);
            List<Rect> rectangleIslands = new List<Rect>();
            rectToColor = new Dictionary<DirectionalRect, Color>();

            placeToIsland = new Dictionary<Rect, IslandData>();
            recantgleEmptySpaces = new List<DirectionalRect>();
            int retriesWorld = 32;
            bool worldFailed = false;
            List<IslandData> toPlaceIslands = new List<IslandData>(islandStructs);
            //while it fails repeat it until it was "retriesWorld" times tried
            do {
                yield return new WaitForEndOfFrame();
                placeToIsland.Clear();
                rectangleIslands.Clear();
                Vector2 worldMin = Vector2.zero;
                Vector2 worldMax = new Vector2(Width, Height);
                Color[] colors = new Color[] { Color.yellow, Color.magenta, Color.white, Color.green, Color.cyan, new Color(0.5f, 0.5f, 0) };
                int i = 0;
                recantgleEmptySpaces.Add(new DirectionalRect() {
                    rect = new Rect(0, 0, Width, Height)
                });
                //if we have to try again clear the done islands
                foreach (IslandData island in toPlaceIslands) {
                    Color color = colors[i % 6];
                    int islandTries = 0;
                    Rect hasToBeIn = climateRectangles[island.Climate];
                    List<DirectionalRect> possible = recantgleEmptySpaces.FindAll(r => r.Overlaps(hasToBeIn));
                    possible = possible.OrderByDescending(r => (r.rect.width + r.rect.height) / 2).ToList();
                    if (possible.Count == 0) {
                        Debug.LogWarning("COULD NOT FIND A PLACE TO FIT ISLAND: " + island);
                        continue;
                    }
                    while (islandTries < 32) {
                        if (possible.Count == 0) {
                            worldFailed = true;
                            break;
                        }
                        Rect placeIn = possible[islandTries % possible.Count].rect;
                        int MaxY = (int)Mathf.Min(hasToBeIn.yMax, placeIn.yMax);
                        int MinY = (int)Mathf.Max(hasToBeIn.yMin, placeIn.yMin);
                        int MaxX = (int)Mathf.Min(hasToBeIn.xMax, placeIn.xMax);
                        int MinX = (int)Mathf.Max(hasToBeIn.xMin, placeIn.xMin);
                        islandTries++;
                        if (MaxX - MinX < island.Width)
                            continue;
                        if (MaxY - MinY < island.Height)
                            continue;
                        int x = placeIslandThreadRandom.Range(MinX, MaxX - island.Width);
                        int y = placeIslandThreadRandom.Range(MinY, MaxY - island.Height);

                        Rect toTest = new Rect(x, y, island.Width + MinTilesAroundIsland, island.Height + MinTilesAroundIsland);
                        foreach (Rect item in placeToIsland.Keys) {
                            if (toTest.Overlaps(item)) //failsafe should NEVER happen!?
                                continue;
                        }
                        //remove all spaces that got invalid through placing it here
                        foreach (DirectionalRect dr in recantgleEmptySpaces) {
                            if (toTest.Overlaps(dr.rect)) {
                                dr.UpdateRect(toTest);
                            }
                        }
                        //next generate new rect
                        Rect Top = new Rect {
                            xMin = worldMin.x,
                            yMin = toTest.yMax,
                            xMax = worldMax.x,
                            yMax = worldMax.y
                        };
                        Rect Right = new Rect {
                            xMin = toTest.xMax,
                            yMin = worldMin.y,
                            xMax = worldMax.x,
                            yMax = worldMax.y
                        };
                        Rect Bottom = new Rect {
                            xMin = worldMin.x,
                            yMin = worldMin.y,
                            xMax = worldMax.x,
                            yMax = toTest.yMin
                        };
                        Rect Left = new Rect {
                            xMin = worldMin.x,
                            yMin = worldMin.y,
                            xMax = toTest.xMin,
                            yMax = worldMax.y
                        };
                        DirectionalRect temp = GetNewRects(rectangleIslands, Top, toTest, Direction.N);
                        rectToColor[temp] = color;
                        recantgleEmptySpaces.Add(temp);

                        temp = GetNewRects(rectangleIslands, Right, toTest, Direction.E);
                        rectToColor[temp] = color;
                        recantgleEmptySpaces.Add(temp);

                        temp = GetNewRects(rectangleIslands, Bottom, toTest, Direction.S);
                        rectToColor[temp] = color;
                        recantgleEmptySpaces.Add(temp);

                        temp = GetNewRects(rectangleIslands, Left, toTest, Direction.W);
                        rectToColor[temp] = color;
                        recantgleEmptySpaces.Add(temp);

                        i++;
                        rectangleIslands.Add(toTest);
                        placeToIsland.Add(toTest, island);
                        yield return new WaitForEndOfFrame();
                        //Debug.Log("Placing island on x " + x + "y " + y + " !");
                        break;
                    }
                    //yield return new WaitUntil(()=>Input.GetKeyDown(KeyCode.Space));
                    yield return new WaitForEndOfFrame();
                }
                retriesWorld--;
            } while (worldFailed && retriesWorld > 0);

            placeProgress += 0.8f;
            if (worldFailed || retriesWorld == 0) {
                Debug.LogError("World did not generate correctly! -- Couldnt fit all of the island! ");
            }
            doneIslands = new List<IslandData>();
            foreach (Rect place in placeToIsland.Keys) {
                yield return new WaitForEndOfFrame();
                IslandData island = placeToIsland[place];
                Tile[] islandTiles = new Tile[island.Tiles.Length];
                for (int i = 0; i < island.Tiles.Length; i++) {
                    Tile t = island.Tiles[i];
                    Tile newTile = SetNewLandTileAt((int)(place.x + t.X), (int)(place.y + t.Y), t);
                    islandTiles[i] = newTile;
                    if (island.tileToStructure.ContainsKey(t)) {
                        //the tile has a structure associated
                        //need to update that reference to the new location of that tile
                        if (tileToStructure.ContainsKey(newTile))
                            Debug.LogError("Structure is being overwritten! " + newTile.Vector2 + ": " +
                                            tileToStructure[newTile] + " with " + island.tileToStructure[t]);
                        tileToStructure[newTile] = island.tileToStructure[t];
                    }
                }
                placeProgress += 0.2f / placeToIsland.Count;
                IslandData placedIslandStruct = new IslandData(island, islandTiles, place);
                doneIslands.Add(placedIslandStruct);
            }
            TileSpriteController.CreateIslandSprites(doneIslands);

            tilesPopulated = true;
            yield return null;
        }

        public static DirectionalRect GetNewRects(List<Rect> islands, Rect newRect, Rect Island, Direction direction) {
            DirectionalRect firstRect = new DirectionalRect {
                direction = direction,
                Island = Island,
                rect = newRect
            };
            if (islands.Count == 0)
                return firstRect;
            List<Rect> Overlaping = new List<Rect>();
            foreach (Rect isl in islands) {
                if (newRect.Overlaps(isl)) {
                    Overlaping.Add(isl);
                }
            }

            foreach (Rect over in Overlaping) {
                firstRect.UpdateRect(over);
            }

            return firstRect;
        }

        private void OnDrawGizmos() {
            if (recantgleEmptySpaces == null)
                return;
            float pos = 0.5f;
            foreach (DirectionalRect re in recantgleEmptySpaces) {
                if (rectToColor.ContainsKey(re) == false)
                    continue;
                Gizmos.color = rectToColor[re];
                //if (re.direction != Direction.N)
                //    continue;
                //if (Gizmos.color!=Color.white)
                //    continue;
                DrawRect(re.rect, pos);
                pos += 0.5f;
            }
            foreach (Rect re in placeToIsland.Keys) {
                Gizmos.color = Color.red;
                DrawRect(re, 0);
            }
            //Gizmos.color = Color.black;
            //DrawRect(ColdIslandSpace);
            //Gizmos.color = Color.gray;
            //DrawRect(MiddleIslandSpace);
            //Gizmos.color = Color.blue;
            //DrawRect(WarmIslandSpace);
        }

        private void DrawRect(Rect rect, float z) {
            Gizmos.DrawWireCube(new Vector3(rect.center.x, rect.center.y, z), new Vector3(rect.size.x, rect.size.y, 0.01f));
        }

        private Tile SetNewLandTileAt(int x, int y, Tile t) {
            LandTile tl = new LandTile(x, y, t);
            SetTileAt(x, y, tl);
            return tl;
        }

        public World GetWorld(bool isIslandEditor) {
            if (ReadyToFillWorld == false) {
                Debug.LogWarning("World wasnt ready when called!");
                return null;
            }
            world = new World(tiles, isIslandEditor);
            if (doneIslands != null) {
                world.CreateIslands(doneIslands);
            }
            return world;
        }

        public Tile[] GetTiles() {
            return tiles;
        }

        public void Destroy() {
            if (gameObject != null)
                Destroy(gameObject);
        }

        public void OnDestroy() {
            Instance = null;
        }

        public Tile GetTileAt(int x, int y) {
            if (x >= Width || y >= Height) {
                return null;
            }
            if (x < 0 || y < 0) {
                return null;
            }
            return tiles[x * Height + y];
        }

        public void SetTileAt(int x, int y, Tile t) {
            if (x >= Width || y >= Height) {
                return;
            }
            if (x < 0 || y < 0) {
                return;
            }
            tiles[x * Height + y] = t;
        }

        public List<Fertility> GetFertilitiesForClimate(Climate climate, int width, int height) {
            List<Fertility> fers = new List<Fertility>();
            if (PrototypController.Instance.GetFertilitiesForClimate(climate) == null) {
                Debug.LogError("NO fertility found for this climate " + climate);
                return null;
            }

            List<Fertility> climFer = new List<Fertility>(PrototypController.Instance.GetFertilitiesForClimate(climate));
            Range range = PrototypController.Instance.IslandSizeToGenerationInfo[Island.GetSizeTyp(width, height)].fertilityRange;
            int count = range.GetRandomCount(mapThreadRandom);
            Island.GetSizeTyp(width, height);
            for (int i = 0; i < count; i++) {
                if (climFer.Count == 0) {
                    Debug.LogWarning("NOT ENOUGH FERTILITIES FOR CLIMATE " + climate);
                    break;
                }
                Fertility f = climFer[mapThreadRandom.Range(0, climFer.Count)];
                climFer.Remove(f);
                fers.Add(f);
            }
            return fers;
        }

        public struct IslandGenInfo {
            public bool generate;
            public Range Width;
            public Range Height;
            public Climate climate;

            public IslandGenInfo(IslandGenInfo genInfo) : this() {
                this.Width = genInfo.Width;
                this.Height = genInfo.Height;
                this.climate = genInfo.climate;
            }

            public IslandGenInfo(Range Width, Range Height, Climate climate, bool generate) {
                this.Width = Width;
                this.Height = Height;
                this.climate = climate;
                this.generate = generate;
            }

            //Maybe add any SpecialFeature it may contain? eg vulkan,
        }

        public class IslandData {
            public string Name;
            public int Width;
            public int Height;
            public int X;
            public int Y;
            public Tile[] Tiles;
            public Climate Climate;
            public List<FertilityPrototypeData> fertilities = new List<FertilityPrototypeData>();
            public List<ResourceGenerationInfo> resources = new List<ResourceGenerationInfo>();
            public Dictionary<Tile, Structure> tileToStructure;
            public Dictionary<string, int> Resources = new Dictionary<string, int>();
            public Size Size;
            public int ResourcesCount;
            public int FertilityCount;
            internal List<ResourceGenerationInfo> ExcludedResources = new List<ResourceGenerationInfo>();
            public List<IslandFeature> Features;
            public bool NeedsFertility => FertilityCount > fertilities.Count;
            public bool NeedsResources => ResourcesCount > resources.Count;

            public IslandData(SaveIsland save) : this(save.Width, save.Height) {
                Width = save.Width;
                Height = save.Height;
                Tiles = save.tiles;
                this.Climate = save.climate;
                Name = save.Name;
                Resources = MapGenerator.Instance.GetResourcesFromRange(save.Resources);
                tileToStructure = new Dictionary<Tile, Structure>();
                Features = save.features;
                foreach (Structure str in save.structures) {
                    tileToStructure.Add(str.BuildTile, str);
                }
            }

            public IslandData(IslandData copy) : this(copy.Width, copy.Height) {
                Tiles = copy.Tiles;
                this.Climate = copy.Climate;
                tileToStructure = copy.tileToStructure;
                this.Name = copy.Name;
                this.fertilities = copy.fertilities;
                Resources = copy.Resources;
                Features = copy.Features;
            }

            public IslandData(IslandData copy, Tile[] islandTiles) : this(copy) {
                this.Tiles = islandTiles;
            }

            public IslandData(IslandData copy, Tile[] islandTiles, Rect place) : this(copy, islandTiles) {
                this.X = (int)place.x;
                this.Y = (int)place.y;
                foreach (IslandFeature f in Features) {
                    f.position += new Vector2(X, Y); //adjust for worldposition
                }
            }

            public IslandData(int Width, int Height) {
                this.Width = Width;
                this.Height = Height;
                Size = Island.GetSizeTyp(Width, Height);
                IslandSizeGenerationInfo info = PrototypController.Instance.IslandSizeToGenerationInfo[Size];
                ResourcesCount = info.resourceRange.GetRandomCount(MapGenerator.Instance.mapThreadRandom);
                FertilityCount = info.fertilityRange.GetRandomCount(MapGenerator.Instance.mapThreadRandom);
            }

            private IslandData() {
            }

            public IslandData(int width, int height, Tile[] tile, Climate climate, Dictionary<Tile, Structure> tileToStructure, List<IslandFeature> features) : this(width, height) {
                Width = width;
                Height = height;
                this.Tiles = tile;
                this.Climate = climate;
                this.tileToStructure = tileToStructure;
                ExcludedResources.AddRange(PrototypController.Instance.ClimateToResourceGeneration[climate].FindAll(x =>
                        x.requiredTile != null && Array.Exists(x.requiredTile, y => Array.Exists(Tiles, z => z.Type == y) == false
                )));
                this.Features = features;
            }

            public Vector2 GetPosition() {
                return new Vector2(X, Y);
            }

            internal void AddResources(ResourceGenerationInfo resourceGenerationInfo, ThreadRandom random) {
                if (resourceGenerationInfo == null)
                    return;
                int count = resourceGenerationInfo.resourceRange[Size].GetRandomCount(random);
                if(count <= 0) {
                    return;
                }
                resources.Add(resourceGenerationInfo);
                Resources[resourceGenerationInfo.ID] = count;
            }

            internal List<Fertility> GetFertilities() {
                List<Fertility> fer = new List<Fertility>();
                foreach (FertilityPrototypeData f in fertilities) {
                    fer.Add(new Fertility(f.ID, f));
                }
                return fer;
            }

            internal void AddFertility(FertilityPrototypeData fertilityPrototypeData) {
                if (fertilityPrototypeData == null)
                    return;
                fertilities.Add(fertilityPrototypeData);
            }

            internal void AddUnlimitedResources(ResourceGenerationInfo unlimited) {
                resources.Add(unlimited);
                Resources[unlimited.ID] = 9999;
            }
        }
    }
    public enum GenerationType { Random, Noise, GroupedNoise };
    public enum StructureType { Natural, Special, Building };

    public class IslandSizeGenerationInfo {
        public Range resourceRange;
        public Range fertilityRange;
        public List<ResourceGenerationInfo> resourceGenerationsInfo = new List<ResourceGenerationInfo>();
    }
    public class SpawnStructureGenerationInfo {
        public string ID;
        public Climate[] climate;
        public TileType[] requiredTile;
        public List<ResourceGenerationInfo> resourceGenerationsInfo = new List<ResourceGenerationInfo>();
        public float perTileChance = 0.01f;
        public float cubic2Fractal = 1.0045f;
        public float valueFractal = .53f;
        public GenerationType genType = GenerationType.Random;
        public StructureType structureType = StructureType.Natural;
        public bool islandUnique = false;
        public bool worldUnique = false;
        [Ignore] bool isInWorldPlaced;
        public bool IsWorldClaimed => isInWorldPlaced;
        public bool ClaimWorldUnique() {
            lock(this) {
                if (isInWorldPlaced)
                    return false;
                isInWorldPlaced = true;
                return true;
            }
        }
    }
    public class ResourceGenerationInfo : IWeighted {
        public string ID;
        public Climate[] climate;
        [XmlArray(ElementName = "Type")]
        public TileType[] requiredTile;
        public Dictionary<Size, Range> resourceRange;
        public float percentageOfIslands;
        private int generated;
        public bool unlimited;

        public float GetStartWeight() {
            return percentageOfIslands;
        }

        public float GetCurrentWeight(int maximumSelect) {
            return Mathf.Clamp(percentageOfIslands - generated / maximumSelect, 0.01f, 1);
        }

        public float Select(int maximumSelect) {
            float old = GetCurrentWeight(maximumSelect);
            generated++;
            return old - GetCurrentWeight(maximumSelect);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

public class MapGenerator : MonoBehaviour {
    public static MapGenerator Instance;

    bool startedGenerating = false;
    bool tilesPopulated = false;
    Tile[] tiles;
    List<IslandStruct> toPlaceIslands;
    List<IslandStruct> doneIslands;
    public bool RandomSeed;
    public int MapSeed;

    public int Width;
    public int Height;
    List<Task> generatorsTasks;

    int completedIslands = 0;
    Dictionary<Tile, Structure> tileToStructure;

    List<IslandGenerator> islandGenerators;
    ConcurrentBag<EditorController.SaveIsland> loadedIslandsList;
    float placeProgress;
    public float GeneratedProgressPercantage {
        get {
            //if (started == false && EditorController.IsEditor ==false)
            //    return 0;
            float percentage = 0;

            int islandamount = toLoadIslands + toGeneratorIslands;
            if(islandGenerators!=null) {
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
            
            if (EditorController.IsEditor==false) {
                percentage *= 0.9f;
                percentage += placeProgress * 0.1f;
            }
            return percentage;
        }
    }

    public bool IsDone {
        get {
            return tilesPopulated &&
              ReadyToPlace;
        }
    }
    bool ReadyToPlace {
        get {
            return started &&
              toGeneratorIslands + toLoadIslands > 0
              && completedIslands == toGeneratorIslands
              && loadedIslands == toLoadIslands;
        }
    }
    bool IsPlacing;
    int toGeneratorIslands = 0;
    int toLoadIslands = 0;
    int islandPlaceSeed = 0;
    int loadedIslands = 0;
    List<IslandGenInfo> islandsToGenerate;
    private Task loadTask;
    private World world;
    private bool started;
    Dictionary<KeyValuePair<Climate, Size>, List<string>> islands;
    private System.Diagnostics.Stopwatch stopwatch;

    public void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two MapController.");
        }
        Instance = this;
        this.gameObject.transform.parent = null;
        if(EditorController.generate) {
            EditorGenerate(EditorController.Width, EditorController.Height, EditorController.GetEditorGenInfo());
        }
        if(EditorController.IsEditor&&EditorController.generate == false) {
            Destroy();
        } 
        DontDestroyOnLoad(this.gameObject);
        
        toPlaceIslands = new List<IslandStruct>();
        tileToStructure = new Dictionary<Tile, Structure>();
        islands = SaveController.GetIslands();
        if (WorldController.Instance!=null && WorldController.Instance.World != null)
            Destroy();
    }

    internal Dictionary<Tile, Structure> GetStructures() {
        return tileToStructure;
    }

    public void DefineParameters(int seed, int height, int width, Dictionary<IslandGenInfo, Range> numberRangeOfIslandsSizes, List<string> hasToUseIslands, bool generatedIslands = false) {
        started = true;
        Random.InitState(seed);
        //THIS MUST BE THE FIRST RANDOM NUMBER!
        //TO make sure that there is no change of the placement 
        islandPlaceSeed = Random.Range(0, int.MaxValue);
        Width = width;
        Height = height;
        tiles = new Tile[Width * Height];
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                SetTileAt(x, y, new Tile(x, y));
            }
        }
        if (hasToUseIslands == null) {
            hasToUseIslands = new List<string>();
        }
        if (generatedIslands) {
            islandsToGenerate = new List<IslandGenInfo>();
            Debug.LogWarning("Has not been correctly implemented! TODO: Make it work! Otherwise => Do not use!");
            foreach (IslandGenInfo genInfo in numberRangeOfIslandsSizes.Keys) {
                Range range = numberRangeOfIslandsSizes[genInfo];
                int numberOfIslands = Random.Range(range.min, range.max+1);
                for (int i = 0; i < numberOfIslands; i++) {
                    islandsToGenerate.Add(new IslandGenInfo(genInfo)); // TODO: rethink this !
                }
            }
        }
        else if (numberRangeOfIslandsSizes != null) {
            foreach (IslandGenInfo genInfo in numberRangeOfIslandsSizes.Keys) {
                Range range = numberRangeOfIslandsSizes[genInfo];
                int numberOfIslands = Random.Range(range.min, range.max +1);
                for (int i = 0; i < numberOfIslands; i++) {
                    hasToUseIslands.Add(GetRandomIslandFileName(
                        Island.GetSizeTyp(genInfo.Width.Middle, genInfo.Height.Middle), genInfo.climate));
                }
            }
        }

        //Load the premade island if there are any 
        LoadIslands(hasToUseIslands);
        //THIS IS TEMPORARY -- replace with an better solution that doesnt require a secondary thing besides seed for a map
        GameDataHolder.Instance.usedIslands = hasToUseIslands.ToArray();
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
        return keyIslands[Random.Range(0, keyIslands.Count)];
    }

    public List<IslandStruct> GetIslandStructs() {
        return doneIslands;
    }

    private void LoadIslands(List<string> toLoad) {
        toLoadIslands = toLoad.Count;
        loadedIslandsList = new ConcurrentBag<EditorController.SaveIsland>();
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
        islandsToGenerate = new List<IslandGenInfo>(toGenerate);
        if(RandomSeed)
            Random.InitState(Random.Range(int.MinValue,int.MaxValue));
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
            IslandGenerator isg = new IslandGenerator(
                Random.Range(igi.Width.min, igi.Width.max),
                Random.Range(igi.Height.min, igi.Height.max),
                Random.Range(0, int.MaxValue), 6,
                igi.climate
            );
            islandGenerators.Add(isg);
            //for now we just put them at fix spots
        }

        generatorsTasks = new List<Task>();
        for (int i = 0; i < islandGenerators.Count; i++) {
            IslandGenerator isg = islandGenerators[i];
            Task t = Task.Factory.StartNew(() => {
                Debug.Log("Start");
                isg.Start();
                Debug.Log("Finished");
            });
            t.ConfigureAwait(false);
            generatorsTasks.Add(t);
            //			await t;
        }
        toGeneratorIslands = islandGenerators.Count;
    }

    public void Update() {
        if (startedGenerating == false) {
            return;
        }
        if(IsDone) {
            return;
        }
        if (generatorsTasks != null) {
            for (int i = 0; i < generatorsTasks.Count; i++) {
                Task t = generatorsTasks[i];
                if (t.IsCompleted && IsDone == false) {
                    Debug.Log("TASK Number " + i + " is finished! ");
                    if (t.IsFaulted) {
                        Debug.LogError("TASK Number " + i + " had an "+ t.Exception.InnerException.Message+ " exception \n" + t.Exception.InnerException.StackTrace);
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
                        toPlaceIslands.Add(gen.GetIslandStruct());
                    }
                }
                if (loadedIslandsList != null) {
                    foreach (EditorController.SaveIsland save in loadedIslandsList) {
                        toPlaceIslands.Add(new IslandStruct(save, GetFertilitiesForClimate(save.climate, 3/*TODO:make nonstatic*/)));
                    }
                }
                //now Place them at point 
                //FOR NOW this is being just random in the world
                //IN Future consider a more specialised way with the world being
                //divided into rectangles which do not contain any island 
                //then pick on of those in which the island fits than inthere random position
                if (EditorController.IsEditor == false) {
                    //we have multiple islands to place!
                    StartCoroutine(PlaceIslandOnMap(toPlaceIslands));
                }
                else {
                    //the islands is the whole map! so just set the tiles to the map tiles
                    tiles = islandGenerators[0].Tiles;
                    tilesPopulated = true;
                }
                if (tilesPopulated == false)
                    return;

            }

        }
        if(IsDone) {
            stopwatch.Stop();
            Debug.Log("Generated map with island number " + toPlaceIslands.Count + " in a Map " + Width + " : " + Height
                + " in " + stopwatch.ElapsedMilliseconds + "ms (" + stopwatch.Elapsed.TotalSeconds + "s)! ");

            if (IsDone && SaveController.IsLoadingSave == false && WorldController.Instance != null) {
                WorldController.Instance.SetGeneratedWorld(GetWorld(), tileToStructure);
                Destroy(); 
            }
            transform.SetParent(null);
        }
    }
    private IEnumerator PlaceIslandOnMap(List<IslandStruct> islandStructs) {
        placeProgress = 0;
        //Makes it easier to have the same placement of the islands
        Random.InitState(islandPlaceSeed);

        List<Rect> rectangleIslands = new List<Rect>();
        Dictionary<Rect, IslandStruct> placeToIsland = new Dictionary<Rect, IslandStruct>();
        int retriesWorld = 32;
        bool worldFailed = false;
        List<IslandStruct> toPlaceIslands = new List<IslandStruct>(islandStructs);
        //while it fails repeat it until it was "retriesWorld" times tried
        do {
            yield return new WaitForEndOfFrame();
            placeToIsland.Clear();
            rectangleIslands.Clear();
            //if we have to try again clear the done islands
            foreach (IslandStruct island in toPlaceIslands) {
                int islandTries = 0;
                bool failed = false;
                while (islandTries < 1024) {
                    int x = Random.Range(0, Width - island.Width);
                    int y = Random.Range(0, Height - island.Height);
                    islandTries++;
                    Rect toTest = new Rect(x, y, island.Width, island.Height);
                    foreach (Rect inWorld in rectangleIslands) {
                        if (toTest.Overlaps(inWorld)) {
                            failed = true;
                            break;
                        }
                    }
                    if (failed) {
                        continue;
                    }
                    rectangleIslands.Add(toTest);
                    placeToIsland.Add(toTest, island);
                    yield return new WaitForEndOfFrame();
                    //Debug.Log("Placing island on x " + x + "y " + y + " !");
                    break;
                }
                if (failed) {
                    worldFailed = true;
                    //Debug.Log("Placing island failed 1024 times!");
                    break;
                }
                else {
                    worldFailed = false;
                }
            }
            retriesWorld--;
        } while (worldFailed && retriesWorld > 0);
        placeProgress += 0.8f;
        if (worldFailed) {
            Debug.Log("World did not generate correctly! -- Couldnt fit all of the island! ");
        }
        doneIslands = new List<IslandStruct>();
        foreach (Rect place in placeToIsland.Keys) {
            yield return new WaitForEndOfFrame();
            IslandStruct island = placeToIsland[place];
            Tile[] islandTiles = new Tile[island.Tiles.Length];
            for (int i = 0; i < island.Tiles.Length; i++) {
                Tile t = island.Tiles[i];
                Tile newTile = SetNewLandTileAt((int)(place.x + t.X), (int)(place.y + t.Y), t);
                islandTiles[i] = newTile;
                if (island.tileToStructure.ContainsKey(t)) {
                    //the tile has a structure associated 
                    //need to update that reference to the new location of that tile
                    tileToStructure.Add(newTile, island.tileToStructure[t]);
                }
            }
            placeProgress += 0.2f / placeToIsland.Count;
            IslandStruct placedIslandStruct = new IslandStruct(island, islandTiles, place);
            doneIslands.Add(placedIslandStruct);
        }
        TileSpriteController.CreateIslandSprites(doneIslands);

        tilesPopulated = true;
        yield return null;
    }

    private Tile SetNewLandTileAt(int x, int y, Tile t) {
        LandTile tl = new LandTile(x, y, t);
        SetTileAt(x, y, tl);
        return tl;
    }

    /// <summary>
    /// WARNING Destroys this GameObject!
    /// </summary>
    /// <returns></returns>    
    public World GetWorld() {
        if (IsDone == false) {
            Debug.LogWarning("World wasnt ready when called!");
            return null;
        }
        world = new World(tiles, Width, Height);
        foreach (IslandStruct island in doneIslands) {
            world.CreateIsland(island);
        }
        return world;
    }


    public Tile[] GetTiles() {
        return tiles;
    }
    public void Destroy() {
        if (gameObject!=null)
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

    public List<Fertility> GetFertilitiesForClimate(Climate climate, int count) {
        List<Fertility> fers = new List<Fertility>();
        if (PrototypController.Instance.GetFertilitiesForClimate(climate) == null) {
            Debug.LogError("NO fertility found for this climate " + climate);
            return null;
        }
        List<Fertility> climFer = new List<Fertility>(PrototypController.Instance.GetFertilitiesForClimate(climate));

        for (int i = 0; i < count; i++) {
            if (climFer.Count == 0) {
                Debug.LogWarning("NOT ENOUGH FERTILITIES FOR CLIMATE " + climate);
                break;
            }
            Fertility f = climFer[UnityEngine.Random.Range(0, climFer.Count)];
            climFer.Remove(f);
            fers.Add(f);
        }
        return fers;
    }

    public struct IslandGenInfo {
        public Range Width;
        public Range Height;
        public Climate climate;

        public IslandGenInfo(IslandGenInfo genInfo) : this() {
            this.Width = genInfo.Width;
            this.Height = genInfo.Height;
            this.climate = genInfo.climate;
        }
        public IslandGenInfo(Range Width, Range Height, Climate climate) {
            this.Width = Width;
            this.Height = Height;
            this.climate = climate;
        }
        //Maybe add any SpecialFeature it may contain? eg vulkan, 
    }
    public struct IslandStruct {
        public string name;
        public int Width;
        public int Height;
        public int x;
        public int y;
        public Tile[] Tiles;
        public Climate climate;
        public List<Fertility> fertilities;
        public Dictionary<Tile, Structure> tileToStructure;
        public Dictionary<string, int> Ressources;

        public IslandStruct(int width, int height, Tile[] tiles, Climate climate, List<Fertility> list, Dictionary<int, int> Ressources) : this() {
            Width = width;
            Height = height;
            Tiles = tiles;
            this.climate = climate;
            tileToStructure = new Dictionary<Tile, Structure>();
            fertilities = list;
            this.name = "";

        }
        public IslandStruct(EditorController.SaveIsland save, List<Fertility> fertilities) : this() {
            Width = save.Width;
            Height = save.Height;
            Tiles = save.tiles;
            this.climate = save.climate;
            Ressources = new Dictionary<string, int>();
            if (save.Ressources != null) {
                foreach (string id in save.Ressources.Keys) {
                    Ressources[id] = Random.Range(save.Ressources[id][0], save.Ressources[id][1] + 1);
                }
            }
            name = save.Name;
            this.fertilities = fertilities;
            tileToStructure = new Dictionary<Tile, Structure>();
            foreach (Structure str in save.structures) {
                tileToStructure.Add(str.BuildTile, str);
            }
        }
        public IslandStruct(IslandStruct copy) : this() {
            Width = copy.Width;
            Height = copy.Height;
            Tiles = copy.Tiles;
            this.climate = copy.climate;
            tileToStructure = copy.tileToStructure;
            this.name = copy.name;
            this.fertilities = copy.fertilities;
            Ressources = copy.Ressources;
            //x = copy.x;
            //y = copy.y;

        }

        public IslandStruct(IslandStruct copy, Tile[] islandTiles) : this(copy) {
            this.Tiles = islandTiles;
        }

        public IslandStruct(IslandStruct copy, Tile[] islandTiles, Rect place) : this(copy, islandTiles) {
            this.x = (int)place.x;
            this.y = (int)place.y;
        }
        public Vector2 GetPosition() {
            return new Vector2(x, y);
        }
    }
    public struct Range {
        public int min;
        public int max;
        public int Middle => (min + max) / 2;

        public Range(int min, int max) {
            this.min = min;
            this.max = max;
        }
        /// <summary>
        /// Returns true if the value is bigger/equal than min
        /// AND smaller(!) than max!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsBetween(int value) {
            return value >= min && value < max;
        }

    }
}

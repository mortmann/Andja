using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

public class MapGenerator : MonoBehaviour {
	public static MapGenerator Instance;

	bool tilesPopulated = false;
	Tile[] tiles;
	public int Width;
	public int Height; 
	List<Task> generatorsTasks;
    List<IslandStruct> toPlaceIslands;

    int completedIslands =0;
	List<IslandGenerator> islandGenerators;
    ConcurrentBag<EditorController.SaveIsland> loadedIslands;
    public float PercantageProgress {
		get { return 100* ((float)completedIslands / toGeneratorIslands); }
	}
	public bool IsDone {
		get { return completedIslands == toGeneratorIslands; }
	}
	int toGeneratorIslands = 0;

	List<IslandGenInfo> islandsToGenerate;

	public void Start(){
		if (Instance != null) {
			Debug.LogError ("There should never be two MapController.");
		}
		Instance = this;
		if(EditorController.IsEditor == false){
			this.Width = GameDataHolder.Instance.width;
			this.Height = GameDataHolder.Instance.height;
		}
		this.gameObject.transform.parent = null;
		DontDestroyOnLoad (this.gameObject);
        toPlaceIslands = new List<IslandStruct>();

    }

    public void DefineParameters(int seed, int height, int width, Dictionary<IslandGenInfo, Range> rangeOfIslandsSizes, List<string> hasToUseIslands, bool generatedIslands = false ) {
        Random.InitState(seed);
        Width = width;
        Height = height;
        if (generatedIslands) {
            islandsToGenerate = new List<IslandGenInfo>();
            Debug.LogWarning("Has not been correctly implemented! TODO: Make it work! Otherwise => Do not use!");
            foreach(IslandGenInfo genInfo in rangeOfIslandsSizes.Keys) {
                Range range = rangeOfIslandsSizes[genInfo];
                int numberOfIslands = Random.Range(range.min, range.max);
                for (int i = 0; i < numberOfIslands; i++) {
                    islandsToGenerate.Add(new IslandGenInfo(genInfo)); // TODO: rethink this !
                }
            }
        } else {
            foreach (IslandGenInfo genInfo in rangeOfIslandsSizes.Keys) {
                Range range = rangeOfIslandsSizes[genInfo];
                int numberOfIslands = Random.Range(range.min, range.max);
                for (int i = 0; i < numberOfIslands; i++) {
                    hasToUseIslands.Add(GetRandomIslandFilePath(
                        Island.GetSizeTyp(genInfo.Width.Middle, genInfo.Height.Middle), genInfo.climate));
                }                
            }
        }
        LoadIslands(hasToUseIslands);
        
    }
    private string GetRandomIslandFilePath(IslandSizeTypes size, Climate climate) {
        string path = EditorController.GetPathToIsland(size, climate);
        DirectoryInfo dir = new DirectoryInfo(path);
        FileInfo[] info = dir.GetFiles("*.isl");
        FileInfo file = info[Random.Range(0, info.Length)]; // 
        path = Path.Combine(path, file.Name);
        return path;
    }
    private void LoadIslands(List<string> toLoad) {
        //create as thread to be safe this isnt going to slow down 
        Task t = Task.Factory.StartNew(() => {
            List<EditorController.SaveIsland> loaded = new List<EditorController.SaveIsland>();
            foreach(string location in toLoad) {
                loaded.Add(EditorController.LoadIsland(location));
            }
        });
        t.ConfigureAwait(false);
    }

    private void SetToGenerate( IEnumerable<IslandGenInfo> toGenerate){
		islandsToGenerate = new List<IslandGenInfo> (toGenerate);
	}
    public void EditorGenerate(int height, int width, params IslandGenInfo[] toGenerate){
		Width = width;
		Height = height;
		islandsToGenerate = new List<IslandGenInfo> (toGenerate);
	}

	public void Generate(){
		// Uncomment this to generate the same "random" terrain every time.
//		Random.InitState(0);
		tiles = new Tile[Width*Height];

		int islandCount = islandsToGenerate.Count;
		islandGenerators = new List<IslandGenerator> ();
		for (int i = 0; i < islandCount; i++) {
			IslandGenInfo igi = islandsToGenerate [i];
			IslandGenerator isg = new IslandGenerator (
                Random.Range(igi.Width.min,igi.Width.max), 
                Random.Range(igi.Height.min, igi.Height.max), 
                Random.Range(0,int.MaxValue), 6,
                Climate.Middle // TODO: feed this something real *nomnomnom*
            );
			islandGenerators.Add (isg);
			//for now we just put them at fix spots
		}

		generatorsTasks = new List<Task> ();
		for (int i = 0; i < islandGenerators.Count; i++) {
			IslandGenerator isg = islandGenerators [i];
			Task t = Task.Factory.StartNew (() => {
				Debug.Log ("Start");
				isg.Start ();
				Debug.Log ("Finished");
			});
			t.ConfigureAwait(false);

			generatorsTasks.Add (t);
//			await t;
			Debug.Log ("started");

		}
		toGeneratorIslands = islandGenerators.Count;
	}

	public void Update(){
		for (int i = 0; i < generatorsTasks.Count; i++) {
			Task t = generatorsTasks [i]; 
			if (t.IsCompleted && IsDone == false) {
				Debug.Log ("TASK Number " + i + " is finished! ");
				if( t.IsFaulted){
					Debug.LogError ("TASK Number " + i + " had an exception: " + t.Exception.ToString());
				}
				completedIslands++;
				generatorsTasks.Remove (t);

            }
		}
		if(IsDone&&tilesPopulated==false){
            //if any island has been generated add them
            foreach(IslandGenerator gen in islandGenerators) {
                toPlaceIslands.Add(gen.GetIslandStruct());
            }
            //now Place them at point 
            //FOR NOW this is being just random in the world
            //IN Future consider a more specialised way with the world being
            //divided into rectangles which do not contain any island 
            //then pick on of those in which the island fits than inthere random position
            PlaceIslandOnMap(toPlaceIslands);

			for (int i = 0; i < tiles.Length; i++) {
				if(tiles [i]==null)
					tiles [i] = new Tile();
			}
			tilesPopulated = true;
		}
	}

    private void PlaceIslandOnMap(List<IslandStruct> toPlaceIslands) {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        List<Rect> rectangleIslands = new List<Rect>();
        foreach(IslandStruct island in toPlaceIslands) {
            bool failed = false;
            int tries = 0;
            while (tries < 1024) {
                int x = 0;
                int y = 0;
                tries++;
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

                break;
            }
            if (failed) {
                Debug.Log("Placing island failed 1024 times!");
            } 
        }
        
        sw.Stop();
        Debug.Log("Generated map with island number " + toPlaceIslands.Count + " in a Map" + Width + " : " + Height + " in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)! ");
    }

    public Tile[] GetTiles(){
		Destroy (this.gameObject);
		return tiles;
	}


	public void SetTileAt(int x,int y,Tile t){
		if (x >= Width ||y >= Height ) {
			return;
		}
		if (x < 0 || y < 0) {
			return;
		}
		if(t.Type != TileType.Ocean){
			tiles[x * Height + y] = new LandTile(x,y,t);
			return;
		} else
			tiles[x * Height + y] = new Tile(x,y);
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
        public IslandGenInfo(Range Width, Range Height , Climate climate ){
            this.Width = Width;
            this.Height = Height;
            this.climate = climate;
		}


		//Maybe add any SpecialFeature it may contain? eg vulkan, 
	}
    public struct IslandStruct {
        public int Width;
        public int Height;
        public int x;
        public int y;
        public Tile[] Tiles;
        public Climate climate;
        public Dictionary<Tile, Structure> tileToStructure;

        public IslandStruct(int width, int height, Tile[] tiles, Climate climate) : this() {
            Width = width;
            Height = height;
            Tiles = tiles;
            this.climate = climate;
            tileToStructure = new Dictionary<Tile, Structure>();
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

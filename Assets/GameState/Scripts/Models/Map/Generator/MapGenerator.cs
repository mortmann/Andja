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
	int completedIslands=0;
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
                Random.Range(0,int.MaxValue), 6
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
			for (int i = 0; i < islandGenerators.Count; i++) {
				IslandGenerator isg = islandGenerators [i];
				int x = 0;
				int y = 0;
				//TODO this needs to be random and be climate based
				if(toGeneratorIslands>1){
					x = (i + 1) * (Width / 2) - Width / 4;
					y = Height / 2 - Height / 4;
				}
				foreach (Tile t in isg.Tiles) {
					SetTileAt (x + t.X, y + t.Y, t);
				}
			}




			for (int i = 0; i < tiles.Length; i++) {
				if(tiles [i]==null)
					tiles [i] = new Tile();
			}
			tilesPopulated = true;
		}
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

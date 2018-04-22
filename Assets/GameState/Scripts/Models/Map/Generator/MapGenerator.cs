using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class MapGenerator : MonoBehaviour {
	public static MapGenerator Instance;

	bool tilesPopulated = false;
	Tile[] tiles;
	public int Width;
	public int Height; 
	List<Task> generatorsTasks;
	int completedIslands=0;
	List<IslandGenerator> generators;
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

	public void SetToGenerate(int height, int width, IEnumerable<IslandGenInfo> toGenerate){
		Width = width;
		Height = height;
		islandsToGenerate = new List<IslandGenInfo> (toGenerate);
	}
	public void SetToGenerate(int height, int width, params IslandGenInfo[] toGenerate){
		Width = width;
		Height = height;
		islandsToGenerate = new List<IslandGenInfo> (toGenerate);
	}

	public void Generate(){
		// Uncomment this to generate the same "random" terrain every time.
//		Random.InitState(0);
		tiles = new Tile[Width*Height];

		int islandCount = islandsToGenerate.Count;
		generators = new List<IslandGenerator> ();
		for (int i = 0; i < islandCount; i++) {
			IslandGenInfo igi = islandsToGenerate [i];
			IslandGenerator isg = new IslandGenerator (Random.Range(igi.minWidth,igi.maxWidth), Random.Range(igi.minHeight,igi.maxHeight), Random.Range(0,int.MaxValue), 6);
			generators.Add (isg);
			//for now we just put them at fix spots
		}

		generatorsTasks = new List<Task> ();
		for (int i = 0; i < generators.Count; i++) {
			IslandGenerator isg = generators [i];
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
		toGeneratorIslands = generators.Count;
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
			for (int i = 0; i < generators.Count; i++) {
				IslandGenerator isg = generators [i];
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
		public int minWidth;
		public int maxWidth;
		public int minHeight;
		public int maxHeight;
		public Climate climate;
		public IslandGenInfo(int minWidth,int maxWidth,int minHeight,int maxHeight,Climate climate ){
			this.minWidth=minWidth;
			this.maxWidth = maxWidth;
			this.minHeight = minHeight;
			this.maxHeight = maxHeight;
			this.climate = climate;
		}


		//Maybe add any SpecialFeature it may contain? eg vulkan, 
	}
}

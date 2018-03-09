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
	public float percantageProgress {
		get { return 100* ((float)completedIslands / toGeneratorIslands); }
	}
	public bool isDone {
		get { return completedIslands == toGeneratorIslands; }
	}
	int toGeneratorIslands = 0;
	public void Start(){
		if (Instance != null) {
			Debug.LogError ("There should never be two SaveController.");
		}
		Instance = this;
		this.Width = GameDataHolder.Instance.width;
		this.Height = GameDataHolder.Instance.height;
		DontDestroyOnLoad (this);
		tiles = new Tile[Width*Height];

		Generation ();


	}
	public void Generation(){
		// Uncomment this to generate the same "random" terrain every time.
		Random.InitState(0);

		int islandCount = 2;
		generators = new List<IslandGenerator> ();
		for (int i = 0; i < islandCount; i++) {
			IslandGenerator isg = new IslandGenerator (Random.Range(150,150), Random.Range(150,150), Random.Range(0,int.MaxValue), 6);
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
		Debug.Log (tiles.Length);
	}

	public void Update(){
		for (int i = 0; i < generatorsTasks.Count; i++) {
			Task t = generatorsTasks [i]; 

			if (t.IsCompleted && isDone == false) {
				Debug.Log ("TASK Number " + i + " is finished! ");
				if( t.IsFaulted){
					Debug.LogError ("TASK Number " + i + " had an exception: " + t.Exception.ToString());
				}
				completedIslands++;
				generatorsTasks.Remove (t);
			}
		}
		if(isDone&&tilesPopulated==false){
			for (int i = 0; i < generators.Count; i++) {
				IslandGenerator isg = generators [i];
				int x = (i + 1) * (Width / 2) - Width / 4;
				int y = Height / 2 - Height / 4;
				foreach (Tile t in isg.tiles) {
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
		
}

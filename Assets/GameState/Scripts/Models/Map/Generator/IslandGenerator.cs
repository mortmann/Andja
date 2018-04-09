using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandGenerator {

	static readonly float shoreElevation = 0.29f;
	static readonly float cliffElevation = 0.37f;
	static readonly float dirtElevation = 0.43f;
	static readonly float mountainElevation = 1.15f;
	static readonly float landThreshold = cliffElevation;
	static readonly float islandThreshold = shoreElevation;
	ThreadRandom random;
	int seed;
	public int Width;
	public int Height;
	public Tile[] Tiles { get; protected set; }

	// Use this for initialization
	public IslandGenerator(int Width, int Height, int seed, int splats) {
		this.Width = Width;
		this.Height = Height;
		this.seed = seed;
		random = new ThreadRandom (seed);
//		Start ();
	}
	public void Start(){
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch ();
		sw.Start ();
		SetupTile ();
		// Make some kind of raised area
		int numSplats = random.Range(6, 12);
		int size = Mathf.Min (Height, Width);
		for (int i = 0; i < numSplats; i++){
			int range = random.Range(size/10, size/5);
			int x = random.Range(range, Width - 2*range);
			int y = random.Range(range, Height - 2*range );

			float centerHeight = (float)(Height - y + Width - x) /(float)(Height+Width);
			ElevateCircleArea(x, y, range,centerHeight*0.1f);
		}


		int numOfSquares = random.Range (2, 6);
		for (int i = 0; i < numOfSquares; i++) {
			float maxXRange = ((Width) / (1.75f * numOfSquares));
			float minXRange = ((Width) / (3f * numOfSquares));
			float maxYRange = ((Height) / (1.75f * numOfSquares));
			float minYRange = ((Height) / (3f * numOfSquares));

			int rangeX = Mathf.RoundToInt(random.RangeFloat(minXRange,maxXRange));			
			int rangeY = Mathf.RoundToInt(random.RangeFloat(minYRange,maxYRange));			

			int cx = random.Range(rangeX, Width - rangeX);
			int cy = random.Range(rangeY, Height - rangeY );

			Rect rect = new Rect (cx, cy, rangeX, rangeY);

			for (int x = Mathf.RoundToInt(rect.xMin); x < Mathf.RoundToInt(rect.xMax); x++) {
				for (int y = Mathf.RoundToInt(rect.yMin); y < Mathf.RoundToInt(rect.yMax); y++) {
					Tile t = GetTileAt (x, y);		
					if(t==null){
						continue;
					}
					t.Elevation += GetSquareElevation (t, new Vector2 (cx, cy), new Vector2(rangeX,rangeY)) * random.RangeFloat (0.09f, 0.11f);
				}
			}

		}

		//		GetTileAt (0, 0).Elevation = .5f;
		FastNoise cubicfractal = new FastNoise (random.Range(0,int.MaxValue));
		cubicfractal.SetFractalGain (0.3f);
		cubicfractal.SetFractalOctaves (5);
		cubicfractal.SetFractalLacunarity (2f);
		cubicfractal.SetFrequency (0.2f);
		cubicfractal.SetNoiseType (FastNoise.NoiseType.CubicFractal);
		cubicfractal.SetFractalType (FastNoise.FractalType.FBM);

		foreach(Tile t in Tiles){
			t.Elevation += GetSquareElevation (t, new Vector2 (Width, Height), new Vector2 (Width / 2, Height / 2));// * random.RangeFloat (0.09f, 0.11f);
			t.Elevation += GetOvalDistanceToCenter (t) * 1f;
			t.Elevation += cubicfractal.GetCubicFractal (t.X,t.Y);
		}
//		for (int i = 0; i < 5; i++) {
//			//make the it more even spread
//			foreach(Tile t in tiles){
//				Tile[] neigh = GetNeighbours (t);
//				float height = 0;
//				foreach(Tile nt in neigh){
//					if(nt!=null)
//						height += nt.Elevation;
//				}
//				t.Elevation += height / neigh.Length;
//				t.Elevation /= 2;
//			}
//		}

		// Make some kind of raised area
		for (int i = 0; i < numSplats; i++){
			int range = random.Range(size/20, size/10);
			int x = random.Range(range, Width - 2*range);
			int y = random.Range(range, Height - 2*range );

			float centerHeight = (float)(Height - y + Width - x) /(float)(Height+Width);
			ElevateCircleArea(x, y, range,centerHeight*0.8f,true);
		}

		for (int i = 0; i < 1; i++) {
			//make the it more even spread
			foreach(Tile t in Tiles){
				Tile[] neigh = GetNeighbours (t);
				float height = 0;
				foreach(Tile nt in neigh){
					if(nt!=null)
						height += nt.Elevation;
				}
				t.Elevation += height / neigh.Length;
				t.Elevation /= 2;
			}
		}		

//		FastNoise fn = new FastNoise (random.Range(0,int.MaxValue));
//		fn.SetFractalGain (0.3f);
//		fn.SetFractalOctaves (5);
//		fn.SetFractalLacunarity (2f);
//		fn.SetFrequency (0.05f);
//		fn.SetNoiseType (FastNoise.NoiseType.Simplex);

//		foreach(Tile t in )){
//			if(t.Elevation>cliffElevation){
//				continue;
//			}
//			t.Elevation -= fn.GetValue (t.X, t.Y) * 0.09f;
//		}

		Debug.Log ("FloodFillLands");
		HashSet<Tile> island = FloodFillLands();
		List<Tile> all = new List<Tile>(Tiles);
		all.ForEach(x=>{
				if(island.Contains(x) == false){
					x.Elevation=0f;
				}
			}
		);
		Debug.Log ("FloodFillOcean");
		FloodFillOcean (island);

		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				Tile t = GetTileAt (x, y);
				if(t.Elevation > mountainElevation){
					t.Type = TileType.Mountain;
				}
				else if(t.Elevation > dirtElevation){
					t.Type = TileType.Dirt;
				}
				else if(t.Elevation > cliffElevation){
					t.Type = TileType.Dirt;
					foreach(Tile nt in GetNeighbours(t)){
						if(nt == null){
							continue;
						}
						if(nt.Elevation<islandThreshold){
//							t.Type = TileType.Cliff;
							break;
						}
					}
				} 
				//else if(t.Elevation > shoreElevation){
				//	t.Type = TileType.Shore;
				//} 
			}
		}
		sw.Stop ();
		Debug.Log ("Generated island with size " + Width +":"+ Height +" in " + sw.ElapsedMilliseconds +"ms (" + sw.Elapsed.TotalSeconds +"s)! ");
	}
	//Returns biggest land mass as list
	protected HashSet<Tile> FloodFillLands() {
		List<Tile> allTiles = new List<Tile> (Tiles);
		HashSet<Tile> currIslandTiles = new HashSet<Tile> ();
		allTiles.RemoveAll (t => t.Elevation < islandThreshold);
		while(allTiles.Count>currIslandTiles.Count){
			Tile tile = allTiles[0];
			allTiles.RemoveAt (0);
			Queue<Tile> tilesToCheck = new Queue<Tile>();
			HashSet<Tile> islandTiles = new HashSet<Tile> ();
			tilesToCheck.Enqueue(tile);
			while (tilesToCheck.Count > 0) {
				Tile t = tilesToCheck.Dequeue();
				if(t==null||islandTiles.Contains(t)||t.Elevation<islandThreshold){
					continue;
				}
				if(t.Elevation<landThreshold&&HasNeighbourLand(t)==false){
					continue;
				}
				islandTiles.Add (t);
				allTiles.Remove (t);
				Tile[] ns = GetNeighbours(t);
				foreach (Tile t2 in ns) {
					if(t2 !=null)
						tilesToCheck.Enqueue(t2);
				}
			}
			if(currIslandTiles.Count<islandTiles.Count){
				currIslandTiles = islandTiles;
			}
		}
		return currIslandTiles;
	}
	protected HashSet<Tile> FloodFillOcean(HashSet<Tile> island) {
		HashSet<Tile> ocean = new HashSet<Tile> ();
		Queue<Tile> tilesToCheck = new Queue<Tile>();
		tilesToCheck.Enqueue(Tiles[0]);
		while (tilesToCheck.Count > 0) {
			Tile t = tilesToCheck.Dequeue ();
			if(t==null||ocean.Contains(t)||island.Contains(t)){
				continue;
			}
			Tile[] neighbours = GetNeighbours(t);
			ocean.Add (t);
			foreach(Tile neigh in neighbours){
				tilesToCheck.Enqueue (neigh);
			}
		}
		return ocean;
	}



	bool RandomShore(float x,float maxX){
		float multi = 1 / (maxX);
		float hasToBeUnder = Mathf.Pow ((multi * x), 3) - 2/maxX;
		float rand = random.RangeFloat (0f, 2f);
		return rand < hasToBeUnder;
	}

	protected HashSet<Tile> IslandFloodFill(Tile tile) {
		if (tile == null) {
			// We are trying to flood fill off the map, so just return
			// without doing anything.
			return null;
		}
		if(tile.Elevation<islandThreshold){
			while(tile.Elevation<islandThreshold){
				Tile[] ns = GetNeighbours(tile);
				foreach(Tile t in ns){
					if(t.Elevation>tile.Elevation){
						tile = t;
					}
				}
			}
		}
		HashSet<Tile> allTiles = new HashSet<Tile> (Tiles);
		HashSet<Tile> islandTiles = new HashSet<Tile> ();
		Queue<Tile> tilesToCheck = new Queue<Tile>();
		tilesToCheck.Enqueue(tile);
		Debug.Log ((Width*Height)/4);

		while (tilesToCheck.Count > 0) {
			Tile t = tilesToCheck.Dequeue();
			if(t==null||islandTiles.Contains(t)){
				continue;
			}
			Debug.Log (islandTiles.Count<(Width*Height)/4);
			if (t.Elevation>islandThreshold||tilesToCheck.Count<2 && islandTiles.Count<(Width*Height)/8) {
				islandTiles.Add (t);
				allTiles.Remove (t);
				Tile[] ns = GetNeighbours(t);
				foreach (Tile t2 in ns) {
					tilesToCheck.Enqueue(t2);
				}
			} else {
				if(t.Elevation<islandThreshold&&tilesToCheck.Count<2)
					Debug.Log (tilesToCheck.Count + "<2 " +  islandTiles.Count + "<" + (Width*Height)/4);
			}
		}
		return islandTiles;
	}


	public Tile[] GetNeighbours(Tile t, bool diagOkay = false) {
		Tile[] ns = new Tile[4];
		if(diagOkay==true)
			ns = new Tile[8];
		Tile n;
		n = GetTileAt(t.X, t.Y + 1);
		//NORTH
		ns[0] = n;  // Could be null, but that's okay.
		//WEST
		n = GetTileAt(t.X + 1, t.Y);
		ns[1] = n;  // Could be null, but that's okay.
		//SOUTH
		n = GetTileAt(t.X, t.Y - 1);
		ns[2] = n;  // Could be null, but that's okay.
		//EAST
		n = GetTileAt(t.X - 1, t.Y);
		ns[3] = n;  // Could be null, but that's okay.

		if (diagOkay == true) {
			n = GetTileAt(t.X + 1, t.Y + 1);
			ns[4] = n;  // Could be null, but that's okay.
			n = GetTileAt(t.X + 1, t.Y - 1);
			ns[5] = n;  // Could be null, but that's okay.
			n = GetTileAt(t.X - 1, t.Y - 1);
			ns[6] = n;  // Could be null, but that's okay.
			n = GetTileAt(t.X - 1, t.Y + 1);
			ns[7] = n;  // Could be null, but that's okay.
		}

		return ns;
	}
	public bool HasNeighbourLand(Tile t) {
		foreach(Tile tile in GetNeighbours(t)){
			if(tile.Elevation>landThreshold){
				return true;
			}
		}
		return false;
	}
	public void SetupTile(){
		Tiles = new Tile[Width*Height];
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				SetTileAt (x, y, new Tile (x,y));
			}
		}		
	}
	public float GetWeightedYFromRandomX(float size,float dist, float randomX) {
		float x1 = 0f; 
		float y1 = 0f; 
		float x2 = dist;
		float y2 = size; 
		float x3 = dist * 2; 
		float y3 = 0f;
		float denom = (x1 - x2) * (x1 - x3) * (x2 - x3);
		float A     = (x3 * (y2 - y1) + x2 * (y1 - y3) + x1 * (y3 - y2)) / denom;
		float B     = (x3*x3 * (y1 - y2) + x2*x2 * (y3 - y1) + x1*x1 * (y2 - y3)) / denom;
		float C     = (x2 * x3 * (x2 - x3) * y1 + x3 * x1 * (x3 - x1) * y2 + x1 * x2 * (x1 - x2) * y3) / denom;
		//		Debug.Log (A+"x^2 + "+ B +"x +"+C);
		return A * Mathf.Pow (randomX, 2) + B * randomX + C;
	}
	public float GetSquareElevation( Tile tile , Vector2 center, Vector2 dimension) {
//		float centerX = (float) Width / 2 - Random.Range(-5f,5f);
//		float centerY = (float) Height / 2 - Random.Range(-5f,5f);
		float[] distances = new float[]{ 
			(float)tile.Y / center.y,(float)(dimension.x - tile.Y) / center.y,
			(float)tile.X / center.x, (float)(dimension.y - tile.X) / center.x
		};
		float min = float.MaxValue;
		foreach( var val in distances ) {
			if( val < min ) {
				min = val;
			}
		}
		return min;
	}

	public float GetOvalDistanceToCenter( Tile tile ) {
		float centerX =(float) (Width / 2)- random.RangeFloat(-5f,5f);
		float centerY =(float) (Height / 2) - random.RangeFloat(-5f,5f);
		if(Mathf.Pow(tile.X-centerX,2) / Mathf.Pow(Width,2) + Mathf.Pow(tile.Y-centerY,2) / Mathf.Pow(Height,2) > 1){
			return 0;
		}
		float dist = (float) Mathf.Abs(centerX - tile.X) / centerX + Mathf.Abs(centerY - tile.Y) / centerY ;
		return 1-dist;
	}
		
	public void SetTileAt(int x,int y,Tile t){
		if (x >= Width ||y >= Height ) {
			return;
		}
		if (x < 0 || y < 0) {
			return;
		}
		Tiles[x * Height + y] = t;
	}
	public Tile GetTileAt(int x,int y){
		if (x >= Width ||y >= Height ) {
			return null;
		}
		if (x < 0 || y < 0) {
			return null;
		}
		return Tiles[x * Height + y];
	}

	void ElevateCircleArea(int q, int r, int range, float centerHeight = .8f, bool hastobeland = false){
		Tile centerHex = GetTileAt(q, r);

		Tile[] areaHexes = GetTilesWithinRangeOf(centerHex, range);

		foreach(Tile h in areaHexes) {
			//if(h.Elevation < 0)
			//h.Elevation = 0;
			if(h.Elevation < dirtElevation && hastobeland == true){
				continue;
			}
			h.Elevation += centerHeight * Mathf.Lerp( 1f, 0.25f, Mathf.Pow( centerHex.DistanceFromVector(h.Vector) / range,2f) );
		}
	}
	Tile[] GetTilesWithinRangeOf(Tile center, float range){
		List<Tile> tiles = new List<Tile> ();
		for( float x = center.X - range; x <= center.X+range; x++ ){
			for( float y = center.Y - range; y <= center.Y+range; y++ ){
				tiles.Add (GetTileAt (Mathf.RoundToInt (x), Mathf.RoundToInt (y)));
			}		
		}
		return tiles.ToArray ();
	}


}

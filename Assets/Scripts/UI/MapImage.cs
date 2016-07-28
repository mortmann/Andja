using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapImage : MonoBehaviour {

	Image image;
	Texture2D tex;
	// Use this for initialization
	void OnEnable () {
		tex = null;
	}
	
	// Update is called once per frame
	void Update () {
		if(tex!=null){
			//if something changes reset it 

			return;
		}
		image = GetComponent<Image> ();
		World w = World.current;
		tex = new Texture2D (w.Width, w.Height);
		Color[] p=tex.GetPixels ();
		int pixel=0;
		foreach (Tile item in w.tiles) {
			if (item.Type == TileType.Water) {
				p [pixel] = Color.blue;			
			} else {
				p [pixel] = Color.green;
			}
			pixel++;
		}
		tex.SetPixels (p);
		tex.Apply ();
		Sprite s = Sprite.Create (tex, new Rect (0, 0, w.Width, w.Height), new Vector2 (100, 100));
		image.sprite = s;
		PlayerController pc = PlayerController.Instance;
		foreach (Island item in w.islandList) {
			City c = item.myCities.Find (x => x.playerNumber == pc.number);
			if(c!=null){
				GameObject g = new GameObject ();
				g.transform.parent = this.transform;
//				Texture2D sprite = new Texture2D (1, 1);
//				Color[] ps = sprite.GetPixels ();
//				for (int a = 0; a < ps.Length; a++) {
//					ps [a] = Color.red;
//				}
//				sprite.SetPixels (ps);
//				sprite.Apply ();
//				i.sprite = Sprite.Create (sprite,new Rect (0, 0, 1, 1),new Vector2(2,2));
//				g.transform.localPosition = new Vector3 (c.myWarehouse.BuildTile.X-2, c.myWarehouse.BuildTile.Y-2);

			}
		}

	}
}

using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Farm : OutputStructure {
	

	#region Serialize
	#endregion
	#region RuntimeOrOther

	public Growable growable;
	public int growableReadyCount;
	public int OnRegisterCallbacks;
	Queue<Structure> workingGrowables;

	#endregion

    public override float Efficiency{
		get {
			return Mathf.Round(((float)OnRegisterCallbacks / (float)myRangeTiles.Count)*1000)/10f;
		}
	}
	public Farm(int id, string name,float produceTime, Item produce, Structure growable,int tileWidth, int tileHeight, int buildcost, int maintance ){
		this.ID = id;
		this.name = name;
		if(growable is Growable ==false){
			Debug.LogError ("this farm didnt receive a Growable Structure");			
		} else {
			this.growable = (Growable)growable;
		}
		this.tileWidth = tileWidth;
		this.tileHeight = tileHeight;
		this.buildcost = buildcost;
		this.maintenancecost = maintance;
		this.output = new Item[1];
		this.output [0] = produce;
		this.produceTime = produceTime;
		this.buildingRange = 3;
		maxOutputStorage = 5;
		myBuildingTyp = BuildingTyp.Blocking;
		BuildTyp = BuildTypes.Single;
		hasHitbox = true;
		this.canTakeDamage = true;
	}
	protected Farm(Farm f){
		OutputCopyData (f);
		this.growable = f.growable;
	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Farm(){}

	public override Structure Clone ()	{
		return new Farm (this);
	}

	public override void LoadPrototypData(Structure s){
		Farm f = s as Farm;
		if(f == null){
			Debug.LogError ("ERROR - Prototyp was wrong");
			return;
		}
		CopyData (f);
	}
	private void CopyData(Farm nb){
		BaseCopyData (nb);
		growable = nb.growable;

	}

	public override void OnBuild ()	{
		workingGrowables = new Queue<Structure> ();
		if(growable == null){
			return;
		}
		GameObject.FindObjectOfType<BuildController> ().BuildOnTile (3, new List<Tile>(myRangeTiles),playerNumber);
		//farm has it needs plant if it can 
		foreach (Tile rangeTile in myRangeTiles) {
			if(rangeTile.Structure != null){
				if(rangeTile.Structure.ID==growable.ID){
					rangeTile.Structure.RegisterOnChangedCallback (OnGrowableChanged);	
					OnRegisterCallbacks++;
					if(((Growable)rangeTile.Structure).hasProduced == true){
						growableReadyCount ++;
						workingGrowables.Enqueue (rangeTile.Structure);
					}
				}
			}
		}
		foreach(Tile rangeTile in myRangeTiles){
			rangeTile.RegisterTileStructureChangedCallback (OnTileStructureChange);
		}
	}
	public override void update (float deltaTime){
		if(growableReadyCount==0){
			return;
		}
		if(output[0].count >= maxOutputStorage){
			return;
		}
		//send out worker to collect goods
		produceCountdown -= deltaTime;
		if (produceCountdown <= 0) {
			produceCountdown = produceTime;
			if (growable != null) {
				Growable g = (Growable)workingGrowables.Dequeue ();
				output[0].count++;
				growableReadyCount--;
				((Growable)g).Reset ();
			}
			if (cbOutputChange != null) {
				cbOutputChange (this);
			}
		}
	}
	public void OnGrowableChanged(Structure str){
		if(str is Growable == false){
			str.UnregisterOnChangedCallback (OnGrowableChanged);
			return;
		}
		if(str.ID != growable.ID){
			return;
		}
		if(((Growable)str).hasProduced == false){
			return;
		}
		workingGrowables.Enqueue (str);
		growableReadyCount ++;
		// send worker todo this job
		// not important right now
	}
	public void OnTileStructureChange(Tile t, Structure old){
		if(old != null && old.ID == growable.ID){
			OnRegisterCallbacks--;
		}
		if(t.Structure == null){
			if(old.ID == growable.ID){
				OnRegisterCallbacks--;
			}
			return;
		}
		if(t.Structure.ID == growable.ID){
			OnRegisterCallbacks++;
			t.Structure.RegisterOnChangedCallback (OnGrowableChanged);	
		}
	}
	protected override void OnDestroy (){
		foreach (Worker item in myWorker) {
			item.Destroy ();
		}
	}
	public override void ExtraBuildUI (GameObject parent){
		//FIXME
		//TODO
		GameObject extra = GameObject.Instantiate (Resources.Load<GameObject> ("Prefabs/GamePrefab/SpriteSlider"));
		extra.transform.SetParent (parent.transform);
	}
	public override void UpdateExtraBuildUI (GameObject parent,Tile t){
		//FIXME
		//TODO
		HashSet<Tile> hs = this.GetInRangeTiles (t);
		if(hs==null){
			return;
		}
		float percentage=0;
		if(growable.fer !=null){
			if(City.island.myFertilities.Contains (growable.fer)==false){
				percentage = 0;
			} else {
				//TODO calculate the perfect grow environment?
			}
		} else {
			int count=0;
			foreach (Tile item in hs) {
				if(item.Structure!=null && item.Structure.ID==growable.ID){
					count++;
				}
			}
			percentage = Mathf.RoundToInt (((float)count / (float)hs.Count) * 100);
		}

		parent.GetComponentInChildren<SpriteSlider> ().ChangePercent (percentage);
		
	}

}

using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FarmPrototypData : OutputPrototypData {
	public Growable growable;
}


[JsonObject(MemberSerialization.OptIn)]
public class Farm : OutputStructure {
	

	#region Serialize
	#endregion
	#region RuntimeOrOther

	public Growable growable { get { return FarmData.growable; }}

	public int growableReadyCount;
	public int OnRegisterCallbacks;
	Queue<Structure> workingGrowables;

	protected FarmPrototypData _farmData;
	public FarmPrototypData  FarmData {
		get { if(_farmData==null){
				_farmData = (FarmPrototypData)PrototypController.Instance.GetStructurePrototypDataForID (ID);
			}
			return _farmData;
		}
	}
	#endregion

    public override float Efficiency{
		get {
			return Mathf.Round(((float)OnRegisterCallbacks / (float)myRangeTiles.Count)*1000)/10f;
		}
	}
	public Farm(int id,FarmPrototypData fpd){
		_farmData = fpd;
		this.ID = id;
	}
	protected Farm(Farm f){
		OutputCopyData (f);

	}
	/// <summary>
	/// DO NOT USE
	/// </summary>
	public Farm(){
		workingGrowables = new Queue<Structure> ();
	}

	public override Structure Clone ()	{
		return new Farm (this);
	}
		

	public override void OnBuild ()	{
		workingGrowables = new Queue<Structure> ();
		if(growable == null){
			return;
		}
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
		produceCountdown += deltaTime;
		if(produceCountdown >= produceTime) {
			produceCountdown = 0;
			if (growable != null) {
				Growable g = (Growable)workingGrowables.Dequeue ();
				output[0].count++;
				growableReadyCount--;
				((Growable)g).Reset ();
			}
            cbOutputChange?.Invoke(this);
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
	public void OnTileStructureChange(Structure now, Structure old){
		if(old != null && old.ID == growable.ID ){
			OnRegisterCallbacks--;
		}
		if(now == null){
			return;
		}
		if(now.ID == growable.ID){
			OnRegisterCallbacks++;
			now.RegisterOnChangedCallback (OnGrowableChanged);	
			Growable g = now as Growable;
			if(g.hasProduced){
				//we need to check if its done
				//if so we need to get it queued for work!
				OnGrowableChanged (g);
			}
		}
	}
	protected override void OnDestroy (){
		if(myWorker==null){
			return;
		}
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
		int count=0;
		foreach (Tile item in hs) {
			if(item==null){
				continue;
			}
			if(item.Structure!=null && item.Structure.ID==growable.ID){
				count++;
			} else
			if(item.Structure==null && Tile.IsBuildType(item.Type)){
				count++;	
			}
		}
		percentage = Mathf.RoundToInt (((float)count / (float)hs.Count) * 100);

		if(growable.fer !=null){
			if(t.MyIsland==null){
				return;
			}
			if(t.MyIsland.myFertilities.Contains (growable.fer)==false){
				percentage = 0;
			} else {
				//TODO calculate the perfect grow environment?

			}
		} 
			
		parent.GetComponentInChildren<SpriteSlider> ().ChangePercent (percentage);
		
	}

}

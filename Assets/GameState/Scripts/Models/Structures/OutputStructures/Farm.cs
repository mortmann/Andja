using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FarmPrototypData : OutputPrototypData {
	public Growable growable;
    public int neededHarvestToProduce;
}


[JsonObject(MemberSerialization.OptIn)]
public class Farm : OutputStructure {


    #region Serialize
    [SerializeField] int currentlyHarvested = 0;
	#endregion
	#region RuntimeOrOther

	public Growable Growable { get { return FarmData.growable; }}
    public int NeededHarvestForProduce { get { return FarmData.neededHarvestToProduce; } }

    public int growableReadyCount;
	public int OnRegisterCallbacks;
	List<Growable> workingGrowables;

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
		workingGrowables = new List<Growable> ();
	}

	public override Structure Clone ()	{
		return new Farm (this);
	}
		

	public override void OnBuild ()	{
		workingGrowables = new List<Growable> ();
		if(Growable == null){
			return;
		}
		//farm has it needs plant if it can 
		foreach (Tile rangeTile in myRangeTiles) {
			if(rangeTile.Structure != null){
				if(rangeTile.Structure.ID==Growable.ID){
					rangeTile.Structure.RegisterOnChangedCallback (OnGrowableChanged);	
					OnRegisterCallbacks++;
					if(((Growable)rangeTile.Structure).hasProduced == true){
						growableReadyCount ++;
						workingGrowables.Add ((Growable)rangeTile.Structure);
					}
				}
			}
		}
		foreach(Tile rangeTile in myRangeTiles){
			rangeTile.RegisterTileOldNewStructureChangedCallback (OnTileStructureChange);
		}	
	}
	public override void Update (float deltaTime){
		if(growableReadyCount==0){
			return;
		}
		if(Output[0].count >= MaxOutputStorage){
			return;
		}
        //TODO: send out worker to collect goods
		produceCountdown += deltaTime;
		if(produceCountdown >= ProduceTime) {
			produceCountdown = 0;
			if (Growable != null) {
				Growable g = (Growable)workingGrowables[0];
                currentlyHarvested++;
				((Growable)g).Harvest ();
			}
        }
        if (currentlyHarvested >= NeededHarvestForProduce) {
            Output[0].count++;
            cbOutputChange?.Invoke(this);
            currentlyHarvested -= NeededHarvestForProduce;
        }
    }
	public void OnGrowableChanged(Structure str) {
		if(str is Growable == false){
            str.UnregisterOnChangedCallback (OnGrowableChanged);
			return;
		}
        Growable grow = (Growable)str;
        if (grow.ID != Growable.ID) { 
            grow.UnregisterOnChangedCallback(OnGrowableChanged);
            return;
		}
		if(((Growable)grow).hasProduced == false){
            if (workingGrowables.Contains((Growable)grow)) {
                growableReadyCount --;
            }
            return;
		}
		workingGrowables.Add (grow);
		growableReadyCount ++;
		// send worker todo this job
		// not important right now
	}
	public void OnTileStructureChange(Structure now, Structure old){
		if(old != null && old.ID == Growable.ID ){
			OnRegisterCallbacks--;
		}
		if(now == null){
			return;
		}
		if(now.ID == Growable.ID){
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
	public override object GetExtraBuildUIData () {
        return Efficiency;
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
			if(item.Structure!=null && item.Structure.ID==Growable.ID){
				count++;
			} else
			if(item.Structure==null && Tile.IsBuildType(item.Type)){
				count++;	
			}
		}
		percentage = Mathf.RoundToInt (((float)count / (float)hs.Count) * 100);

		if(Growable.Fertility !=null){
			if(t.MyIsland==null){
				return;
			}
			if(t.MyIsland.myFertilities.Contains (Growable.Fertility)==false){
				percentage = 0;
			} else {
				//TODO calculate the perfect grow environment?

			}
		} 
			
		parent.GetComponentInChildren<SpriteSlider> ().ChangePercent (percentage);
		
	}

}

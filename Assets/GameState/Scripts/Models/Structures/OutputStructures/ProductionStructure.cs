using UnityEngine;
using System.Collections.Generic;
using System;

using Newtonsoft.Json;

public enum InputTyp { AND, OR }

public class ProductionPrototypeData : OutputPrototypData {
    public Item[] intake;
    public InputTyp myInputTyp;
}

[JsonObject(MemberSerialization.OptIn)]
public class ProductionStructure : OutputStructure {

    #region Serialize
    private Item[] _intake;
    [JsonPropertyAttribute]
    public Item[] MyIntake {
        get {
            if (_intake == null) {
                if (ProductionData.intake == null) {
                    return null;
                }
                if (ProductionData.myInputTyp == InputTyp.AND) {
                    _intake = new Item[ProductionData.intake.Length];
                    for (int i = 0; i < ProductionData.intake.Length; i++) {
                        _intake[i] = ProductionData.intake[i].Clone();
                    }
                }
                if (ProductionData.myInputTyp == InputTyp.OR) {
                    _intake = new Item[1];
                    _intake[0] = ProductionData.intake[0].Clone();
                }
            }
            return _intake;
        }
        set {
            _intake = value;
        }
    }

    #endregion
    #region RuntimeOrOther
    private int _orItemIndex = int.MinValue; //TODO think about to switch to short if it needs to save space 
    public int OrItemIndex {
        get {
            if (_orItemIndex == int.MinValue) {
                for (int i = 0; i < ProductionData.intake.Length; i++) {
                    Item item = ProductionData.intake[i];
                    if (item.ID == MyIntake[0].ID) {
                        _orItemIndex = i;
                    }
                }
            }
            return _orItemIndex;
        }
        set {
            _orItemIndex = value;
        }
    }

    public Dictionary<OutputStructure, Item[]> RegisteredStructures;
    MarketStructure nearestMarketStructure;
    public InputTyp MyInputTyp { get { return ProductionData.myInputTyp; } }

    #endregion

    protected ProductionPrototypeData _productionData;
    public ProductionPrototypeData ProductionData {
        get {
            if (_productionData == null) {
                _productionData = (ProductionPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _productionData;
        }
    }

    //	public override float Efficiency{
    //		get {
    //			float inputs=0;
    //			for (int i = 0; i < MyIntake.Length; i++) {
    //				if(ProductionData.intake[i].count==0){
    //					Debug.LogWarning(ProductionData.intake[i].ToString() + " INTAKE REQUEST IS 0!!");
    //					continue;
    //				}
    //				inputs += MyIntake[i].count/ProductionData.intake[i].count;
    //			}
    //			if(inputs==0){
    //				return 0;
    //			}
    //			return Mathf.Clamp(Mathf.Round(inputs*1000)/10f,0,100);
    //		}
    //	}

    public ProductionStructure(int id, ProductionPrototypeData ProductionData) {
        this.ID = id;
        this._productionData = ProductionData;
    }
    /// <summary>
    /// DO NOT USE
    /// </summary>
    protected ProductionStructure() {
        RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
    }

    protected ProductionStructure(ProductionStructure str) {
        OutputCopyData(str);
    }


    public override Structure Clone() {
        return new ProductionStructure(this);
    }

    public override void Update(float deltaTime) {
        if (Output == null) {
            return;
        }
        for (int i = 0; i < Output.Length; i++) {
            if (Output[i].count == MaxOutputStorage) {
                return;
            }
        }

        base.Update_Worker(deltaTime);

        if (HasRequiredInput() == false) {
            return;
        }
        produceCountdown += deltaTime;
        if (produceCountdown >= ProduceTime) {
            produceCountdown = 0;
            if (MyIntake != null) {
                for (int i = 0; i < MyIntake.Length; i++) {
                    MyIntake[i].count--;
                }
            }
            for (int i = 0; i < Output.Length; i++) {
                Output[i].count += OutputData.output[i].count;
                cbOutputChange?.Invoke(this);
            }
        }
    }
    public bool HasRequiredInput() {
        if (ProductionData.intake == null) {
            return true;
        }
        if (MyInputTyp == InputTyp.AND) {
            for (int i = 0; i < MyIntake.Length; i++) {
                if (ProductionData.intake[i].count > MyIntake[i].count) {
                    return false;
                }
            }
        }
        else if (MyInputTyp == InputTyp.OR) {
            if (ProductionData.intake[OrItemIndex].count > MyIntake[0].count) {
                return false;
            }
        }
        return true;
    }

    public override void SendOutWorkerIfCan() {
        if (myWorker.Count >= MaxNumberOfWorker || jobsToDo.Count == 0 && nearestMarketStructure == null) {
            return;
        }
        Dictionary<Item, int> needItems = new Dictionary<Item, int>();
        for (int i = 0; i < MyIntake.Length; i++) {
            if (GetMaxIntakeForIntakeIndex(i) > MyIntake[i].count) {
                needItems.Add(MyIntake[i].Clone(), GetMaxIntakeForIntakeIndex(i) - MyIntake[i].count);
            }
        }
        if (needItems.Count == 0) {
            return;
        }
        if (jobsToDo.Count == 0 && nearestMarketStructure != null) {
            List<Item> getItems = new List<Item>();
            for (int i = MyIntake.Length - 1; i >= 0; i--) {
                if (City.HasAnythingOfItem(MyIntake[i])) {
                    Item item = MyIntake[i].Clone();
                    item.count = GetMaxIntakeForIntakeIndex(i) - MyIntake[i].count;
                    getItems.Add(item);
                }
            }
            if (getItems.Count <= 0) {
                return;
            }
            myWorker.Add(new Worker(this, nearestMarketStructure, getItems.ToArray(), false));
            World.Current.CreateWorkerGameObject(myWorker[0]);
        }
        else {
            base.SendOutWorkerIfCan();
        }
    }
    public void OnOutputChangedStructure(Structure str) {
        if (str is OutputStructure == false) {
            return;
        }
        if (jobsToDo.ContainsKey((OutputStructure)str)) {
            jobsToDo.Remove((OutputStructure)str);
        }
        OutputStructure ustr = ((OutputStructure)str);
        List<Item> getItems = new List<Item>();
        List<Item> items = new List<Item>(ustr.Output);
        foreach (Item item in RegisteredStructures[(OutputStructure)str]) {
            Item i = items.Find(x => x.ID == item.ID);
            if (i.count > 0) {
                getItems.Add(i);
            }
        }
        if (((OutputStructure)str).outputClaimed == false) {
            jobsToDo.Add(ustr, getItems.ToArray());
        }

    }

    public bool AddToIntake(Inventory toAdd) {
        if (MyIntake == null) {
            return false;
        }
        for (int i = 0; i < MyIntake.Length; i++) {
            if ((MyIntake[i].count + toAdd.GetAmountForItem(MyIntake[i])) > GetMaxIntakeForIntakeIndex(i)) {
                return false;
            }
            MyIntake[i].count += toAdd.GetAmountForItem(MyIntake[i]);
            toAdd.SetItemCountNull(MyIntake[i]);
            CallbackChangeIfnotNull();
        }

        return true;
    }

    public override Item[] GetRequieredItems(OutputStructure str, Item[] items) {
        List<Item> all = new List<Item>();
        for (int i = MyIntake.Length - 1; i >= 0; i--) {
            int id = MyIntake[i].ID;
            for (int s = 0; s < items.Length; s++) {
                if (items[i].ID == id) {
                    Item item = items[i].Clone();
                    item.count = GetMaxIntakeForIntakeIndex(i) - MyIntake[i].count;
                    if (item.count > 0)
                        all.Add(item);
                }
            }
        }
        return all.ToArray();
    }
    public override void OnBuild() {
        jobsToDo = new Dictionary<OutputStructure, Item[]>();
        RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
        //		for (int i = 0; i < intake.Length; i++) {
        //			intake [i].count = maxIntake [i];
        //		}
        if (myRangeTiles != null) {
            foreach (Tile rangeTile in myRangeTiles) {
                if (rangeTile.Structure == null) {
                    continue;
                }
                if (rangeTile.Structure is OutputStructure) {
                    if (rangeTile.Structure is MarketStructure) {
                        FindNearestMarketStructure(rangeTile);
                        continue;
                    }
                    if (RegisteredStructures.ContainsKey((OutputStructure)rangeTile.Structure) == false) {
                        Item[] items = HasNeedItem(((OutputStructure)rangeTile.Structure).Output);
                        if (items.Length == 0) {
                            continue;
                        }
                        ((OutputStructure)rangeTile.Structure).RegisterOutputChanged(OnOutputChangedStructure);
                        RegisteredStructures.Add((OutputStructure)rangeTile.Structure, items);
                    }
                }
            }
            City.RegisterStructureAdded(OnStructureBuild);
        }
        //FIXME this is a temporary fix to a stupid bug, which cause
        //i cant find because it works otherwise
        // bug is that myHome doesnt get set by json for this kind of structures
        // but it works for warehouse for example
        // to save save space we could always set it here but that would mean for every kind extra or in place structure???
        if (myWorker != null) {
            foreach (Worker w in myWorker) {
                w.myHome = this;
            }
        }

    }

    public void ChangeInput(Item change) {
        if (change == null) {
            return;
        }
        if (MyInputTyp == InputTyp.AND) {
            return;
        }
        for (int i = 0; i < ProductionData.intake.Length; i++) {
            Item item = ProductionData.intake[i];
            if (item.ID == change.ID) {
                MyIntake[0] = item.Clone();
                break;
            }
        }
    }
    /// <summary>
    /// Give an index for the needed Item, so only use in for loops
    /// OR
    /// for OR Inake use with orItemIndex
    /// </summary>
    /// <param name="i">The index.</param>
    public int GetMaxIntakeForIntakeIndex(int itemIndex) {
        if (itemIndex < 0 || itemIndex > ProductionData.intake.Length) {
            Debug.LogError("GetMaxIntakeMultiplier received an invalid number " + itemIndex);
            return -1;
        }
        return ProductionData.intake[itemIndex].count * 5; //TODO THINK ABOUT THIS
    }

    public Item[] HasNeedItem(Item[] output) {
        List<Item> items = new List<Item>();
        for (int i = 0; i < output.Length; i++) {
            for (int s = 0; s < MyIntake.Length; s++) {
                if (output[i].ID == MyIntake[s].ID) {
                    items.Add(output[i]);
                }
            }
        }
        return items.ToArray();
    }
    public void OnStructureBuild(Structure str) {
        if (str is OutputStructure == false) {
            return;
        }
        bool inRange = false;
        for (int i = 0; i < str.myStructureTiles.Count; i++) {
            if (myRangeTiles.Contains(str.myStructureTiles[i]) == true) {
                inRange = true;
                break;
            }
        }
        if (inRange == false) {
            return;
        }
        if (str is MarketStructure) {
            FindNearestMarketStructure(str.BuildTile);
            return;
        }
        Item[] items = HasNeedItem(((OutputStructure)str).Output);
        if (items.Length > 0) {
            ((OutputStructure)str).RegisterOutputChanged(OnOutputChangedStructure);
            RegisteredStructures.Add((OutputStructure)str, items);
        }
    }
    public void FindNearestMarketStructure(Tile tile) {
        if (tile.Structure is MarketStructure) {
            if (nearestMarketStructure == null) {
                nearestMarketStructure = (MarketStructure)tile.Structure;
            }
            else {
                float firstDistance = nearestMarketStructure.MiddleVector.magnitude - MiddleVector.magnitude;
                float secondDistance = tile.Structure.MiddleVector.magnitude - MiddleVector.magnitude;
                if (Mathf.Abs(secondDistance) < Mathf.Abs(firstDistance)) {
                    nearestMarketStructure = (MarketStructure)tile.Structure;
                }
            }
        }
    }


}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class UnitUI : MonoBehaviour {
    public Transform content;
    public GameObject itemPrefab;
    public Button settleButton;
    public Button patrolButton;
    Button currentlySelectedButton;
    public GameObject buttonCanvas;
    public ItemUI cannonsItem;
    public GameObject unitCombatInfo;
    public UnitHealthUI unitHealth;
    public NameInputField unitName;
    public Inventory inv;
    Dictionary<int, ItemUI> itemToGO;
    Unit unit;

    bool IsCurrentShipUI => unit is Ship;
    public Button addCannon;
    public Button removeCannon;
    private List<GameObject> unitGoalGOs;
    private List<GameObject> unitPatrolGoalGOs;

    public GameObject unitGoalPrefab;
    public GameObject unitPatrolGoalPrefab;
    public LineRenderer PatrolLineRendererPrefab;
    private List<LineRenderer> PatrolLineRendererList;

    public void Start() {
        patrolButton.onClick.AddListener(() => TogglePatrol());
        settleButton.onClick.AddListener(() => ToggleSettle());
    }

    public void Show(Unit unit) {
        this.unit = unit;
        unitHealth.Show(unit);
        settleButton.gameObject.SetActive(unit.IsPlayerUnit());
        patrolButton.gameObject.SetActive(unit.IsPlayerUnit());
        cannonsItem.gameObject.SetActive(unit.IsPlayerUnit());
        addCannon.gameObject.SetActive(unit.IsPlayerUnit());
        removeCannon.gameObject.SetActive(unit.IsPlayerUnit());
        //clear inventory screen
        foreach (Transform item in content.transform) {
            GameObject.Destroy(item.gameObject);
        }
        if (unitGoalGOs != null)
            foreach (GameObject goal in unitGoalGOs)
                Destroy(goal);
        if (unitPatrolGoalGOs != null)
            foreach (GameObject goal in unitPatrolGoalGOs)
                Destroy(goal);
        unitGoalGOs = new List<GameObject>();
        unitPatrolGoalGOs = new List<GameObject>();
        if (unit.IsPlayerUnit() == false) {
            return;
        }
        OnPatrolRouteChange(unit.patrolCommand);
        unit.patrolCommand.RegisterOnRouteChange(OnPatrolRouteChange);

        inv = unit.inventory;
        buttonCanvas.SetActive(true);

        //only ships can settle
        if (IsCurrentShipUI) {
            Ship ship = ((Ship)unit);
            cannonsItem.gameObject.transform.parent.gameObject.SetActive(true);
            cannonsItem.SetItem(ship.CannonItem, ship.MaximumAmountOfCannons);
            settleButton.gameObject.SetActive(true);
        }
        else {
            cannonsItem.gameObject.transform.parent.gameObject.SetActive(false);
            settleButton.gameObject.SetActive(false);
        }

        if (inv == null) {
            return;
        }

        inv.RegisterOnChangedCallback(OnInvChange);
        itemToGO = new Dictionary<int, ItemUI>();
        if (inv == null) {
            return;
        }
        for (int i = 0; i < inv.NumberOfSpaces; i++) {
            AddItemGameObject(i);
        }
    }

    private void OnPatrolRouteChange(PatrolCommand change) {
        if (unitPatrolGoalGOs != null)
            foreach (GameObject goal in unitPatrolGoalGOs)
                Destroy(goal);
        if (PatrolLineRendererList == null) {
            PatrolLineRendererList = new List<LineRenderer>();
        } else {
            PatrolLineRendererList.Clear();
            if (unitPatrolGoalGOs != null)
                foreach (LineRenderer goal in PatrolLineRendererList)
                    Destroy(goal.gameObject);
        }
        Vector2[] array = unit.patrolCommand.ToPositionArray();
        if (array.Length == 0)
            return;
        foreach (Vector2 v in array) {
            GameObject target = Instantiate(unitPatrolGoalPrefab);
            target.transform.position = new Vector3(v.x, v.y, -1);
        }
        if (array.Length == 1)
            return;

        for (int i = 0; i < array.Length; i++) {
            Vector2 v1 = array[i];
            Vector2 v2 = i < array.Length - 1 ? array[i + 1] : array[0];

            LineRenderer PatrolLineRenderer = Instantiate(PatrolLineRendererPrefab);
            PatrolLineRenderer.positionCount = 2;
            PatrolLineRenderer.SetPosition(0, new Vector3(v1.x, v1.y, -1));
            PatrolLineRenderer.SetPosition(1, new Vector3(v2.x, v2.y, -1));
            PatrolLineRendererList.Add(PatrolLineRenderer);
            //for 2 dest make it look better with 1 line
            if (array.Length == 2)
                return;
        }
        unit.patrolCommand.RegisterOnRouteChange(OnPatrolRouteChange);
    }

    private void TogglePatrol() {
        if (MouseController.Instance.mouseUnitState != MouseUnitState.Patrol) {
            SelectButton(patrolButton);
            MouseController.Instance.mouseUnitState = MouseUnitState.Patrol;
        }
        else {
            DeselectButton();
        }
    }
    private void ToggleSettle() {
        if (MouseController.Instance.mouseUnitState != MouseUnitState.Build) {
            SelectButton(settleButton);
            MouseController.Instance.BuildFromUnit();
        }
        else {
            DeselectButton();
        }
    }
    private void SelectButton(Button button) {
        DeselectButton();
        currentlySelectedButton = button;
        currentlySelectedButton.image.color = Color.blue;
    }
    private void DeselectButton() {
        if (currentlySelectedButton != null) {
            currentlySelectedButton.image.color = Color.white;
        }
        //for the case it is open when scene change or game closes
        if(MouseController.Instance!=null)
            MouseController.Instance.mouseUnitState = MouseUnitState.Normal;
        currentlySelectedButton = null;
    }
    private void AddItemGameObject(int i) {
        GameObject go = GameObject.Instantiate(itemPrefab);
        go.transform.SetParent(content.transform, false);
        ItemUI iui = go.GetComponent<ItemUI>();
        if (inv.HasItemInSpace(i) == false) {
            go.name = "item " + i;
            iui.SetItem(null, inv.MaxStackSize);
            itemToGO.Add(i, iui);
            return;
        }
        Item item = inv.GetItemInSpace(i);
        go.name = "item " + i;
        if (item.ID!=null||item.ID.Length==0) {
            iui.SetItem(item, inv.MaxStackSize);
            iui.AddClickListener((s) => { OnItemClick(i); });
            //			EventTrigger trigger = go.GetComponent<EventTrigger> ();
            //			EventTrigger.Entry entry = new EventTrigger.Entry( );
            //			entry.eventID = EventTriggerType.PointerClick;
            //			entry.callback.AddListener(  );
            //			trigger.triggers.Add( entry );
        }
        itemToGO.Add(i, iui);

    }
    void OnItemClick(int clicked) {
        Debug.Log("clicked " + clicked);
        unit.ToTradeItemToNearbyWarehouse(inv.GetItemInSpace(clicked));
    }
    public void OnInvChange(Inventory changedInv) {
        foreach (int i in itemToGO.Keys) {
            GameObject.Destroy(itemToGO[i].gameObject);
        }
        itemToGO = new Dictionary<int, ItemUI>();
        for (int i = 0; i < inv.NumberOfSpaces; i++) {
            AddItemGameObject(i);
        }
        inv = changedInv;

    }
    public void Update() {
        if (unit.CurrentHealth <= 0) {
            UIController.Instance.CloseUnitUI();
        }
        if (unit.IsPlayerUnit()) {
            if (IsCurrentShipUI) {
                Ship ship = ((Ship)unit);
                if (ship.HasCannonsToAddInInventory() != addCannon.gameObject.activeSelf) {
                    addCannon.gameObject.SetActive(ship.HasCannonsToAddInInventory());
                }
                if (ship.CanRemoveCannons() != removeCannon.gameObject.activeSelf) {
                    removeCannon.gameObject.SetActive(ship.CanRemoveCannons());
                }
                cannonsItem.RefreshItem(((Ship)unit).CannonItem);
            }
            if (unit.QueuedCommands != null) {
                int moveCommandCount = 0;
                for (int i = 0; i < unit.QueuedCommands.Count; i++) {
                    Command c = unit.QueuedCommands[i];
                    if (c is MoveCommand == false) {
                        continue; // TODO: make it otherwise visible
                    }
                    if (unitGoalGOs.Count - 1 <= moveCommandCount)
                        unitGoalGOs.Add(Instantiate(unitGoalPrefab));
                    unitGoalGOs[moveCommandCount].transform.position = c.Position;
                    moveCommandCount++;
                }
                while (unit.QueuedCommands.Count < unitGoalGOs.Count) {
                    Destroy(unitGoalGOs[unitGoalGOs.Count - 1]);
                    unitGoalGOs.RemoveAt(unitGoalGOs.Count - 1);
                }
            }
        }
    }
    //TODO: make this possible with 
    public void AddCannons() {
        if (IsCurrentShipUI == false) {
            return;
        }
        Ship ship = ((Ship)unit);
        ship.AddCannonsFromInventory(InputHandler.ShiftKey);
    }
    public void RemoveCannons() {
        if (IsCurrentShipUI == false) {
            return;
        }
        Ship ship = ((Ship)unit);
        ship.RemoveCannonsToInventory(InputHandler.ShiftKey);
    }
    private void OnDisable() {
        if(unit!=null)
            unit.patrolCommand.UnregisterOnRouteChange(OnPatrolRouteChange);
        DeselectButton();
        if (unitGoalGOs == null)
            return;
        foreach (var unitGoalGO in unitGoalGOs) {
            Destroy(unitGoalGO);
        }
        unitGoalGOs.Clear();
        if (unitPatrolGoalGOs != null)
            foreach (GameObject goal in unitPatrolGoalGOs)
                Destroy(goal);
        if (PatrolLineRendererList != null)
            foreach (LineRenderer goal in PatrolLineRendererList) {
                if (goal == null)
                    continue;
                Destroy(goal.gameObject);
            }
    }
}

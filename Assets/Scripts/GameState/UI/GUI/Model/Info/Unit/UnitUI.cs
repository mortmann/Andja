using Andja.Controller;
using Andja.Model;
using Andja.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Model {
    public class UnitUI : MonoBehaviour {
        public ImageText[] UnitInfos;
        public GameObject unitGoalPrefab;
        public GameObject unitPatrolGoalPrefab;
        public LineRenderer PatrolLineRendererPrefab;
        public Transform content;
        public GameObject itemPrefab;
        public Button settleButton;
        public Button patrolButton;
        public GameObject buttonCanvas;
        public ItemUI cannonsItem;
        public Button removeCannon;
        public Button addCannon;
        public GameObject unitCombatInfo;
        
        private UnitInventory _inventory;
        private Dictionary<int, ItemUI> _itemToGO;
        private Unit _unit;
        private Button _currentlySelectedButton;
        private bool IsCurrentShipUI => _unit is Ship;
        private List<GameObject> _unitGoalGOs;
        private List<GameObject> _unitPatrolGoalGOs;

        private List<LineRenderer> _patrolLineRendererList;

        public void Start() {
            patrolButton.onClick.AddListener(TogglePatrol);
            settleButton.onClick.AddListener(ToggleSettle);
        }

        public void Show(Unit showUnit) {
            if (_unit == showUnit) {
                return;
            }

            _unit?.UnregisterOnDestroyCallback(OnUnitDestroy);
            _unit?.Inventory?.UnregisterOnChangedCallback(OnInvChange);
            _unit = showUnit;
            if (_unit == null)
                return;
            UIController.Instance.HighlightUnits(_unit);
            _unit.RegisterOnDestroyCallback(OnUnitDestroy);
            settleButton.gameObject.SetActive(_unit.IsOwnedByCurrentPlayer());
            patrolButton.gameObject.SetActive(_unit.IsOwnedByCurrentPlayer());
            cannonsItem.gameObject.SetActive(_unit.IsOwnedByCurrentPlayer());
            addCannon.gameObject.SetActive(_unit.IsOwnedByCurrentPlayer());
            removeCannon.gameObject.SetActive(_unit.IsOwnedByCurrentPlayer());
            //clear Inventory screen
            foreach (Transform item in content.transform) {
                GameObject.Destroy(item.gameObject);
            }

            if (_unitGoalGOs != null)
                foreach (GameObject goal in _unitGoalGOs)
                    Destroy(goal);
            if (_unitPatrolGoalGOs != null)
                foreach (GameObject goal in _unitPatrolGoalGOs)
                    Destroy(goal);
            _unitGoalGOs = new List<GameObject>();
            _unitPatrolGoalGOs = new List<GameObject>();
            for (int i = 0; i < 3; i++) {
                UnitInfos[i].gameObject.SetActive(false);
            }

            Attack attack = _unit.GetElement<Attack>();
            UnitInfos[3].Set(UISpriteController.GetIcon(attack.ArmorType.ID), attack.ArmorType);
            UnitInfos[4].Set(UISpriteController.GetIcon(attack.DamageType.ID), attack.DamageType);
            if (_unit.IsOwnedByCurrentPlayer() == false) {
                return;
            }

            UnitInfos[0].Set(UISpriteController.GetIcon(CommonIcon.CurrentDamage),
                StaticLanguageVariables.CurrentDamage, () => attack.CurrentDamage + "");
            UnitInfos[1].Set(UISpriteController.GetIcon(CommonIcon.MaximumDamage),
                StaticLanguageVariables.MaximumDamage, () => attack.MaximumDamage + "");
            UnitInfos[2].Set(UISpriteController.GetIcon(CommonIcon.Speed),
                StaticLanguageVariables.Speed, () => _unit.Speed + "");

            OnPatrolRouteChange(_unit.PatrolCommand);
            _unit.PatrolCommand.RegisterOnRouteChange(OnPatrolRouteChange);

            _inventory = (UnitInventory)_unit.Inventory;
            buttonCanvas.SetActive(true);

            //only ships can settle
            if (IsCurrentShipUI) {
                Ship ship = ((Ship)_unit);
                cannonsItem.gameObject.transform.parent.gameObject.SetActive(true);
                cannonsItem.SetItem(ship.ShipAttack.CannonItem, ship.ShipAttack.MaximumAmountOfCannons);
                settleButton.gameObject.SetActive(true);
                if (_unit.RangeUStructure != null) {
                    if (_unit.RangeUStructure is WarehouseStructure) {
                        if (_unit.RangeUStructure.PlayerNumber == PlayerController.currentPlayerNumber) {
                            _unit.RangeUStructure.City.TradeUnit = _unit;
                            ICity city = _unit.RangeUStructure.City;
                            UIController.Instance.OpenOwnedCityInventory(
                                city,
                                item => city.TradeWithShip(item,
                                    () => city.PlayerTradeAmount,
                                    ship)
                            );
                        }
                    }
                }
            }
            else {
                cannonsItem.gameObject.transform.parent.gameObject.SetActive(false);
                settleButton.gameObject.SetActive(false);
            }

            if (_inventory == null) {
                return;
            }

            _inventory.RegisterOnChangedCallback(OnInvChange);
            _itemToGO = new Dictionary<int, ItemUI>();
            if (_inventory == null) {
                return;
            }

            for (int i = 0; i < _inventory.NumberOfSpaces; i++) {
                AddItemGameObject(i);
            }
        }

        private void OnUnitDestroy(Unit unit, IAttack attack) {
            UIController.Instance.CloseInfoUI();
        }

        private void OnPatrolRouteChange(PatrolCommand change) {
            if (_unitPatrolGoalGOs != null)
                foreach (GameObject goal in _unitPatrolGoalGOs)
                    Destroy(goal);
            if (_patrolLineRendererList == null) {
                _patrolLineRendererList = new List<LineRenderer>();
            }
            else {
                _patrolLineRendererList.Clear();
                if (_unitPatrolGoalGOs != null)
                    foreach (LineRenderer goal in _patrolLineRendererList)
                        Destroy(goal.gameObject);
            }

            Vector2[] array = _unit.PatrolCommand.ToPositionArray();
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
                _patrolLineRendererList.Add(PatrolLineRenderer);
                //for 2 dest make it look better with 1 line
                if (array.Length == 2)
                    return;
            }

            _unit.PatrolCommand.RegisterOnRouteChange(OnPatrolRouteChange);
        }

        private void TogglePatrol() {
            if (MouseController.Instance.MouseUnitState != MouseUnitState.Patrol) {
                SelectButton(patrolButton);
                MouseController.Instance.SetMouseUnitState(MouseUnitState.Patrol);
            }
            else {
                DeselectButton();
            }
        }

        private void ToggleSettle() {
            if (MouseController.Instance.MouseUnitState != MouseUnitState.Build) {
                SelectButton(settleButton);
                MouseController.Instance.BuildFromUnit();
            }
            else {
                DeselectButton();
            }
        }

        private void SelectButton(Button button) {
            DeselectButton();
            _currentlySelectedButton = button;
            _currentlySelectedButton.image.color = Color.blue;
        }

        private void DeselectButton() {
            if (_currentlySelectedButton != null) {
                _currentlySelectedButton.image.color = Color.white;
            }

            //for the case it is open when scene change or game closes
            if (MouseController.Instance != null)
                MouseController.Instance.SetMouseUnitState(MouseUnitState.Normal);
            _currentlySelectedButton = null;
        }

        private void AddItemGameObject(int i) {
            GameObject go = GameObject.Instantiate(itemPrefab);
            go.transform.SetParent(content.transform, false);
            ItemUI iui = go.GetComponent<ItemUI>();
            if (_inventory.HasItemInSpace(i) == false) {
                go.name = "item " + i;
                iui.SetItem(null, _inventory.MaxStackSize);
                _itemToGO.Add(i, iui);
                return;
            }

            Item item = _inventory.GetItemInSpace(i);
            go.name = "item " + i;
            if (item.ID != null || item.ID.Length == 0) {
                iui.SetItem(item, _inventory.MaxStackSize);
                iui.AddClickListener((s) => { OnItemClick(i, s); });
            }

            _itemToGO.Add(i, iui);
        }

        private void OnItemClick(int clicked, PointerEventData data) {
            switch (data.button) {
                case PointerEventData.InputButton.Left:
                    _unit.TradeItemToNearbyWarehouse(_inventory.GetItemInSpace(clicked));
                    break;
                case PointerEventData.InputButton.Right:
                    World.Current.CreateItemOnMap(_inventory.GetItemInSpace(clicked), _unit.CurrentPosition);
                    _inventory.RemoveItemInSpace(clicked);
                    break;
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }

        public void OnInvChange(Inventory changedInv) {
            foreach (int i in _itemToGO.Keys) {
                GameObject.Destroy(_itemToGO[i].gameObject);
            }

            _itemToGO = new Dictionary<int, ItemUI>();
            for (int i = 0; i < _inventory.NumberOfSpaces; i++) {
                AddItemGameObject(i);
            }

            _inventory = (UnitInventory)changedInv;
        }

        public void Update() {
            if (_unit.CurrentHealth <= 0) {
                UIController.Instance.CloseInfoUI();
            }

            if (_unit.IsOwnedByCurrentPlayer()) {
                if (IsCurrentShipUI) {
                    Ship ship = ((Ship)_unit);
                    if (ship.HasCannonsToAddInInventory() != addCannon.gameObject.activeSelf) {
                        addCannon.gameObject.SetActive(ship.HasCannonsToAddInInventory());
                    }

                    if (ship.CanRemoveCannons() != removeCannon.gameObject.activeSelf) {
                        removeCannon.gameObject.SetActive(ship.CanRemoveCannons());
                    }

                    cannonsItem.RefreshItem(((Ship)_unit).ShipAttack.CannonItem);
                }

                if (_unit.QueuedCommands != null) {
                    int moveCommandCount = 0;
                    for (int i = 0; i < _unit.QueuedCommands.Count; i++) {
                        Command c = _unit.QueuedCommands[i];
                        if (c is MoveCommand == false) {
                            continue; // TODO: make it otherwise visible
                        }

                        if (_unitGoalGOs.Count - 1 <= moveCommandCount)
                            _unitGoalGOs.Add(Instantiate(unitGoalPrefab));
                        _unitGoalGOs[moveCommandCount].transform.position = c.Position;
                        moveCommandCount++;
                    }

                    while (_unit.QueuedCommands.Count < _unitGoalGOs.Count) {
                        Destroy(_unitGoalGOs[_unitGoalGOs.Count - 1]);
                        _unitGoalGOs.RemoveAt(_unitGoalGOs.Count - 1);
                    }
                }

                InfoUI.Instance.UpdateUpkeep(_unit.UpkeepCost);
            }

            InfoUI.Instance.UpdateHealth(_unit.CurrentHealth, _unit.MaximumHealth);
        }

        //TODO: make this possible with
        public void AddCannons() {
            if (IsCurrentShipUI == false) {
                return;
            }

            Ship ship = ((Ship)_unit);
            ship.AddCannonsFromInventory(InputHandler.ShiftKey);
        }

        public void RemoveCannons() {
            if (IsCurrentShipUI == false) {
                return;
            }

            Ship ship = ((Ship)_unit);
            ship.RemoveCannonsToInventory(InputHandler.ShiftKey);
        }

        public void OnDisable() {
            if (_unit != null) {
                _unit.PatrolCommand.UnregisterOnRouteChange(OnPatrolRouteChange);
                _unit.UnregisterOnDestroyCallback(OnUnitDestroy);
                UIController.Instance?.DehighlightUnits(_unit);
                if (_unit.RangeUStructure != null)
                    _unit.RangeUStructure.City.TradeUnit = null;
                MouseController.Instance?.UnselectUnit(false);
            }

            DeselectButton();
            if (_unitGoalGOs == null)
                return;
            foreach (var unitGoalGO in _unitGoalGOs) {
                Destroy(unitGoalGO);
            }

            _unitGoalGOs.Clear();
            if (_unitPatrolGoalGOs != null)
                foreach (GameObject goal in _unitPatrolGoalGOs)
                    Destroy(goal);
            if (_patrolLineRendererList != null)
                foreach (LineRenderer goal in _patrolLineRendererList) {
                    if (goal == null)
                        continue;
                    Destroy(goal.gameObject);
                }

            _unit = null;
        }
    }
}
using Andja.Controller;
using Andja.Model.Components;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class Pirate {
        public static readonly int Number = GameData.PirateNumber; // so it isnt the same like the number of wilderness
        public static readonly float AggroRange = GameData.PirateAggroRange;
        [JsonPropertyAttribute] private float startCooldown;
        [JsonPropertyAttribute] private List<Ship> Ships;
        private float checkShipsCooldown = 0f;
        public Pirate() {
            Ships = new List<Ship>();
            this.startCooldown = GameData.PirateCooldown;
        }

        public void Update(float deltaTime) {
            if (startCooldown > 0) {
                startCooldown = Mathf.Clamp(startCooldown - deltaTime, 0, startCooldown);
                return;
            }
            if (Ships.Count < GameData.PirateShipCount) {
                AddShip();
            }
            if(checkShipsCooldown <= 0) {
                checkShipsCooldown = 5f;
                CheckShips();
            } else {
                checkShipsCooldown -= deltaTime;
            }
        }

        private void CheckShips() {
            foreach (Ship s in Ships) {
                if (s.CurrentMainMode == UnitMainModes.Attack)
                    continue;

                List<Ship> targets = new List<Ship>();
                Collider2D[] colls = Physics2D.OverlapCircleAll(s.CurrentPosition, AggroRange);
                foreach (Collider2D c in colls) {
                    ITargetableHoldingScript iths = c.gameObject.GetComponent<ITargetableHoldingScript>();
                    if (iths == null || !(iths.Holding is Ship ship)) continue;
                    if(ship.PlayerNumber != Number)
                        targets.Add(ship);
                }

                if (targets.Count <= 0) continue;
                var grouped = targets.GroupBy(x => x.PlayerNumber, (y,z)=>new { count = y, Ships = z });
                var ships = grouped.OrderBy(x => x.count).First();
                if (ships.count < 2) {
                    s.GiveAttackCommand(ships.Ships.First(), true);
                }
            }
        }

        public void AddShip() {
            Ship ship = PrototypController.Instance.GetPirateShipPrototyp();
            Tile t = World.Current.GetTileAt(Random.Range(0, World.Current.Height), 0);
            ship = (Ship)World.Current.CreateUnit(ship, null, t, Number);
            ship.RegisterOnDestroyCallback(OnShipDestroy);
            ship.RegisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
            Ships.Add(ship);
            OnShipArriveDestination(ship, true);
        }

        private void OnShipArriveDestination(Unit unit, bool goal) {
            Ship ship = unit as Ship;
            if (Ships.Contains(ship) == false) {
                Debug.LogError("Why did called when it is not a pirate ship?");
                return;
            }
            if (goal) {
                ship.GiveMovementCommand(World.Current.GetRandomOceanTile());
            }
        }

        public void OnShipDestroy(Unit u, IWarfare warfare) {
            u.UnregisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
            u.UnregisterOnDestroyCallback(OnShipDestroy);
            Ships.Remove((Ship)u);
        }

        internal void Load() {
            foreach(Ship ship in Ships) {
                ship.RegisterOnDestroyCallback(OnShipDestroy);
                ship.RegisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
                if(ship.Pathfinding.IsAtDestination)
                    OnShipArriveDestination(ship, true);
                ship.Load();
            }
        }
    }
}
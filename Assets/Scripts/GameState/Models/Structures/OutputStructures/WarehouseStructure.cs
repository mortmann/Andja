using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using Andja.Utility;

namespace Andja.Model {

    public class WarehousePrototypData : MarketPrototypData {
        public int tradeItemCount;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WarehouseStructure : MarketStructure {

        #region RuntimeOrOther

        public int TradeItemCount => WarehouseData.tradeItemCount;
        public Tile TradeTile { get; protected set; }
        public List<Unit> InRangeUnits { get; protected set; }

        private WarehousePrototypData _warehouseData;
        public WarehousePrototypData WarehouseData =>
            _warehouseData ??= (WarehousePrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        #endregion RuntimeOrOther

        public WarehouseStructure(string id, WarehousePrototypData wpd) {
            this.ID = id;
            InRangeUnits = new List<Unit>();
            this._warehouseData = wpd;
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public WarehouseStructure() {
            InRangeUnits = new List<Unit>();
        }

        protected WarehouseStructure(WarehouseStructure str) {
            this.ID = str.ID;
            InRangeUnits = new List<Unit>();
        }

        public override bool SpecialCheckForBuild(List<Tile> tiles) {
            return tiles.None(t => t.City != null 
                               && t.City.IsWilderness() == false 
                               && t.City.Warehouse != null);
        }

        public void AddUnitToTrade(Unit u) {
            InRangeUnits.Add(u);
        }

        public void RemoveUnitFromTrade(Unit u) {
            InRangeUnits.Remove(u);
        }

        public override void OnBuild() {
            base.OnBuild();
            Tile[,] sortedTiles = new Tile[TileWidth, TileHeight];
            List<Tile> ts = new List<Tile>(Tiles);
            ts.Sort((x, y) => x.X.CompareTo(y.X) + x.Y.CompareTo(y.Y));
            foreach (Tile ti in ts) {
                int x = ti.X - ts[0].X;
                int y = ti.Y - ts[0].Y;
                sortedTiles[x, y] = ti; // so we have the tile at the correct spot
            }
            //now we have the tile thats has the smallest x/y
            //to get the tile we now have to rotate a vector thats
            //1 up and 1 left from the temptile
            Vector2 rot = new Vector2((float)TileWidth / 2f + 0.5f, 0).Rotate(((Structure)this).Rotation);
            TradeTile = World.Current.GetTileAt(Mathf.FloorToInt(Center.x - rot.x), Mathf.FloorToInt(Center.y + rot.y));

            this.City.Warehouse = this;
        }

        public override Structure Clone() {
            return new WarehouseStructure(this);
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _warehouseData = null;
        }
        public override bool InCityCheck(IEnumerable<Tile> tiles, int playerNumber) {
            return true;
        }
    }
}
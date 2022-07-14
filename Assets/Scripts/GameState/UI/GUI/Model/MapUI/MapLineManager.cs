using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;
using Andja.Model;
using System.Linq;
using Andja.Utility;
using System;

namespace Andja.UI.Model {
    public class MapLineManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {
        UILineRenderer lineRenderer;
        int stopIndex = -1;
        private Vector2 worldSize;
        private Vector2 mapImageSize;
        private Vector2 worldToMapScale;
        private Vector2 mapToWorldScale;
        public Vector2 currentWorldPosition;
        public Vector2 currentLocalPosition;
        List<Vector2> tempPoints;
        public GameObject prefabStop;
        public Transform stopParent;
        private TradeRoute tradeRoute;
        Dictionary<TradeRoute.Stop, MapTradeStop> mapTradeStops = new Dictionary<TradeRoute.Stop, MapTradeStop>();
        List<Line> lines = new List<Line>();
        internal void SetTradeRoute(TradeRoute tradeRoute) {
            this.tradeRoute = tradeRoute;
            lineRenderer = GetComponent<UILineRenderer>();
            Line.LineThickness = lineRenderer.LineThickness;
            if (mapTradeStops != null) {
                foreach (var mts in mapTradeStops) {
                    Destroy(mts.Value.gameObject);
                }
            }
            mapTradeStops?.Clear();
            tempPoints?.Clear();
            lines?.Clear();
            stopIndex = -1;

            worldSize = new Vector2(World.Current.Width, World.Current.Height);
            mapImageSize = GetComponent<RectTransform>().rect.size;
            worldToMapScale = mapImageSize / worldSize;
            mapToWorldScale = worldSize / mapImageSize;

            List<TradeRoute.Stop> points = tradeRoute.Goals;
            Vector2[] vecs = new Vector2[] { new Vector2(-1, -1) };
            if (points.Count > 0) {
                vecs = new Vector2[points.Count];
                for (int i = 0; i < points.Count; i++) {
                    vecs[i] = ScalePositionToMap(points[i].Destination);
                    CreateStop(points[i], vecs[i]);
                }
                for (int i = 0; i < vecs.Length; i++) {
                    Line line = new Line(vecs[i], vecs[(i + 1) % vecs.Length], points[i % points.Count]);
                    lines.Add(line);
                }
                lineRenderer.Points = lines.Select(x => x.a).Append(lines.Last().b).ToArray();
            } else {
                lineRenderer.Points = vecs;
            }
            UpdateSpots();
        }
        public void OnPointerDown(PointerEventData eventData) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), 
                                    eventData.position, eventData.pressEventCamera, out Vector2 position);
            stopIndex = -1;
            for (int i = 0; i < lines.Count; i++) {
                if(lines[i].IsPointInLine(position)) {
                    stopIndex = i;
                    break;
                }
            }
            if (stopIndex == -1)
                return;
            stopIndex++;
            tempPoints = new List<Vector2>(lineRenderer.Points);
            tempPoints.Insert(stopIndex, position);
            lineRenderer.Points = tempPoints.ToArray();
            currentLocalPosition = position;
            currentWorldPosition = ScalePositionToWorld(position);
        }

        private Vector2 ScalePositionToWorld(Vector2 position) {
            position.Scale(mapToWorldScale);
            return position;
        }
        private Vector2 ScalePositionToMap(Vector2 position) {
            position.Scale(worldToMapScale);
            return position;
        }
        public void OnPointerUp(PointerEventData eventData) {
            if (stopIndex == -1)
                return;
            TradeRoute.Stop stop = tradeRoute.AddStop(lines[stopIndex - 1].startingStop, currentWorldPosition);
            CreateStop(stop, currentLocalPosition);
            CreateNewLine(currentLocalPosition, stop);
            stopIndex = -1;
        }
        private void CreateNewLine(Vector2 position, TradeRoute.Stop stop) {
            Line oldLine = lines[stopIndex - 1];
            Line newLine = new Line(position, oldLine.b, stop);
            lines.Insert(stopIndex, newLine);
            lineRenderer.Points = lines.Select(x => x.a).Append(lines.Last().b).ToArray();
            oldLine.b = position;
            UpdateSpots();
        }


        public void OnDrag(PointerEventData eventData) {
            if (stopIndex == -1)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(),
                                eventData.position, eventData.pressEventCamera, out Vector2 position);
            currentLocalPosition = position;
            currentWorldPosition = ScalePositionToWorld(position);
            tempPoints = new List<Vector2>(lineRenderer.Points);
            tempPoints[stopIndex] = position;
            lineRenderer.Points = tempPoints.ToArray();
        }

        /// <summary>
        /// Point is the Stop at this position
        /// mapPos is the LOCAL in the ui position (not world cords)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="mapPos"></param>
        private void CreateStop(TradeRoute.Stop point, Vector2 mapPos) {
            MapTradeStop mts = Instantiate(prefabStop).GetComponent<MapTradeStop>();
            mts.Setup(point, point is TradeRoute.Trade);
            mts.transform.SetParent(stopParent, false);
            mts.transform.localPosition = mapPos;
            mapTradeStops[point] = mts;
            if (tradeRoute.Trades[0] == point) {
                mts.SetFirst();
            }
        }
        internal void RemoveStop(MapTradeStop mapStop) {
            tradeRoute.RemoveStop(mapStop.stop);
            mapTradeStops.Remove(mapStop.stop);
            RemoveLinePoint(lines.Find(x => x.startingStop == mapStop.stop));
        }

        internal void AddCity(ICity city) {
            TradeRoute.Stop stop = tradeRoute.GetTradeFor(city);
            Vector2 dest = ScalePositionToMap(stop.Destination);
            if(lines.Count == 1) {
                //This is the for the case when the route first starts between 2 cities
                lines.Add(new Line(ScalePositionToMap(tradeRoute.GetTrade(0).Destination), dest, stop));
            }
            else {
                //modify old endline to end at new last
                lines.Last().b = dest;
            }
            //line from stop to start
            lines.Add(new Line(dest, lines.First().a, stop));
            CreateStop(stop, ScalePositionToMap(city.Warehouse.TradeTile.Vector2));
            lineRenderer.Points = lines.Select(x => x.a).Append(lines.Last().b).ToArray();
            UpdateSpots();
        }


        internal void RemoveCity(ICity city) {
            var tradeStops = mapTradeStops.Keys.Where(x => x is TradeRoute.Trade t && t.city == city);
            foreach(var ts in tradeStops.ToArray()) {
                Destroy(mapTradeStops[ts].gameObject);
                mapTradeStops.Remove(ts);
            }
            var l = lines.FindAll(x => x.startingStop is TradeRoute.Trade t && t.city == city);
            if(tradeRoute.Trades.Count <= 1) {
                foreach(TradeRoute.Stop s in mapTradeStops.Keys.ToArray()) {
                    MapTradeStop mts = mapTradeStops[s];
                    if (mts.cityStop)
                        continue;
                    mapTradeStops.Remove(s);
                    RemoveStop(mts);
                    Destroy(mts.gameObject);
                }
            } 
            l.ForEach(x=>RemoveLinePoint(x));
            UpdateSpots();
            mapTradeStops[tradeRoute.Trades[0]].SetFirst();
        }

        private void UpdateSpots() {
            foreach (Line l in lines) {
                mapTradeStops[l.startingStop].SetRotation(l.Angle);
            }
        }

        private void RemoveLinePoint(Line line) {
            int index = lines.IndexOf(line);
            lines.Remove(line);
            if (lines.Count > 0) {
                int oi = (index - 1 + lines.Count) % lines.Count;
                lines[oi].b = line.b;
                lineRenderer.Points = lines.Select(x => x.a).Append(lines.Last().b).ToArray();
            }
            else
                lineRenderer.Points = new Vector2[] { new Vector2(-1, -1) };
        }
        
        internal void OnTradeStopDown(TradeRoute.Stop stop) {
            stopIndex = tradeRoute.Goals.IndexOf(stop);
        }

        internal void OnTradeStopUp(TradeRoute.Stop stop) {
            Line l = lines.Find(x => x.startingStop == stop);
            int index = lines.IndexOf(l);
            lines[index - 1].b = currentLocalPosition;
            l.a = currentLocalPosition;
            stop.SetPosition(currentWorldPosition);
            stopIndex = -1;
        }

    }
}

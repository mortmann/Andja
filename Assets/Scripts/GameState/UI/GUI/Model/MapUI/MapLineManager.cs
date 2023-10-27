using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;
using Andja.Model;
using System.Linq;
using Andja.Utility;
using System;

namespace Andja.UI.Model {
    public class MapLineManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
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
            if(this.tradeRoute != null) {
                this.tradeRoute.UnregisterGoalAdded(OnStopAdded);
                this.tradeRoute.UnregisterGoalRemoved(OnStopRemoved);
            }
            this.tradeRoute = tradeRoute;            
            this.tradeRoute.RegisterGoalAdded(OnStopAdded);
            this.tradeRoute.RegisterGoalRemoved(OnStopRemoved);

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
                    CreateStop(points[i]);
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

        private void OnStopRemoved(TradeRoute.Stop stop) {
            Destroy(mapTradeStops[stop].gameObject);
            mapTradeStops.Remove(stop);
            RemoveLinePoint(lines.Find(x => x.startingStop == stop));
        }

        private void OnStopAdded(TradeRoute.Stop stop) {
            CreateStop(stop);
            if (tradeRoute.Valid == false) {
                return;
            }
            if(stop is TradeRoute.Trade trade) {
                AddCity(trade);
            } else {
                CreateNewLine(currentLocalPosition, stop);
            }
            //CreateNewLine(currentLocalPosition, stop);
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
            tradeRoute.AddStop(lines[stopIndex - 1].startingStop, currentWorldPosition);
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


        public void OnStopPointDrag(PointerEventData eventData) {
            if (stopIndex == -1)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(),
                                eventData.position, eventData.pressEventCamera, out Vector2 position);
            currentLocalPosition = position;
            currentWorldPosition = ScalePositionToWorld(position);
            tempPoints = new List<Vector2>(lineRenderer.Points) {
                [stopIndex] = position
            };
            lineRenderer.Points = tempPoints.ToArray();
        }

        /// <summary>
        /// Point is the Stop at this position
        /// mapPos is the LOCAL in the ui position (not world cords)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="mapPos"></param>
        private void CreateStop(TradeRoute.Stop point) {
            MapTradeStop mts = Instantiate(prefabStop).GetComponent<MapTradeStop>();
            mts.Setup(point, point is TradeRoute.Trade);
            mts.transform.SetParent(stopParent, false);
            mts.transform.localPosition = ScalePositionToMap(point.Destination);
            mapTradeStops[point] = mts;
            if (tradeRoute.Trades[0] == point) {
                mts.SetFirst();
            }
        }
        internal void RemoveStop(MapTradeStop mapStop) {
            tradeRoute.RemoveStop(mapStop.stop);
        }

        internal void AddCity(TradeRoute.Stop stop) {
            if (tradeRoute.Valid == false) {
                return;
            }
            Vector2 dest = ScalePositionToMap(stop.Destination);
            if(lines.Count == 0) {
                //This is the for the case when the route first starts between 2 cities
                TradeRoute.Trade trade = tradeRoute.GetTrade(0);
                lines.Add(new Line(ScalePositionToMap(trade.Destination), dest, trade));

                lines.Add(new Line(dest, ScalePositionToMap(trade.Destination), stop));
            }
            else if(lines.Count > 0) {
                //modify old endline to end at new last
                lines.Last().b = dest;
                //line from stop to start
                lines.Add(new Line(dest, lines.First().a, stop));
            } 
            lineRenderer.Points = lines.Select(x => x.a).Append(lines.Last().b).ToArray();
            UpdateSpots();
        }


        internal void RemoveCity(ICity city) {
            var tradeStops = mapTradeStops.Keys.Where(x => x is TradeRoute.Trade t && t.city == city).ToArray();
            foreach(var ts in tradeStops) {
                Destroy(mapTradeStops[ts].gameObject);
                mapTradeStops.Remove(ts);
            }
            lines.FindAll(x => x.startingStop is TradeRoute.Trade t && t.city == city)
                .ForEach(x => RemoveLinePoint(x));
            UpdateSpots();
            if(tradeRoute.Trades.Count > 0) {
                mapTradeStops[tradeRoute.Trades[0]].SetFirst();
            }
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
            UpdateSpots();
        }

    }
}

using Andja.Model;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {

    public class EventSpriteController : MonoBehaviour {
        public static EventSpriteController Instance { get; protected set; }
        private static Dictionary<string, Sprite> _nameToSprite;
        private Dictionary<GameEvent, GameObject> _eventToGo;
        public List<ExtraEventParticles> EventParticles;

        public void Start() {
            if (Instance != null) {
                Debug.LogError("There should never be two eventsprite controllers.");
            }
            Instance = this;
            _eventToGo = new Dictionary<GameEvent, GameObject>();
            LoadSprites();
        }

        public static void LoadSprites() {
            _nameToSprite = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/EventSprites/");
            foreach (Sprite s in sprites) {
                _nameToSprite.Add(s.name, s);
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Event);
            if (custom != null) {
                foreach (Sprite s in custom) {
                    _nameToSprite[s.name] = s;
                }
            }
        }

        internal void CreateEventTileSprites(string sprite_name, GameEvent gameEvent) {
            GameObject go = new GameObject();
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (FogOfWarController.FogOfWarOn) {
                if (FogOfWarController.IsFogOfWarAlways) {
                    sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
            sr.sprite = _nameToSprite[sprite_name];
            go.transform.position = gameEvent.position;
            _eventToGo.Add(gameEvent, go);
            sr.sortingLayerName = "TileModifier";
            sr.sortingOrder = 100000;
            List<ExtraEventParticles> ps = EventParticles.FindAll(x => x.GameEventID == gameEvent.ID);
            foreach (var particleGo in ps.Select(p => Instantiate(p.Prefab))) {
                particleGo.transform.SetParent(go.transform);
                particleGo.transform.localPosition = Vector3.zero;
                particleGo.GetComponent<Renderer>().sortingLayerName = "UnderSky";
                particleGo.GetComponent<Renderer>().sortingOrder = 100000;
                if (FogOfWarController.FogOfWarOn == false || FogOfWarController.IsFogOfWarAlways == false) continue;
                particleGo.GetComponent<ParticleSystemRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }

        internal void UpdateEventTileSprites(GameEvent gameEvent, float percantage) {
            throw new NotImplementedException();
        }

        internal void DestroyEventTileSprites(GameEvent gameEvent) {
            Destroy(_eventToGo[gameEvent]);
            _eventToGo.Remove(gameEvent);
        }

        [Serializable]
        public struct ExtraEventParticles {
            public string GameEventID;
            public GameObject Prefab;
        }
    }
}
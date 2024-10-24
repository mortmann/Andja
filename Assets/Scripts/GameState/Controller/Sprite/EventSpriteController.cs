﻿using Andja.Model;
using Andja.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller {

    public class EventSpriteController : MonoBehaviour {
        public static EventSpriteController Instance { get; protected set; }
        private static Dictionary<string, Sprite> nameToSprite;
        private Dictionary<GameEvent, GameObject> eventToGO;
        public List<ExtraEventParticles> EventParticles;

        private void Start() {
            if (Instance != null) {
                Debug.LogError("There should never be two eventsprite controllers.");
            }
            Instance = this;
            eventToGO = new Dictionary<GameEvent, GameObject>();
            LoadSprites();
        }

        private void Update() {
        }

        public static void LoadSprites() {
            nameToSprite = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/EventSprites/");
            foreach (Sprite s in sprites) {
                nameToSprite.Add(s.name, s);
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Event);
            if (custom != null) {
                foreach (Sprite s in custom) {
                    nameToSprite[s.name] = s;
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
            sr.sprite = nameToSprite[sprite_name];
            go.transform.position = gameEvent.position;
            eventToGO.Add(gameEvent, go);
            sr.sortingLayerName = "TileModifier";
            sr.sortingOrder = 100000;
            List<ExtraEventParticles> ps = EventParticles.FindAll(x => x.gameEventID == gameEvent.ID);
            foreach (ExtraEventParticles p in ps) {
                GameObject particle_go = Instantiate(p.Prefab);
                particle_go.transform.SetParent(go.transform);
                particle_go.transform.localPosition = Vector3.zero;
                particle_go.GetComponent<Renderer>().sortingLayerName = "UnderSky";
                particle_go.GetComponent<Renderer>().sortingOrder = 100000;
                if (FogOfWarController.FogOfWarOn) {
                    if (FogOfWarController.IsFogOfWarAlways) {
                        particle_go.GetComponent<ParticleSystemRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    }
                }
            }
        }

        internal void UpdateEventTileSprites(GameEvent gameEvent, float percantage) {
        }

        internal void DestroyEventTileSprites(GameEvent gameEvent) {
            Destroy(eventToGO[gameEvent]);
            eventToGO.Remove(gameEvent);
        }

        [Serializable]
        public struct ExtraEventParticles {
            public string gameEventID;
            public GameObject Prefab;
        }
    }
}
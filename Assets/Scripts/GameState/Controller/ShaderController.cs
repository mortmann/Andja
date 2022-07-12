using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntroPi;
using Andja.Model;
using Debug = UnityEngine.Debug;
using System;
using Andja.Utility;
using System.Linq;

namespace Andja.Controller {
    public enum ShadowType { CompleteClear, Clear, Few, Medium, High, VeryHigh, Full }
    public enum Speed { Slow, Medium, Fast, LudicrousSpeed, MopsGeschwindigkeit /*wir haben einen marderschaden*/ }

    public class ShaderController : MonoBehaviour {
        private Material _oceanMaterial;
        private CloudShadows _cloudShadows;
        private readonly Dictionary<GameEvent, Weather> _eventToWeather = new Dictionary<GameEvent, Weather>();

        private static readonly Weather NormalWeather = new Weather {
            cloudCoverage = ShadowType.Few,
            cloudSpeed = Speed.Medium,
            oceanSpeed = Speed.Slow
        };

        public void Start() {
            _cloudShadows = FindObjectOfType<CloudShadows>();
            EventController.Instance.RegisterOnEvent(OnEventStarted, OnEventEnded);
            _oceanMaterial = TileSpriteController.Instance.oceanInstance.GetComponent<Renderer>().material;
        }

        private void OnEventEnded(GameEvent gameevent) {
            if (_eventToWeather.ContainsKey(gameevent))
                _eventToWeather.Remove(gameevent); //TODO: make the weather clear up slowly
        }

        private void OnEventStarted(GameEvent gameevent) {
            _eventToWeather.Add(gameevent, new Weather {
                cloudCoverage = gameevent.CloudCoverage,
                cloudSpeed = gameevent.CloudSpeed,
                oceanSpeed = gameevent.OceanSpeed
            });
        }

        public float GetCloudCoverageFor(ShadowType shadowType) {
            return shadowType switch
            {
                ShadowType.CompleteClear => -1f,
                ShadowType.Clear => -0.4f,
                ShadowType.Few => -0.25f,
                ShadowType.Medium => -0.15f,
                ShadowType.High => 0.15f,
                ShadowType.VeryHigh => 0.3f,
                ShadowType.Full => 1f,
                _ => 0,
            };
        }
        public float GetCloudSpeedFor(Speed speed) {
            return speed switch
            {
                Speed.Slow => 0.25f * WorldController.Instance.TimeMultiplier,
                Speed.Medium => 0.5f * WorldController.Instance.TimeMultiplier,
                Speed.Fast => 1.15f * WorldController.Instance.TimeMultiplier,
                Speed.LudicrousSpeed => 1.5f * WorldController.Instance.TimeMultiplier,
                Speed.MopsGeschwindigkeit => 2f * WorldController.Instance.TimeMultiplier,
                _ => 1f * WorldController.Instance.TimeMultiplier,
            };
        }

        public float GetOceanSpeedFor(Speed speed) {
            return speed switch
            {
                Speed.Slow => 0.75f * WorldController.Instance.TimeMultiplier,
                Speed.Medium => 0.9f * WorldController.Instance.TimeMultiplier,
                Speed.Fast => 1.1f * WorldController.Instance.TimeMultiplier,
                Speed.LudicrousSpeed => 1.25f * WorldController.Instance.TimeMultiplier,
                Speed.MopsGeschwindigkeit => 1.5f * WorldController.Instance.TimeMultiplier,
                _ => 1f,
            };
        }


        public void LateUpdate() {
            Weather[] closest = new Weather[2];
            closest[0] = NormalWeather;
            closest[1] = NormalWeather;
            float tOne = 0;
            float tTwo = 0;
            foreach (var ge in _eventToWeather) {
                float distance = Util.FindClosestDistancePointCircle(CameraController.Instance.middle, ge.Key.position, ge.Key.range);
                float tValue = 1 - EasingFunction.EaseInOutQuad(0, 1, Mathf.Clamp01(distance / (ge.Key.range * 1.1f)));
                if (tValue == 0)
                    continue;
                if(tValue>tOne) {
                    tOne = tValue;
                    closest[0] = ge.Value;
                } else 
                if(tValue > tTwo) {
                    tTwo = tValue;
                    closest[1] = ge.Value;
                }
            }
            float tempCloudSpeed = Mathf.Lerp(GetCloudSpeedFor(closest[1].cloudSpeed), 
                                                GetCloudSpeedFor(closest[0].cloudSpeed), tOne);
            float tempCloudCoverage = Mathf.Lerp(GetCloudCoverageFor(closest[1].cloudCoverage),
                                                    GetCloudCoverageFor(closest[0].cloudCoverage), tOne);
            float tempOceanSpeed = Mathf.Lerp(GetOceanSpeedFor(closest[1].oceanSpeed), 
                                                GetOceanSpeedFor(closest[0].oceanSpeed), tOne);
            _cloudShadows.SpeedMultiplier = tempCloudSpeed;
            _cloudShadows.CoverageModifier = tempCloudCoverage;
            
            _oceanMaterial.SetVector("_TimeScale",
                        new Vector4(tempOceanSpeed * WorldController.Instance.TimeMultiplier, (tempOceanSpeed / 10f) * WorldController.Instance.TimeMultiplier));
        }
    }
    public class Weather {
        public ShadowType cloudCoverage;
        public Speed cloudSpeed;
        public Speed oceanSpeed;
    }
}
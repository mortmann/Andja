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

        Material OceanMaterial;
        CloudShadows CloudShadows;
        Dictionary<GameEvent, Weather> eventToWeather = new Dictionary<GameEvent, Weather>();
        static readonly Weather normalWeather = new Weather {
            cloudCoverage = ShadowType.Few,
            cloudSpeed = Speed.Medium,
            oceanSpeed = Speed.Slow
        };

        void Start() {
            CloudShadows = FindObjectOfType<CloudShadows>();
            EventController.Instance.RegisterOnEvent(OnEventStarted, OnEventEnded);
            OceanMaterial = TileSpriteController.Instance.oceanInstance.GetComponent<Renderer>().material;
        }

        private void OnEventEnded(GameEvent gameevent) {
            if (eventToWeather.ContainsKey(gameevent))
                eventToWeather.Remove(gameevent); //TODO: make the weather clear up slowly
        }

        private void OnEventStarted(GameEvent gameevent) {
            eventToWeather.Add(gameevent, new Weather {
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


        void LateUpdate() {
            Weather[] clostest = new Weather[2];
            clostest[0] = normalWeather;
            clostest[1] = normalWeather;
            float tOne = 0;
            float tTwo = 0;
            foreach (var ge in eventToWeather) {
                float distance = Util.FindClosestDistancePointCircle(CameraController.Instance.middle, ge.Key.position, ge.Key.range);
                float tValue = 1 - EasingFunction.EaseInOutQuad(0, 1, Mathf.Clamp01(distance / (ge.Key.range * 1.1f)));
                if (tValue == 0)
                    continue;
                if(tValue>tOne) {
                    tOne = tValue;
                    clostest[0] = ge.Value;
                } else 
                if(tValue > tTwo) {
                    tTwo = tValue;
                    clostest[1] = ge.Value;
                }
            }
            float tempCloudSpeed = Mathf.Lerp(GetCloudSpeedFor(clostest[1].cloudSpeed), 
                                                GetCloudSpeedFor(clostest[0].cloudSpeed), tOne);
            float tempCloudCoverage = Mathf.Lerp(GetCloudCoverageFor(clostest[1].cloudCoverage),
                                                    GetCloudCoverageFor(clostest[0].cloudCoverage), tOne);
            float tempOceanSpeed = Mathf.Lerp(GetOceanSpeedFor(clostest[1].oceanSpeed), 
                                                GetOceanSpeedFor(clostest[0].oceanSpeed), tOne);
            CloudShadows.SpeedMultiplier = tempCloudSpeed;
            CloudShadows.CoverageModifier = tempCloudCoverage;
            
            OceanMaterial.SetVector("_TimeScale",
                        new Vector4(tempOceanSpeed * WorldController.Instance.TimeMultiplier, (tempOceanSpeed / 10f) * WorldController.Instance.TimeMultiplier));
        }
    }
    public class Weather {
        public ShadowType cloudCoverage;
        public Speed cloudSpeed;
        public Speed oceanSpeed;
    }
}
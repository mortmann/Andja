using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EntroPi;
namespace Andja.Controller {

    public class ShaderController : MonoBehaviour {
        public enum ShadowType { CompleteClear, Clear, Few, Medium, High, VeryHigh, Full }
        public enum Speed { Slow, Medium, Fast, LudicrousSpeed, MopsGeschwindigkeit /*wir haben einen marderschaden*/ }

        public Material OceanMaterial;


        CloudShadows CloudShadows;

        void Start() {
            CloudShadows = FindObjectOfType<CloudShadows>();

        }

        public void SetCloudCoverage(ShadowType shadowType) {
            switch (shadowType) {
                case ShadowType.CompleteClear:
                    CloudShadows.CoverageModifier = -1;
                    break;

                case ShadowType.Clear:
                    CloudShadows.CoverageModifier = -0.4f;
                    break;

                case ShadowType.Few:
                    CloudShadows.CoverageModifier = -0.25f;
                    break;

                case ShadowType.Medium:
                    CloudShadows.CoverageModifier = -0.15f;
                    break;

                case ShadowType.High:
                    CloudShadows.CoverageModifier = 0.15f;
                    break;

                case ShadowType.VeryHigh:
                    CloudShadows.CoverageModifier = 0.3f;
                    break;

                case ShadowType.Full:
                    CloudShadows.CoverageModifier = 1f;
                    break;
            }
        }
        public void SetCloudSpeed(Speed speed) {
            switch (speed) {
                case Speed.Slow:
                    CloudShadows.SpeedMultiplier = 0.25f * WorldController.Instance.TimeMultiplier;
                    break;

                case Speed.Medium:
                    CloudShadows.SpeedMultiplier = 0.5f * WorldController.Instance.TimeMultiplier;
                    break;

                case Speed.Fast:
                    CloudShadows.SpeedMultiplier = 1f * WorldController.Instance.TimeMultiplier;
                    break;

                case Speed.LudicrousSpeed:
                    CloudShadows.SpeedMultiplier = 1.5f * WorldController.Instance.TimeMultiplier;
                    break;

                case Speed.MopsGeschwindigkeit:
                    CloudShadows.SpeedMultiplier = 2f * WorldController.Instance.TimeMultiplier;
                    break;
            }
        }

        public void SetOceanSpeed(Speed speed) {
            switch (speed) {
                case Speed.Slow:
                    OceanMaterial.SetVector("_TimeScale",
                        new Vector4(1 * WorldController.Instance.TimeMultiplier, 1 * WorldController.Instance.TimeMultiplier));
                    break;

                case Speed.Medium:
                    OceanMaterial.SetVector("_TimeScale",
                        new Vector4(1 * WorldController.Instance.TimeMultiplier, 1 * WorldController.Instance.TimeMultiplier));
                    break;

                case Speed.Fast:
                    OceanMaterial.SetVector("_TimeScale",
                        new Vector4(1 * WorldController.Instance.TimeMultiplier, 1 * WorldController.Instance.TimeMultiplier));
                    break;

                case Speed.LudicrousSpeed:
                    OceanMaterial.SetVector("_TimeScale",
                        new Vector4(1 * WorldController.Instance.TimeMultiplier, 1 * WorldController.Instance.TimeMultiplier));
                    break;

                case Speed.MopsGeschwindigkeit:
                    OceanMaterial.SetVector("_TimeScale",
                        new Vector4(1 * WorldController.Instance.TimeMultiplier, 1 * WorldController.Instance.TimeMultiplier));
                    break;
            }
        }


        void Update() {
        
        }
    }

}
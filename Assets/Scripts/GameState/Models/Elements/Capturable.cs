using Andja.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public class CapturablePrototypData {
        public float maximumCaptureSpeed = 0.05f;
        public float decreaseCaptureSpeed = 0.01f;
    }

    public class Capturable : Element {
        [JsonPropertyAttribute] public float capturedProgress = 0;
        private Structure Structure;
        private CapturablePrototypData Data;
        private float _currentCaptureSpeed = 0f;

        public float MaximumCaptureSpeed => Structure.CalculateRealValue(nameof(Data.maximumCaptureSpeed), Data.maximumCaptureSpeed);
        public float DecreaseCaptureSpeed => Structure.CalculateRealValue(nameof(Data.decreaseCaptureSpeed), Data.decreaseCaptureSpeed);

        public void Capture(IWarfare warfare, float progress) {
            if (Captured) {
                DoneCapturing(warfare);
                return;
            }
            _currentCaptureSpeed = Mathf.Clamp(_currentCaptureSpeed + progress, 0, MaximumCaptureSpeed);
        }

        private void DoneCapturing(IWarfare warfare) {
            //either capture it or destroy based on if is a city of that player on that island
            ICity c = Structure.BuildTile.Island.Cities.Find(x => x.PlayerNumber == warfare.PlayerNumber);
            if (c != null) {
                capturedProgress = 0;
                Structure.OnDestroy();
                Structure.City = c;
                Structure.OnBuild();
            }
            else {
                Structure.Destroy();
            }
        }

        public bool Captured => Mathf.Approximately(capturedProgress, 1);
        public Capturable(Structure structure) : base(structure) {
            Structure = structure;
        }

        public override void OnDestroy() {
        }

        public override void OnLoad() {
        }

        public override void OnStart() {
        }

        public override void OnUpdate(float deltaTime) {
            if (_currentCaptureSpeed > 0) {
                capturedProgress += _currentCaptureSpeed * deltaTime;
                //reset the speed so that units can again add their speed
                _currentCaptureSpeed = 0;
            }
            else if (capturedProgress > 0) {
                capturedProgress -= DecreaseCaptureSpeed * deltaTime;
            }
            capturedProgress = Mathf.Clamp01(capturedProgress);
        }
    }

}
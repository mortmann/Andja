using Andja.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public class CapturablePrototypeData : ElementData {
        public float takeOverStartGoal = 100;
        public float maximumCaptureSpeed = 0.05f;
        public float decreaseCaptureSpeed = 0.01f;
        public override Element GetNewElement(BaseThing thing) {
            return new Capturable(thing as Structure);
        }
    }

    public class Capturable : Element, ICapturable {
        [JsonPropertyAttribute] public float CapturedProgress;

        private readonly Structure _structure;
        private CapturablePrototypeData _data;

        public CapturablePrototypeData Data => _data ??= _structure.GetElementData<CapturablePrototypeData>();
        private float _currentCaptureSpeed;

        public float TakeOverStartGoal =>
            _structure.CalculateRealValue(nameof(Data.takeOverStartGoal), Data.takeOverStartGoal);

        public float MaximumCaptureSpeed =>
            _structure.CalculateRealValue(nameof(Data.maximumCaptureSpeed), Data.maximumCaptureSpeed);

        public float DecreaseCaptureSpeed =>
            _structure.CalculateRealValue(nameof(Data.decreaseCaptureSpeed), Data.decreaseCaptureSpeed);


        public void Capture(ICapturer capturer) {
            if (Captured) {
                DoneCapturing(capturer);
                return;
            }

            _currentCaptureSpeed = Mathf.Clamp(_currentCaptureSpeed + capturer.CaptureSpeed, 0, MaximumCaptureSpeed);
        }

        private void DoneCapturing(ICapturer capturer) {
            //either capture it or destroy based on if is a city of that player on that island
            ICity c = _structure.BuildTile.Island.Cities.Find(x => x.PlayerNumber == capturer.PlayerNumber);
            if (c != null) {
                CapturedProgress = 0;
                _structure.OnDestroy();
                _structure.City = c;
                _structure.OnBaseThingBuild();
            }
            else {
                _structure.Destroy();
            }
        }

        public bool Captured => Mathf.Approximately(CapturedProgress, 1);

        public Capturable(Structure structure) : base(structure) {
            _structure = structure;
        }

        public override void OnDestroy() { }

        public override void OnLoad() { }

        public override void OnStart(bool loading = false) { }

        public override void OnUpdate(float deltaTime) {
            if (_currentCaptureSpeed > 0) {
                CapturedProgress += _currentCaptureSpeed * deltaTime;
                //reset the speed so that units can again add their speed
                _currentCaptureSpeed = 0;
            }
            else if (CapturedProgress > 0) {
                CapturedProgress -= DecreaseCaptureSpeed * deltaTime;
            }

            CapturedProgress = Mathf.Clamp01(CapturedProgress);
        }

        public static implicit operator Capturable(BaseThing baseThing) {
            return baseThing.GetElement<Capturable>();
        }
    }
}
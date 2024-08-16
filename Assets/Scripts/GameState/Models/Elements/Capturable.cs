using Andja.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class Capturable : Element {
        [JsonPropertyAttribute] public float capturedProgress = 0;
        Structure Structure;
        float CurrentCaptureSpeed => Structure.
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
            if (CurrentCaptureSpeed > 0) {
                capturedProgress += CurrentCaptureSpeed * deltaTime;
                //reset the speed so that units can again add their speed
                CurrentCaptureSpeed = 0;
            }
            else if (capturedProgress > 0) {
                capturedProgress -= DecreaseCaptureSpeed * deltaTime;
            }
            capturedProgress = Mathf.Clamp01(capturedProgress);
        }
    }

}
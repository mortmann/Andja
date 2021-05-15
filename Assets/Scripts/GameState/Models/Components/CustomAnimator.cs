using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Andja.Controller;

namespace Andja.Model.Components {

    public class CustomAnimator : MonoBehaviour {
        public Sprite[] Sprites;
        public float AnimationSpeed = 0.33f;
        public int NumberOfSprites = 9;
        int AnimationPos;
        float Timer;
        SpriteRenderer Renderer;
        float Speed = 4;

        void Start() {
            Renderer = GetComponent<SpriteRenderer>();
            GetComponent<SpriteRenderer>().sprite = Sprites[AnimationPos];
        }

        void Update() {
            Timer += Speed * WorldController.Instance.DeltaTime;
            if (Timer > AnimationSpeed) {
                AnimationPos++;
                AnimationPos %= NumberOfSprites;
                Timer = 0;
            }
        }

        internal void SetSprites(Sprite[] Sprites) {
            this.Sprites = Sprites;
            Renderer.sprite = Sprites[AnimationPos];
        }

    }
}
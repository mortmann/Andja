using Andja.Controller;
using UnityEngine;

namespace Andja.Model.Components {

    [RequireComponent(typeof(SpriteRenderer))]
    public class EffectAnimator : MonoBehaviour {
        public Effect effect;
        private SpriteRenderer spriteRenderer;
        private Sprite[] sprites;
        private float speed;
        private float timer = 0;
        private int index = 0;
        SpriteRenderer structureRenderer;
        public void Show(Sprite[] sprites, string layer, Effect effect, SpriteRenderer structureRenderer) {
            this.structureRenderer = structureRenderer;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = layer;
            spriteRenderer.sortingOrder = 1;
            spriteRenderer.maskInteraction = structureRenderer.maskInteraction;
            this.sprites = sprites;
            this.speed = 1f / sprites.Length;
            timer = speed;
            spriteRenderer.sprite = sprites[0];
            this.effect = effect;
        }

        private void Update() {
            if (sprites == null || sprites.Length == 0) {
                Debug.LogError("EffectAnimator has no sprites -- destroying");
                Destroy(this);
                return;
            }
            if (WorldController.Instance.IsPaused)
                return;
            spriteRenderer.maskInteraction = structureRenderer.maskInteraction;
            timer += WorldController.Instance.DeltaTime;
            if (timer >= speed) {
                index = (index + 1) % sprites.Length;
                spriteRenderer.sprite = sprites[index];
            }
        }
    }
}
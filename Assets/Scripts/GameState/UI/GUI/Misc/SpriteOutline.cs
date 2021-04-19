using Andja.Controller;
using UnityEngine;

namespace Andja {

    public class SpriteOutline : MonoBehaviour {
        public Color color = Color.white;
        public Color otherColor = Color.gray;
        public Color ownColor = Color.green;
        public Color enemyColor = Color.red;
        public int outlineSize = 1;
        public int PlayerNumber = -1;

        private SpriteRenderer spriteRenderer;

        private void OnEnable() {
            spriteRenderer = GetComponent<SpriteRenderer>();

            UpdateOutline(true);
        }

        private void OnDisable() {
            UpdateOutline(false);
        }

        private void Update() {
            UpdateOutline(true);
        }

        private void UpdateOutline(bool outline) {
            if (spriteRenderer == null)
                return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Outline", outline ? 1f : 0);
            if (PlayerNumber == PlayerController.currentPlayerNumber) {
                mpb.SetColor("_OutlineColor", ownColor);
            }
            else {
                if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, PlayerController.currentPlayerNumber)) {
                    mpb.SetColor("_OutlineColor", enemyColor);
                }
                else {
                    mpb.SetColor("_OutlineColor", otherColor);
                }
            }
            mpb.SetFloat("_OutlineSize", outlineSize);
            spriteRenderer.SetPropertyBlock(mpb);
        }
    }
}
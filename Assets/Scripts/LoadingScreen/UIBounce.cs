using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.LoadScreen {
    public class UIBounce : MonoBehaviour {
        Vector3 move;
        void Start() {
            move = Quaternion.AngleAxis(Random.value, Vector3.up) * new Vector2(Random.value * 100f, Random.value * 100f);
        }

        void Update() {
            transform.position = transform.position + (move * Time.deltaTime);
            Vector3[] v = new Vector3[4];
            GetComponent<RectTransform>().GetWorldCorners(v);

            float maxY = Mathf.Max(v[0].y, v[1].y, v[2].y, v[3].y);
            float minY = Mathf.Min(v[0].y, v[1].y, v[2].y, v[3].y);
            float maxX = Mathf.Max(v[0].x, v[1].x, v[2].x, v[3].x);
            float minX = Mathf.Min(v[0].x, v[1].x, v[2].x, v[3].x);

            if (minY < 0 || maxY > Screen.height) {
                move.y *= -1;
            }
            if (minX < 0 || maxX > Screen.width) {
                move.x *= -1;
            }
        }
    }
}

using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.Editor.UI {

    public class ResourcesSetter : MonoBehaviour {
        public Transform Content;
        public GameObject ResourceSet;

        // Use this for initialization
        private void Start() {
            foreach (Item item in PrototypController.Instance.MineableItems) {
                GameObject go = Instantiate(ResourceSet);
                go.transform.SetParent(Content);
                go.GetComponentInChildren<Text>().text = item.Name;
                InputField upperAmountField = go.transform.Find("UpperLimit").gameObject.GetComponent<InputField>();
                upperAmountField.onEndEdit.AddListener(x => {
                    if (x.Length == 0)
                        upperAmountField.text = "0";
                    int amount = 0;
                    int.TryParse(x, out amount);
                    EditorController.Instance.OnResourceChange(item.ID, amount, false);
                });
                InputField lowerAmountField = go.transform.Find("LowerLimit").gameObject.GetComponent<InputField>();
                lowerAmountField.onEndEdit.AddListener(x => {
                    if (x.Length == 0)
                        lowerAmountField.text = "0";
                    int amount = 0;
                    int.TryParse(x, out amount);
                    EditorController.Instance.OnResourceChange(item.ID, amount, true);
                });
            }
        }

        // Update is called once per frame
        private void Update() {
        }
    }
}
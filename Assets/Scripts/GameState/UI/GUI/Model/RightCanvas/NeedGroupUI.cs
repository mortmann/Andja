using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class NeedGroupUI : MonoBehaviour {
        public Text nameText;
        public GameObject listGO;
        public GameObject needPrefab;
        private Dictionary<string, NeedUI> needToUI;
        private List<Need>[] Needs;

        private NeedGroup NeedGroup;
        public bool IsEmpty => Needs[NeedsUIController.CurrentSelectedLevel].Count == 0;

        private void OnEnable() {
        }

        public void SetGroup(NeedGroup group) {
            foreach (Transform t in listGO.transform)
                Destroy(t.gameObject);
            NeedGroup = group;
            Needs = new List<Need>[PrototypController.Instance.NumberOfPopulationLevels];
            for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
                Needs[i] = new List<Need>();
            }
            needToUI = new Dictionary<string, NeedUI>();
            nameText.text = group.Name;
            gameObject.name = group.Name;
            foreach (Need need in group.Needs) {
                GameObject b = Instantiate(needPrefab);
                b.transform.SetParent(listGO.transform, false);
                NeedUI ui = b.GetComponent<NeedUI>();
                ui.SetNeed(need);
                needToUI[need.ID] = ui;
                Needs[need.StartLevel].Add(need);
            }
        }

        public void Show(HomeStructure home) {
            foreach (Need need in NeedGroup.Needs) {
                if(home.PopulationLevel != need.StartLevel) {
                    needToUI[need.ID].gameObject.SetActive(false);
                } else {
                    needToUI[need.ID].gameObject.SetActive(true);
                }
                needToUI[need.ID].Show(home);
            }
        }

        public void UpdateLevel(int level) {
            for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
                Needs[i].ForEach(x => {
                    needToUI[x.ID].gameObject.SetActive(x.StartLevel == level);
                });
            }
        }
    }
}
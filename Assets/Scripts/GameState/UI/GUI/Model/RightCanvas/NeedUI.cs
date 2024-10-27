using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class NeedUI : TranslationBase {
        public Slider slider;
        public Text nameText;
        public Text percentageText;
        public Image image;

        protected INeed need;
        protected HomeStructure home;
        private bool locked;
        private TranslationData notInRange;
        private TranslationData inRange;
        private TranslationData Locked;

        public void SetNeed(Need need) {
            if (need == null)
                Destroy(gameObject);
            this.need = need;
            this.name = need.Name;
            string name = need.Name + " | ";
            if (need.IsItemNeed()) {
                name += need.Item.Name;
            }
            else {
                if (need.Structures == null) {
                    nameText.text = "Missing Structure";
                    //					Debug.LogWarning(ns[i].ID + " " + curr.name +" is missing its structure! Either non declared or structure not existing!");
                    return;
                }
                //TODO: rework needed
                if (need.Structures.Length == 0)
                    return;
                name += need.Structures[0].SmallName;
            }
            nameText.text = name;
            if (PlayerController.CurrentPlayer.HasNeedUnlocked(need) == false) {
                locked = true;
                PlayerController.CurrentPlayer.RegisterNeedUnlock(OnNeedUnlock);
            }
            OnChangeLanguage();
        }

        public void Show(HomeStructure homeStructure) {
            if (need == null) {
                Debug.LogError("NEEDUI " + name + " is missing its need! -- Should not happen");
                return;
            }
            home = homeStructure;
            INeed n = home.GetNeedGroups()?.Find(x => need.Group != null && x.ID == need.Group.ID)?
                                          .Needs.Find(x => x.ID == need.ID);
            slider.value = 0;
            if (n == null) {
                return;
            }
            need = n;
        }

        private void OnNeedUnlock(Need need) {
            if (need.ID != this.need.ID)
                return;
            PlayerController.CurrentPlayer.UnregisterNeedUnlock(OnNeedUnlock);
            locked = false;
        }

        private void Update() {
            if (locked || need == null)
                return;
            if (need.IsItemNeed()) {
                float Percentage = Mathf.RoundToInt(need.GetFulfillment(home.PopulationLevel) * 100);
                percentageText.text = Percentage + "%";
                slider.value = Percentage;
            }
            else {
                if (home.IsStructureNeedFulfilled(need)) {
                    percentageText.text = inRange?.translation;
                    slider.value = 100;
                }
                else {
                    percentageText.text = notInRange?.translation;
                    slider.value = 0;
                }
            }
        }

        public override void OnStart() {
        }

        public override void OnChangeLanguage() {
            notInRange = UILanguageController.Instance.GetTranslationData("NeedNotInRange");
            inRange = UILanguageController.Instance.GetTranslationData("NeedInRange");
            Locked = UILanguageController.Instance.GetTranslationData("NeedLocked");
            if (locked) {
                percentageText.text = Locked?.translation;
            }
        }

        public override TranslationData[] GetTranslationDatas() {
            return new TranslationData[3] {
            new TranslationData("NeedInRange"),
            new TranslationData("NeedNotInRange"),
            new TranslationData("NeedLocked")
        };
        }
    }
}
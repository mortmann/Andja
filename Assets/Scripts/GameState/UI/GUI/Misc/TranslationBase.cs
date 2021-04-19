using Andja.Controller;
using UnityEngine;

namespace Andja.UI {

    public abstract class TranslationBase : MonoBehaviour {

        public void Start() {
            UILanguageController.Instance.RegisterLanguageChange(OnChangeLanguage);
            OnStart();
        }

        public abstract void OnStart();

        public abstract void OnChangeLanguage();

        public abstract TranslationData[] GetTranslationDatas();

        public string GetRealName() {
            string realname = "";
            Transform current = transform;
            while (current != null) {
                realname = current.name + "/" + realname;
                current = current.parent;
            }
            return realname;
        }
    }
}
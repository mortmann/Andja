using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.UI {

    public class ShowHoverOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private LanguageVariables Variables;
        private StaticLanguageVariables variable;
        private TranslationData data;
        private bool showName;

        public void OnPointerEnter(PointerEventData eventData) {
            if (showName) {
                FindObjectOfType<HoverOverScript>().Show(Variables?.Name ?? data?.translation ?? "***Missing***");
            }
            else {
                FindObjectOfType<HoverOverScript>().Show(Variables?.HoverOver ?? data?.hoverOverTranslation ?? "***Missing***");
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            FindObjectOfType<HoverOverScript>().Unshow();
        }

        internal void SetVariable(LanguageVariables data, bool showName) {
            Variables = data;
            this.showName = showName;
        }

        internal void SetVariable(StaticLanguageVariables variable, bool showName) {
            this.showName = showName;
            UILanguageController.Instance.RegisterLanguageChange(OnChange);
            this.variable = variable;
            OnChange();
        }

        private void OnChange() {
            data = UILanguageController.Instance.GetTranslationData(variable);
        }
    }
}
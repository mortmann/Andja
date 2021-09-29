using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.UI {

    public class ShowToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private LanguageVariables Variables;
        private StaticLanguageVariables variable;
        private TranslationData data;
        private bool showName;

        public void OnPointerEnter(PointerEventData eventData) {
            if (showName) {
                FindObjectOfType<ToolTip>().Show(Variables?.Name ?? data?.translation ?? "***Missing***");
            }
            else {
                FindObjectOfType<ToolTip>().Show(Variables?.HoverOver ?? data?.toolTipTranslation ?? "***Missing***");
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            FindObjectOfType<ToolTip>().Unshow();
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
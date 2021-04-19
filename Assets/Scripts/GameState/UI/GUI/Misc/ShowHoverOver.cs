using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowHoverOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    LanguageVariables Variables;
    StaticLanguageVariables variable;
    TranslationData data;
    bool showName;
    public void OnPointerEnter(PointerEventData eventData) {
        if(showName) {
            GameObject.FindObjectOfType<HoverOverScript>().Show(Variables?.Name ?? data.translation);
        }
        else {
            GameObject.FindObjectOfType<HoverOverScript>().Show(Variables?.HoverOver ?? data.hoverOverTranslation);
        }
    }
    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
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

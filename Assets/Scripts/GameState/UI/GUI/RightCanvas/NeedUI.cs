using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class NeedUI : MonoBehaviour {
    public Slider slider;
    public Text nameText;
    public Text percentageText;
    public Image image;

    protected Need need;
    protected HomeStructure home;
    private bool locked;
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
            percentageText.text = "LOCKED!";
            locked = true;
            PlayerController.CurrentPlayer.RegisterNeedUnlock(OnNeedUnlock);
            return;
        }
    }
    public void Show(HomeStructure homeStructure) {
        if (need == null) {
            Debug.LogError("NEEDUI "+ name +" is missing its need! -- Should not happen");
            return;
        }
        home = homeStructure;
        Need n = home.GetNeedGroups()?.Find(x => need.Group != null && x.ID == need.Group.ID)?.Needs.Find(x => x.ID == need.ID);
        if(n == null) {
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

    void Update() {
        if (locked || need == null)
            return;
        if (need.IsItemNeed()) {
            float percantage = need.GetFullfiment(home.PopulationLevel) * 100;
            percentageText.text = percantage + "%";
            slider.value = percantage;
        }
        else {
            if (home.IsStructureNeedFullfilled(need)) {
                percentageText.text = "In Range";
                slider.value = 100;
            }
            else {
                percentageText.text = "Not in Range";
                slider.value = 0;
            }
        }
    }

}

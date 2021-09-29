using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Andja.Model;

namespace Andja.UI.Model {
    public class ServiceStructureUI : MonoBehaviour {

        public Transform usageItemParent;
        public GameObject itemPrefab;
        ServiceStructure serviceStructure;
        Dictionary<Item, ItemUI> itemToUI = new Dictionary<Item, ItemUI>();
        public void Show(Structure structure) { 
            if (serviceStructure == structure)
                return;
            serviceStructure = structure as ServiceStructure;
            if (serviceStructure == null)
                return;
            itemToUI.Clear();
            foreach (Transform item in usageItemParent) {
                Destroy(item.gameObject);
            }
            if(serviceStructure.UsageItems != null) {
                foreach (Item item in serviceStructure.UsageItems) {
                    GameObject go = Instantiate(itemPrefab);
                    go.transform.SetParent(usageItemParent);
                    ItemUI iui = go.GetComponent<ItemUI>();
                    iui.SetItem(item, item.count, true);
                    itemToUI[item] = iui;
                }
            }
        }

        void LateUpdate() {
            if(serviceStructure.remainingUsageItems != null) {
                for (int i = 0; i < serviceStructure.remainingUsageItems.Length; i++) {
                    itemToUI[serviceStructure.UsageItems[i]].SetMissing(serviceStructure.remainingUsageItems[i] <= 0);
                }
            }
        }
    }

}
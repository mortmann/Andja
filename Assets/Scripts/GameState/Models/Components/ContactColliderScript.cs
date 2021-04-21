using UnityEngine;

namespace Andja.Model.Components {

    public class ContactColliderScript : MonoBehaviour {
        public OutputStructure contact;

        //dont know why this aint working
        private void OnCollisionEnter2D(Collision2D coll) {
            ITargetableHoldingScript ihs = coll.gameObject.GetComponent<ITargetableHoldingScript>();
            if (ihs == null || ihs.IsUnit == false)
                return;
            Unit unit = (Unit)ihs.Holding;
            if (unit.inventory != null) {
                unit.IsInRangeOfWarehouse(contact);
                ((WarehouseStructure)contact).AddUnitToTrade(unit);
            }
        }

        private void OnTriggerEnter2D(Collider2D coll) {
            ITargetableHoldingScript ihs = coll.gameObject.GetComponent<ITargetableHoldingScript>();
            if (ihs == null || ihs.IsUnit == false)
                return;
            Unit unit = (Unit)ihs.Holding;
            if (unit.inventory != null) {
                unit.IsInRangeOfWarehouse(contact);
                ((WarehouseStructure)contact).AddUnitToTrade(unit);
            }
        }

        private void OnCollisionExit2D(Collision2D coll) {
            ITargetableHoldingScript ihs = coll.gameObject.GetComponent<ITargetableHoldingScript>();
            if (ihs == null || ihs.IsUnit == false)
                return;
            Unit unit = (Unit)ihs.Holding;
            if (unit.inventory != null) {
                unit.IsInRangeOfWarehouse(null);
                ((WarehouseStructure)contact).RemoveUnitFromTrade(unit);
            }
        }

        private void OnTriggerExit2D(Collider2D collision) {
            ITargetableHoldingScript ihs = collision.gameObject.GetComponent<ITargetableHoldingScript>();
            if (ihs == null || ihs.IsUnit == false)
                return;
            Unit unit = (Unit)ihs.Holding;
            if (unit.inventory != null) {
                unit.IsInRangeOfWarehouse(null);
                ((WarehouseStructure)contact).RemoveUnitFromTrade(unit);
            }
        }
    }
}
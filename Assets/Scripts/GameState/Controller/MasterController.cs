using UnityEngine;

namespace Andja.Controller {
    /// <summary>
    /// Responsible to copy the Controllers from the Loadingscreen to the gamescreen.
    /// </summary>
    public class MasterController : MonoBehaviour {
        public bool isLoadingScreen;
        private static GameObject loadMaster;

        private void OnEnable() {
            //Get the FIRST active master -> when loaded to gamestate
            //this will be the loadstate one
            if (loadMaster != null && loadMaster != this && isLoadingScreen == false) {
                //to make it look better in hierachy we will resume the parent state of controller
                for (int i = loadMaster.transform.childCount - 1; i >= 0; i--) {
                    Transform child = loadMaster.transform.GetChild(i);
                    //if we find this child already
                    //kill it because the new one is better
                    Destroy(transform.Find(child.name)?.gameObject);
                    //get the better one
                    child.SetParent(this.transform);
                }
                // Death to the MASTER -- LONG LIVE THE MASTER!
                Destroy(loadMaster);

                //TODO: find a better fix for this:
                CameraController.Instance.GameScreenSetup();
                WorldController.Instance.SetRandomSeed();
            }
            else if (isLoadingScreen) {
                loadMaster = gameObject;
                //if there is no other master yet -- we are loadstate
                DontDestroyOnLoad(this);
            }
        }
    }
}
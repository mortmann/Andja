using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Andja.UI.Menu {
    public class VersionChecker : MonoBehaviour {
        const string versionURL = "https://pastebin.com/raw/b0WRMLD4";
        void Start() {
            StartCoroutine(CheckVersion());
        }

        IEnumerator CheckVersion() {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                yield break;
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
                yield break;

            UnityWebRequest www = UnityWebRequest.Get(versionURL);
            yield return www.SendWebRequest();
            if (www.error != null) {
                yield return null;
            }
            string version = www.downloadHandler.text;
            www.Dispose();
            if(version != Application.version) {
                MainMenuInfo.AddInfo(MainMenuInfo.InfoTypes.NewVersion, version);
            }
        }
    }
}

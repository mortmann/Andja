using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Andja.Utility {
    public class VariableTextSetter : MonoBehaviour {
        public enum Variables { PathfindingQueuedSearches, PathfindingTotalSearches, PathfindingAverageTimeSearches }
        public Variables Variable;
        Text text;
        void Start() {
            text = GetComponent<Text>();
        }

        // Update is called once per frame
        void LateUpdate() {
            switch (Variable) {
                case Variables.PathfindingQueuedSearches:
                    text.text = Pathfinding.PathfindingThreadHandler.queuedJobs.Count +"";
                    break;
                case Variables.PathfindingTotalSearches:
                    text.text = Pathfinding.PathfindingThreadHandler.TotalSearches + "";
                    break;
                case Variables.PathfindingAverageTimeSearches:
                    text.text = Pathfinding.PathfindingThreadHandler.averageSearchTime + "";
                    break;
            }
        }
    }
}


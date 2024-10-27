using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.Controller {

    public abstract class BaseMouseState {

        public virtual CursorType CursorType { get; }

        public virtual void Activate() {
            MouseController.ChangeCursorType(CursorType);
        }

        public abstract void Update();

        public virtual void Reset() {

        }
        public virtual void Deactivate() {

        }
        public virtual void OnGui() { }
    }

}
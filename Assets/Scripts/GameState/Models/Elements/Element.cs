using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.Model {

    public abstract class Element {

        public readonly BaseThing Parent;

        public Element(BaseThing baseThing) {
            Parent = baseThing;
        }

        public abstract void OnStart(bool loading = false);

        public abstract void OnDestroy();

        public abstract void OnUpdate(float deltaTime);

        public abstract void OnLoad();

    }

}
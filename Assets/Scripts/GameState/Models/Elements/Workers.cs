using Andja.Model;

namespace Andja.Model
{
    public class WorkersPrototypeData : ElementData {
        
        public override Element GetNewElement(BaseThing thing) {
            return new Producer(thing as Structure);
        }
    }
    
    public abstract class Workers : Element  {
        protected Workers(Structure structure) : base(structure) {
        }

        public override void OnStart(bool loading = false) {
            throw new System.NotImplementedException();
        }

        public override void OnDestroy() {
            throw new System.NotImplementedException();
        }

        public override void OnUpdate(float deltaTime) {
            throw new System.NotImplementedException();
        }

        public override void OnLoad() {
            throw new System.NotImplementedException();
        }
        
        
    }
}
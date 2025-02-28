using Andja.Controller;

namespace Andja.Model {
    public class CapturerPrototypeData : ElementData {
        public float captureSpeed = 0.01f;
        public float captureRange = 1f;
    }

    public class Capturer : Element, ICapturer {
        private Unit Unit => (Unit)Parent;

        public float CaptureRange => Data.captureRange;

        protected CapturerPrototypeData _data;

        private CapturerPrototypeData Data => _data ??= Parent.GetElementData<CapturerPrototypeData>();

        public Capturer(BaseThing baseThing) : base(baseThing) { }

        public override void OnStart(bool loading = false) { }

        public override void OnDestroy() { }

        public override void OnUpdate(float deltaTime) { }

        public override void OnLoad() { }

        public void UpdateCapture() {
            Unit.CurrentDoingMode = Parent.IsInRange(Unit.CurrentTarget, CaptureRange)
                ? UnitDoModes.Capture
                : UnitDoModes.Move;
        }

        public static implicit operator Capturer(BaseThing baseThing) {
            return baseThing.GetElement<Capturer>();
        }

        public bool CanCapture(Capturable capturable) {
            if (PlayerController.Instance.ArePlayersAtWar(PlayerNumber, capturable.PlayerNumber) == false) {
                return false;
            }

            return Parent.IsInRange(capturable.Parent, CaptureRange) ||
                   Unit.GiveMovementCommand(Unit.ClosestTargetPosition(capturable.Parent.Position));
        }
    }
}
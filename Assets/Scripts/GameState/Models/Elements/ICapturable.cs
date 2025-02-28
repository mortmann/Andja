namespace Andja.Model {
    public interface ICapturable {
        float TakeOverStartGoal { get; }
        float MaximumCaptureSpeed { get; }
        float DecreaseCaptureSpeed { get; }
        bool Captured { get; }
        int PlayerNumber { get; }
        void Capture(ICapturer capturer, float progress);
        void OnDestroy();
        void OnLoad();
        void OnStart(bool loading = false);
        void OnUpdate(float deltaTime);
    }
}
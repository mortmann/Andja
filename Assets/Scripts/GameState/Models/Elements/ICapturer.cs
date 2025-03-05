namespace Andja.Model {
    public interface ICapturer {
        float CaptureRange { get; }
        float CaptureSpeed { get; }
        void OnStart(bool loading = false);
        void OnDestroy();
        void OnUpdate(float deltaTime);
        void OnLoad();
        int PlayerNumber { get; }
    }
}
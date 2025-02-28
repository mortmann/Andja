namespace Andja.Model {
    public interface ICapturer {
        void OnStart(bool loading = false);
        void OnDestroy();
        void OnUpdate(float deltaTime);
        void OnLoad();
        int PlayerNumber { get; }
    }
}
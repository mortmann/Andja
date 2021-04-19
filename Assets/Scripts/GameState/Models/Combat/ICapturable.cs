namespace Andja.Model {

    public interface ICapturable : ITargetable {

        void Capture(IWarfare warfare, float progress);

        bool Captured { get; }
    }
}
using Andja;
using Andja.Model;

public interface ITestCallback {
    public void StructureBoolean(Structure structure, bool boo);
    void Structure(Structure obj);
    void RouteChange(Route arg1, Route arg2);
    void EventableEffectChange(GEventable arg1, Effect arg2, bool arg3);
    void StructureDestroy(Structure arg1, IWarfare arg2);
    void NeedUnlock(Need obj);
}

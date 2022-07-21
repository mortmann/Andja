using Andja.Model;

public interface ITestCallback {
    public void StructureBoolean(Structure structure, bool boo);
    void Structure(Structure obj);
    void RouteChange(Route arg1, Route arg2);
    void EventableEffectChange(IGEventable arg1, Effect arg2, bool arg3);
}

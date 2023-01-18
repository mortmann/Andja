using Andja.Controller;
using Newtonsoft.Json;
using System;
namespace Andja.Controller {
    [Serializable]
    public abstract class BaseSaveData {

        public string Serialize(bool preserveReferences) {
            Formatting formatting = SaveController.DebugModeSave ? Formatting.Indented : Formatting.None;
            string save = JsonConvert.SerializeObject(this, formatting,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = preserveReferences ?
                                            PreserveReferencesHandling.Objects : PreserveReferencesHandling.None,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    }
            );
            return save;
        }

        public static T Deserialize<T>(string save) {
            T state = JsonConvert.DeserializeObject<T>(save, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            });
            return state;
        }
    }
    [Serializable]
    public class LinkedSaves : BaseSaveData {
        public PlayerControllerSave player;
        public WorldSaveState world;
        public GameEventSave events;
        public UIControllerSave ui;

        public LinkedSaves() {
        }
        public LinkedSaves(PlayerControllerSave player, WorldSaveState world, GameEventSave events, UIControllerSave ui) {
            this.player = player;
            this.world = world;
            this.events = events;
            this.ui = ui;
        }
    }
}
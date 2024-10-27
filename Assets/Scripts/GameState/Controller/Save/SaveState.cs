using Newtonsoft.Json;
using System;
namespace Andja.Controller {
    [Serializable]
    public class SaveState {
        public string gamedata;
        public string linkedsave;
        public string camera;
        public string fw;

        public static SaveState GetDebugSaveStateFromSave(string save) {
            SaveState state = new SaveState();
            string[] lines = save.Split(new string[] { "##" + Environment.NewLine }, StringSplitOptions.None);
            int i = 0;
            foreach (System.Reflection.FieldInfo field in typeof(SaveState).GetFields()) {
                field.SetValue(state, lines[i]);
                i++;
            }
            return state;
        }
        public static SaveState GetSaveStateFromSave(string save) {
            return JsonConvert.DeserializeObject<SaveState>(save, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
            });
        }

        internal string SerializeDebugFormat() {
            string save = "";
             foreach (System.Reflection.FieldInfo field in typeof(SaveState).GetFields()) {
                string bsd = field.GetValue(this) as string;
                save += bsd;
                save += "##" + Environment.NewLine;
            }
            return save;
        }

        internal string Serialize() {
            return JsonConvert.SerializeObject(this, Formatting.Indented,
                        new JsonSerializerSettings {
                            NullValueHandling = NullValueHandling.Ignore,
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            TypeNameHandling = TypeNameHandling.Auto
                        }
                );
        }

        internal static SaveState CreateNew() {
            return new SaveState() {
                gamedata = GameData.Instance.GetSaveGameData().Serialize(false),
                linkedsave = new LinkedSaves(
                    PlayerController.Instance.GetSavePlayerData(),
                    WorldController.Instance.GetSaveWorldData(),
                    EventController.Instance.GetSaveGameEventData(),
                    UIController.Instance.GetUISaveData()
                    ).Serialize(true),
                camera = CameraController.Instance.GetSaveCamera().Serialize(false),
                fw = FogOfWarController.FogOfWarOn ? FogOfWarController.Instance.GetFogOfWarSave().Serialize(false) : null,
            };
        }
    }
}
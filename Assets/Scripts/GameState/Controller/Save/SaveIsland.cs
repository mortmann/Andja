using Andja.Model;
using Andja.Utility;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Andja.Controller {
    [JsonObject]
    public class SaveIsland {
        [JsonPropertyAttribute] public int Width;
        [JsonPropertyAttribute] public int Height;
        [JsonPropertyAttribute] public Climate climate;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.None)] public LandTile[] tiles;
        [JsonPropertyAttribute(TypeNameHandling = TypeNameHandling.Auto)] public List<Structure> structures;
        [JsonPropertyAttribute] public Dictionary<string, Range> Resources;

        [JsonIgnore] public string Name; // for loading in image or similar things
        [JsonPropertyAttribute] public List<IslandFeature> features;

        public SaveIsland() {
        }

        public SaveIsland(List<Structure> structures, Tile[] tiles, int Width, int Height, Climate climate, Dictionary<string, Range> Resources) {
            this.Width = Width;
            this.Height = Height;
            this.climate = climate;
            this.structures = new List<Structure>(structures);
            this.tiles = tiles.Cast<LandTile>().ToArray();
            this.Resources = new Dictionary<string, Range>();
            foreach (string id in Resources.Keys) {
                if (Resources[id].upper <= 0)
                    continue;
                this.Resources[id] = Resources[id];
            }
        }
        public string Serialize() {
            return JsonConvert.SerializeObject(this, Application.isEditor ? Formatting.Indented : Formatting.None,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    //PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                }
            );
        }

        internal static SaveIsland Deserialize(string saveStatePath) {
            try {
                return JsonConvert.DeserializeObject<SaveIsland>(
                    FileUtil.Unzip(File.ReadAllBytes(saveStatePath)),
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }
                );
            }
            catch {
                return JsonConvert.DeserializeObject<SaveIsland>(SaveController.GetIslandSaveFile(saveStatePath),
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                    });
            }
        }
    }
}
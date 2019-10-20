using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class StructureSpriteController : MonoBehaviour {
    public Dictionary<Structure, GameObject> structureGameObjectMap;
    public Dictionary<Structure, GameObject> structureExtraUIMap;
    public readonly static string EffectFilePath = "Textures/Effects/Structures/";
    public Dictionary<string, Sprite> structureSprites = new Dictionary<string, Sprite>();
    public Sprite circleSprite;
    public Sprite upgradeSprite;
    public Sprite unitCircleSprite;

    BuildController bm;
    CameraController cc;
    World World {
        get { return World.Current; }
    }
    void Start() {
        structureGameObjectMap = new Dictionary<Structure, GameObject>();
        structureExtraUIMap = new Dictionary<Structure, GameObject>();

        LoadSprites();
        cc = CameraController.Instance;
        if (EditorController.IsEditor) {
            EditorController.Instance.RegisterOnStructureDestroyed(OnTileStructureDestroyed);
        }
    }

    void Update() {
        List<Structure> ts = new List<Structure>(structureGameObjectMap.Keys);
        foreach (Structure str in ts) {
            if (cc.structureCurrentInCameraView.Contains(str) == false) {
                GameObject.Destroy(structureGameObjectMap[str]);
                structureGameObjectMap.Remove(str);
            }
        }
        foreach (Structure str in cc.structureCurrentInCameraView) {
            if (structureGameObjectMap.ContainsKey(str) == false) {
                OnStrucutureCreated(str);
            }
        }
    }
    public void OnStrucutureCreated(Structure structure) {
        GameObject go = new GameObject();
        structure.RegisterOnChangedCallback(OnStructureChanged);
        structure.RegisterOnDestroyCallback(OnStructureDestroyed);
        structure.RegisterOnExtraUICallback(OnStructureExtraUI);
        structure.RegisterOnEffectChangedCallback(OnStructureEffectChange);
        float x = 0;
        float y = 0;
        if (structure.TileWidth > 1) {
            x = 0.5f + ((float)structure.TileWidth) / 2 - 1;
        }
        if (structure.TileHeight > 1) {
            y = 0.5f + ((float)structure.TileHeight) / 2 - 1;
        }
        Tile t = structure.BuildTile;
        go.transform.position = new Vector3(t.X + x, t.Y + y);
        go.transform.transform.eulerAngles = new Vector3(0, 0, 360 - structure.rotated);
        go.transform.SetParent(this.transform, true);
        go.name = structure.SmallName + "_" + structure.myStructureTiles[0].ToString();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Structures";
        structureGameObjectMap.Add(structure, go);
        if (structure is RoadStructure) {
            ((RoadStructure)structure).RegisterOnRoadCallback(OnRoadChange);
            GameObject gos = new GameObject();
            TextMesh text = gos.AddComponent<TextMesh>();
            text.characterSize = 0.1f;
            text.anchor = TextAnchor.MiddleCenter;

            gos.transform.SetParent(go.transform);
            gos.transform.localPosition = Vector3.zero;
            gos.GetComponent<MeshRenderer>().sortingLayerName = "StructuresUI";
            if (((RoadStructure)structure).Route != null) {
                //				text.text = ((Road)structure).Route.toString ();
            }
            Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            text.font = ArialFont;
        }

        SetSpriteRendererStructureSprite(go, structure);

        if (structure is OutputStructure && ((OutputStructure)structure).ContactRange > 0) {
            GameObject goContact = new GameObject();
            CircleCollider2D cc2d = goContact.AddComponent<CircleCollider2D>();
            cc2d.radius = ((OutputStructure)structure).ContactRange;
            cc2d.isTrigger = true;
            goContact.transform.SetParent(go.transform);
            goContact.transform.localPosition = Vector3.zero;
            ContactColliderScript c = goContact.AddComponent<ContactColliderScript>();
            c.contact = ((OutputStructure)structure);
            goContact.name = "ContactCollider";
        }

        if (structure.HasHitbox) {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
        }
        if (structure.ReadOnlyEffects != null) {
            foreach(Effect e in structure.ReadOnlyEffects) {
              OnStructureEffectChange(structure, e, true);
            }
        }
        
    }

    private void OnStructureEffectChange(IGEventable target, Effect effect, bool added) {
        if (effect.OnMapSpriteName == null || effect.OnMapSpriteName.Length == 0)
            return;
        Structure structure = target as Structure;
        if (structure == null || structureGameObjectMap.ContainsKey(structure) == false)
            return;
        GameObject strgo = structureGameObjectMap[structure];
        if (added == false) {
            EffectAnimator[] effectsanimators = strgo.GetComponentsInChildren<EffectAnimator>();
            if (effectsanimators == null || effectsanimators.Length == 0)
                return;
            EffectAnimator removeEffect = Array.Find<EffectAnimator>(effectsanimators, x => x.effect.ID == effect.ID);
            if (removeEffect == null)
                return;
            Destroy(removeEffect.gameObject);
        }
        else {
            GameObject effectGO = new GameObject();
            effectGO.transform.SetParent(strgo.transform);
            effectGO.transform.localPosition = new Vector3(0, 0, 0);
            EffectAnimator ea = effectGO.AddComponent<EffectAnimator>();
            string path = EffectFilePath + effect.OnMapSpriteName + "_" + structure.SpriteName;
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites.Length == 0)
                return;
            ea.Show(sprites, 0.25f, "Structures",effect);
        }
    }

    void OnStructureChanged(Structure structure) {
        if (structure == null) {
            Debug.LogError("Structure change and its empty?");
            return;
        }
        if (structureGameObjectMap.ContainsKey(structure) == false) {
            //			Debug.LogError ("StructureSprite not in the Map to a gameobject! "+ structure.SmallName+"@"+ structure.myBuildingTiles[0].toString ());
            return;
        }
        if (structure is GrowableStructure) {
            SpriteRenderer sr = structureGameObjectMap[structure].GetComponent<SpriteRenderer>();
            if (structureSprites.ContainsKey(structure.SmallName + "_" + ((GrowableStructure)structure).currentStage))
                sr.sprite = structureSprites[structure.SmallName + "_" + ((GrowableStructure)structure).currentStage];
        }
        else if (structure is HomeStructure) {
            SetSpriteRendererStructureSprite(structureGameObjectMap[structure], structure);
        }
    }
    public void OnStructureExtraUI(Structure structure, bool show) {
        if (show) {
            if (structureExtraUIMap.ContainsKey(structure))
                return;
            GameObject extraUI = null;
            switch (structure.ExtraUITyp) {
                case ExtraUI.None:
                    return;
                case ExtraUI.Range:
                    extraUI = CreateRange(structure);
                    break;
                case ExtraUI.Efficiency:
                    //GameObject extra = GameObject.Instantiate (Resources.Load<GameObject> ("Prefabs/GamePrefab/SpriteSlider"));
                    break;
                case ExtraUI.Upgrade:
                    extraUI = CreateUpgrade(structure);
                    break;
            }
            if (extraUI == null)
                Debug.LogError("No Extra UI to Show was created for type " + structure.ExtraUITyp);
            structureExtraUIMap.Add(structure, extraUI);
        }
        else {
            //Not showing it anymore so delete it
            if (structureExtraUIMap.ContainsKey(structure))
                Destroy(structureExtraUIMap[structure]);
        }
    }

    private GameObject CreateUpgrade(Structure structure) {
        GameObject go = new GameObject {
            name = "UpgradeUI"
        };
        go.transform.position = structureGameObjectMap[structure].transform.position;
        go.transform.localScale = new Vector3(1, 1, 0);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = upgradeSprite;
        sr.sortingLayerName = "StructuresUI";
        go.transform.SetParent(structureGameObjectMap[structure].transform);
        return go;
    }

    private GameObject CreateRange(Structure structure) {
        GameObject go = new GameObject {
            name = "RangeUI"
        };
        go.transform.position = structureGameObjectMap[structure].transform.position;
        go.transform.localScale = new Vector3(((OutputStructure)structure).ContactRange, ((OutputStructure)structure).ContactRange, 0);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.sortingLayerName = "StructuresUI";
        go.transform.SetParent(structureGameObjectMap[structure].transform);
        return go;
    }

    void SetSpriteRendererStructureSprite(GameObject go, Structure str) {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (structureSprites.ContainsKey(str.GetSpriteName())) {
            sr.sprite = structureSprites[str.GetSpriteName()];
        }
        else {
            sr.sprite = structureSprites["nosprite"];
            go.transform.localScale = new Vector3(str.TileWidth, str.TileHeight);
            go.transform.localRotation = Quaternion.identity;
        }
    }

    Sprite GetSprite(string name) {
        if (structureSprites.ContainsKey(name)) {
            return structureSprites[name];
        }
        else {
            return structureSprites["nosprite"];
        }
    }
    void OnTileStructureDestroyed(Tile t) {
        OnStructureDestroyed(t.Structure);
    }
    void OnStructureDestroyed(Structure structure) {
        if (structureGameObjectMap.ContainsKey(structure) == false) {
            return;
        }
        GameObject go = structureGameObjectMap[structure];
        GameObject.Destroy(go);
        structure.UnregisterOnChangedCallback(OnStructureChanged);
        structure.UnregisterOnDestroyCallback(OnStructureDestroyed);
        structure.UnregisterOnExtraUICallback(OnStructureExtraUI);

        structureGameObjectMap.Remove(structure);
    }

    public void OnRoadChange(RoadStructure road) {
        Structure s = road;
        SetSpriteRendererStructureSprite(structureGameObjectMap[s], s);
        if (road.Route != null) {
            structureGameObjectMap[s].GetComponentInChildren<TextMesh>().text = road.Route.ToString();
        }
    }

    void LoadSprites() {
        structureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Structures/");
        foreach (Sprite s in sprites) {
            structureSprites[s.name] = s;
        }
        Sprite[] custom = CustomSpriteLoader.Load("Structures");
        if (custom == null)
            return;
        foreach (Sprite s in custom) {
            structureSprites[s.name] = s;
        }
    }
    public Sprite GetStructureSprite(Structure str) {
        if (structureSprites.ContainsKey(str.GetSpriteName()) == false) {
            //FIXME this should be active in future 
            //fornow there arent many sprites anyway
            //			Debug.LogError ("No Structure Sprite for that Name!");
            return null;
        }
        return GetSprite(str.GetSpriteName());
    }
    public GameObject GetGameObject(Structure str) {
        if (structureGameObjectMap.ContainsKey(str) == false) {
            return null;
        }
        return structureGameObjectMap[str];
    }
}

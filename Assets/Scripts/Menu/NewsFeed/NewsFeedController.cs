using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class NewsFeedController : MonoBehaviour {
    const string file = "news";
    public Transform NewsFeedContent;
    public NewsFeedItem NewsItemPrefab;
    Dictionary<string, NewsFeedItem> pasteToNewsItem = new Dictionary<string, NewsFeedItem>();
    Dictionary<string, NewsItem> urlToNewsItems = new Dictionary<string, NewsItem>();
    string path;

    void Start() {
        path = Path.Combine(Application.temporaryCachePath, file);
        foreach (Transform t in NewsFeedContent)
            Destroy(t.gameObject);
        //File.Delete(path);
        LoadNews();
        StartCoroutine(GetNews());
    }

    private void LoadNews() {
        if(File.Exists(path))
            urlToNewsItems = JsonConvert.DeserializeObject<Dictionary<string, NewsItem>>(File.ReadAllText(path));
        foreach(NewsItem ni in urlToNewsItems.Values)
            CreateNewsItem(ni);
    }

    private const string listNewsPasteURL = "https://pastebin.com/raw/uY2sEy4n";
    IEnumerator GetNews() {
        if (Application.internetReachability == NetworkReachability.NotReachable)
            yield break;
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            yield break;

        UnityWebRequest www = UnityWebRequest.Get(listNewsPasteURL);
        yield return www.SendWebRequest();
        if (www.error != null) {
            gameObject.SetActive(false);
            yield return null;
        }
        string[] allUrls = www.downloadHandler.text.Split();
        foreach(string s in pasteToNewsItem.Keys.ToArray()) {
            if(Array.Exists(allUrls, x => s.Equals(x)) == false) {
                Destroy(pasteToNewsItem[s].gameObject);
                pasteToNewsItem.Remove(s);
                urlToNewsItems.Remove(s);
            }
        }
        www.Dispose();
        for (int i = 0; i < allUrls.Length; i++) {
            string url = allUrls[i];
            if (string.IsNullOrWhiteSpace(url))
                continue;
            www = UnityWebRequest.Get("https://pastebin.com/raw/"+url);
            yield return www.SendWebRequest();
            if(pasteToNewsItem.ContainsKey(url)) {
                urlToNewsItems[url].Check(www.downloadHandler.text, i, pasteToNewsItem);
            } else {
                urlToNewsItems[url] = new NewsItem() {
                    order = i,
                    text = www.downloadHandler.text,
                    url = url
                };
                CreateNewsItem(urlToNewsItems[url]);
            }
            www.Dispose();
        }
        yield return null;
    }

    private void CreateNewsItem(NewsItem ni) {
        NewsFeedItem NewsItem = Instantiate(NewsItemPrefab);
        NewsItem.Show(ni.text);
        NewsItem.transform.SetParent(NewsFeedContent, false);
        NewsItem.transform.SetSiblingIndex(ni.order);
        pasteToNewsItem[ni.url] = NewsItem;
    }

    private void OnDisable() {
        string text = JsonConvert.SerializeObject(urlToNewsItems);
        if (File.Exists(path) && text.Equals(File.ReadAllText(path)))
            return;
        File.WriteAllText(path, text);
    }

    [JsonObject]
    class NewsItem {
        public int order;
        public string url;
        public string text;

        internal void Check(string newText, int i, Dictionary<string, NewsFeedItem> pasteToNewsItem) {
            if(order != i) {
                order = i;
                pasteToNewsItem[url].transform.SetSiblingIndex(order);
            }
            if (text.Equals(newText) == false) {
                text = newText;
                pasteToNewsItem[url].Show(text);
            }
        }

    }

}

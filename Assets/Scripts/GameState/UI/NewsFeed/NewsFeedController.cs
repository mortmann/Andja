using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NewsFeedController : MonoBehaviour {

    public Transform NewsFeedContent;
    public NewsFeedItem NewsItemPrefab;

    void Start() {
        foreach (Transform t in NewsFeedContent)
            Destroy(t.gameObject);
        StartCoroutine(GetNews());
    }

    private const string listNewsPasteURL = "https://pastebin.com/raw/uY2sEy4n";
    List<string> allNews = new List<string>();
    IEnumerator GetNews() {
        UnityWebRequest www = UnityWebRequest.Get(listNewsPasteURL);
        yield return www.SendWebRequest();
        if (www.error != null) {
            gameObject.SetActive(false);
            yield return null;
        }
        string[] allUrls = www.downloadHandler.text.Split();
        foreach(string url in allUrls) {
            if (string.IsNullOrWhiteSpace(url))
                continue;
            www = UnityWebRequest.Get("https://pastebin.com/raw/"+url);
            yield return www.SendWebRequest();
            allNews.Add(www.downloadHandler.text);
        }
        www.Dispose();
        for (int i = 0; i < allNews.Count; i++) {
            NewsFeedItem NewsItem = Instantiate(NewsItemPrefab);
            NewsItem.Show(allNews[i]);
            NewsItem.transform.SetParent(NewsFeedContent, false);
        }
        yield return null;
    }



}

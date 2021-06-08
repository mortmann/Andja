using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using Andja.Controller;
using System.Text.RegularExpressions;

public class YouTrackHandler {
    private static readonly string key = "Bearer perm:QWpld2FvZQ==.NDYtMQ==.LIUrr5A9iYBrvbQEdiL8deY6YSKpiQ";//"sadasdads";//
    private static readonly string apiURL = "https://andja.myjetbrains.com/youtrack/api/issues";//"ptsv2.com/t/6uve1-1623018807/post";//
    private static readonly string attachmentURL = "https://andja.myjetbrains.com/youtrack/api/issues/{0}/attachments";
    public static IEnumerator SendReport(string title, string desc, string[] labels, Texture2D[] images,
            string logFile, string metaData, string saveFile, string priority, Action OnSuccess, Action<string> OnFailure) {
        string json = JsonConvert.SerializeObject(new Issue() {
            summary = title,
            description = desc,
            tags = Convert(labels),
            customFields = new CustomField[] { new SingleEnumIssueCustomField() { name = "Priority", value = new Value(priority) } },
    }, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
        json = json.Replace("YouTrackHandler+SingleEnumIssueCustomField, Assembly-CSharp", "SingleEnumIssueCustomField");
        byte[] array = Encoding.ASCII.GetBytes(json);
        UnityWebRequest www = UnityWebRequest.Put(apiURL, array);
        www.method = "POST";
        www.SetRequestHeader("Accept", "application/json");
        www.SetRequestHeader("Authorization", key);
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();
        if (www.error != null) {
            // Error 
            Debug.LogError(www.error + " " + www.downloadHandler.text);
            OnFailure(www.error);
            www.Dispose();
            yield break;
        }
        string id = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text)["id"].ToString();
        string finalURL = string.Format(attachmentURL, id);
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        byte[] bytes = UnityWebRequest.GenerateBoundary();
        for (int i = 0; i < images.Length; i++) {
            if(images[i] != null) {
                formData.Add(new MultipartFormFileSection("upload=@", images[i].EncodeToJPG(), 
                                                    "screenshot_" + i + ".jpg", "image/jpeg"));
            }
        }
        string name = Regex.Replace(title, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        if (saveFile != null) {
            formData.Add(new MultipartFormFileSection("upload=@", Encoding.ASCII.GetBytes(saveFile), name+ ".sav", "text/plain"));
            formData.Add(new MultipartFormFileSection("upload=@", Encoding.ASCII.GetBytes(metaData), name+ ".meta", "text/plain"));
        }
        if (logFile != null) {
            formData.Add(new MultipartFormFileSection("upload=@", Encoding.ASCII.GetBytes(logFile), name+".log", "text/plain"));
        }
        if(formData.Count > 0) {
            UnityWebRequest attach = UnityWebRequest.Post(finalURL, formData, bytes);
            attach.method = "POST";
            attach.SetRequestHeader("Accept", "application/json");
            attach.SetRequestHeader("Authorization", key);
            attach.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + Encoding.UTF8.GetString(bytes));
            yield return attach.SendWebRequest();
            attach.Dispose();
        }
        www.Dispose();
        OnSuccess();
        yield break;
    }

    private static string Convert(string labels) {
            switch (labels) {
                case "Game":
                    return "6-3";

                case "UI":
                    return "6-4";

                case "AI":
                    return "6-5";

                case "Balance":
                    return "6-6";

                case "Request":
                    return "6-7";

                case "Other":
                    return "6-8";

                case "Pathfinding":
                    return "6-9";

                default:
                    Debug.LogError("Label not defined");
                    return "6-2";
            }
    }

    private static Label[] Convert(string[] labels) {
        Label[] convert = new Label[labels.Length];
        for (int i = 0; i < labels.Length; i++) {
            switch (labels[i]) {
                case "Game":
                    convert[i] = new Label() { id = "6-3" };
                    break;

                case "UI":
                    convert[i] = new Label() { id = "6-4" };
                    break;

                case "AI":
                    convert[i] = new Label() { id = "6-5" };
                    break;

                case "Balance":
                    convert[i] = new Label() { id = "6-6" };
                    break;

                case "Request":
                    convert[i] = new Label() { id = "6-7" };
                    break;

                case "Other":
                    convert[i] = new Label() { id = "6-8" };
                    break;

                case "Pathfinding":
                    convert[i] = new Label() { id = "6-9" };
                    break;

                default:
                    Debug.LogError("Label not defined");
                    break;
            }
        }
        return convert;
    }

    private static void SendFile() {
        
    }

    [JsonObject]
    class Issue {
        public Project project = new Project();
        public string summary = "";
        public string description = "";
        public string GameVersion = Application.version;
        public string PCID = SystemInfo.deviceUniqueIdentifier;
        public Label[] tags;
        public CustomField[] customFields;
}
    [JsonObject]
    class Project {
        public string id = "0-1";
    }
    class Attachment {

    }
    class Label {
        public string id = "";
    }
    class CustomField {
        public string name;
    }
    class SingleEnumIssueCustomField : CustomField {
        public Value value;
    }
    class Value {
        public string name;

        public Value(string name) {
            this.name = name;
        }
    }
}

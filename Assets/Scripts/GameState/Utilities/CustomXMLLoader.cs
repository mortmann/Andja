using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CustomXMLLoader  {

    static readonly string inStreamingAssetsPath = "CustomXML";
    static readonly string customXMLExtension = "*.xml";

    public static void Load(string type, Action<string> readFromXML) {
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath);
        string[] xmls = Directory.GetFiles(fullPath, type + customXMLExtension, SearchOption.AllDirectories);
        foreach (string file in xmls) {
            try {
                if(file == null) {
                    Debug.Log("Loading custom xml failed! Reason: File is empty for " + file + ".");
                    continue;
                }
                try {
                    readFromXML(File.ReadAllText(file));
                } 
                catch {
                    Debug.Log("Loading custom xml failed! Reason: XML in File faulty for " + file + ".");
                }
            }
            catch {
                Debug.Log("Loading custom xml failed! Reason: File could not be read for " + file + ".");
                continue;
            }
        }
    }

}

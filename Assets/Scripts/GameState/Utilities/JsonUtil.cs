﻿using System;
using UnityEngine;

namespace Andja.Utility {

    public class JsonUtil {

        //Usage:
        //YouObject[] objects = JsonHelper.getJsonArray<YouObject> (jsonString);
        public static T[] getJsonArray<T>(string json) {
            //		string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.array;
        }

        //Usage:
        //string jsonString = JsonHelper.arrayToJson<YouObject>(objects);
        public static string arrayToJson<T>(T[] array) {
            Wrapper<T> wrapper = new Wrapper<T> {
                array = array
            };
            return JsonUtility.ToJson(wrapper);
        }

        [Serializable]
        private class Wrapper<T> {
            public T[] array;
        }
    }
}
using System;

public static class ExtensionMethods {
    public static T ToEnum<T>(this string value, bool ignoreCase = true) {
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }
}

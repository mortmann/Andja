namespace Andja.Utility {

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class TextReplace : System.Attribute {
        public string replace;

        public TextReplace(string replace) {
            this.replace = replace;
        }
    }
}
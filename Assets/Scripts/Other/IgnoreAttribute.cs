namespace Andja {

    [System.AttributeUsage(System.AttributeTargets.Field |
                           System.AttributeTargets.Property)
    ]
    public class IgnoreAttribute : System.Attribute {

        public IgnoreAttribute() {
        }
    }
}
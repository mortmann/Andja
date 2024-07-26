using Andja.Model;
using AssertNet.Core.AssertionTypes;
using AssertNet.Core.AssertionTypes.Objects;
using AssertNet.Core.Failures;
using System.Linq;
using UnityEditor.VersionControl;
public static class AssertThatExtension {

    public static void AllItemsAreSame(this EnumerableAssertion<Item> assertion, Item equals) {
        if(assertion.Target.All(item  => Item.AreSame(item, equals)) == false) {
            assertion.FailureHandler.Fail(
                new FailureBuilder("AllItemsAreSame()")
                .AppendEnumerable("Expecting", assertion.Target)
                .Append("to be the same as ", equals)
                .Finish());
        }
    } 
    public static void SameItem(this SingleAssertion assertion, Item equals) {
        if(assertion.Target is Item item && Item.AreSame(item, equals) == false) {
            assertion.FailureHandler.Fail(
                new FailureBuilder("AllItemsAreSame()")
                .Append("Expecting", assertion.Target)
                .Append("to be the same as ", equals)
                .Finish());
        }
    }
}
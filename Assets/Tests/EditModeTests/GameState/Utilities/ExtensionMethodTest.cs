using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Andja.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ExtensionMethodTest {
    public enum Test { T3s7, Testicale, Testeron, Exception }

    [Theory]
    [TestCase("T3s7", Test.T3s7)]
    [TestCase("testicale", Test.Testicale)]
    [TestCase("TESTERON", Test.Testeron)]
    [TestCase("t3S7", Test.T3s7)]
    [TestCase("sadasd", Test.Exception)]
    public void ToEnum(string textEnum, Test test) {
        if(test == Test.Exception) {
            Assert.Catch<System.ArgumentException>(()=>textEnum.ToEnum<Test>());
            return;
        }
        Assert.AreEqual(test, textEnum.ToEnum<Test>());
    }
    [Test]
    public void FloorToInt() {
        Assert.AreEqual(new Vector2(1,1), new Vector2(1.5f, 1.5f).FloorToInt());

    }
    [Test]
    public void CeilToInt() {
        Assert.AreEqual(new Vector2(2, 2), new Vector2(1.5f, 1.5f).CeilToInt());

    }
    [Test]
    public void RoundToInt() {
        Assert.AreEqual(new Vector2(2, 2), new Vector2(1.5f, 1.5f).CeilToInt());
    }
    [Test]
    public void IsFlooredVector() {
        Assert.IsTrue(new Vector2(1.5f, 1.5f).IsFlooredVector(new Vector2(1, 1)));
    }
    [Test]
    public void IsInBounds() {
        Assert.IsTrue(new Vector2(1.5f, 1.5f).IsInBounds(0,0,2,2));
    }
    [Test]
    public void IsInBounds_False() {
        Assert.IsFalse(new Vector2(35f, 1.5f).IsInBounds(0, 0, 2, 2));
    }

    [Test]
    public void GetIntValueXmlNode() {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<test>1</test>");
        Assert.AreEqual(1,xmlDoc.SelectNodes("test").Item(0).GetIntValue());
    }

    [Test]
    public void GetIntValueXmlNode_ThrowsException() {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<test>a</test>");
        Assert.Catch<System.FormatException>(()=> xmlDoc.SelectNodes("test").Item(0).GetIntValue());
    }
    [Test]
    public void GetIntValueXmlElement() {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<test>1</test>");
        Assert.AreEqual(1, xmlDoc.GetElementsByTagName("test").Item(0).GetIntValue());
    }

    [Test]
    public void GetIntValueXmlElement_ThrowsException() {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml("<test>a</test>");
        Assert.Catch<System.FormatException>(() => xmlDoc.GetElementsByTagName("test").Item(0).GetIntValue());
    }

    [Test]
    public void CloneItemArray() {
        var items = new[] { ItemProvider.Brick, ItemProvider.Fish };
        Assert.IsTrue(items.All(x => items.CloneArray().ToList().Exists(y => x.ID == y.ID)));
    }
    [Test]
    public void CloneItemArrayWithCounts() {
        var items = new[] { ItemProvider.Brick_25, ItemProvider.Fish_25 };
        Assert.IsTrue(items.All(x => items.CloneArrayWithCounts().ToList()
                      .Exists(y => x.ID == y.ID && x.count == y.count)));
    }
    [Test]
    public void ItemArrayReplaceKeepCounts() {
        var shouldBeitems = new[] { ItemProvider.Brick_25, ItemProvider.Fish_25, ItemProvider.Tool };
        var items = new[] { ItemProvider.Brick_25, ItemProvider.Fish_25 };
        var newitems = new[] { ItemProvider.Brick, ItemProvider.Fish, ItemProvider.Tool };

        Assert.IsTrue(items.All(x => shouldBeitems.ToList().Exists(y => x.ID == y.ID && x.count == y.count)));
    }
    [Test]
    public void MinBy() {
        Assert.AreEqual(0, new List<int> { 0, 1, 2, 3, 4 }.MinBy(x => x));
    }
    [Test]
    public void MaxBy() {
        Assert.AreEqual(4, new List<int> { 0, 1, 2, 3, 4 }.MaxBy(x => x));
    }
    [Test]
    public void IntClampZero_Negativ() {
        Assert.AreEqual(0, (-1).ClampZero());
    }
    [Test]
    public void IntClampZero_Positiv() {
        Assert.AreEqual(2, 2.ClampZero());
    }
    [Test]
    public void IntClampZero_Maximum() {
        Assert.AreEqual(2, 5.ClampZero(2));
    }

    [Theory]
    [TestCase(1, 0, 1, 0, 0)]
    [TestCase(1, 0, 0, 1, 90)]
    [TestCase(1, 0, -1, 0, 180)]
    [TestCase(1, 0, 0, -1, 270)]
    [TestCase(1, 0, 1, 0, 360)]
    public void Rotate(int startX, int startY, int endX, int endY, float rotate) {
        Assert.AreEqual(new Vector2(endX, endY).RoundToInt(), 
                        new Vector2(startX, startY).Rotate(rotate).RoundToInt());
    }

}

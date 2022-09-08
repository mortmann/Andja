using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Andja.Model;
using Andja.Controller;
using Andja.Utility;
using System.Linq;
using Moq;
using static AssertNet.Assertions;

public class CityInventoryTest {
    CityInventory inventory;
    Item[] buildItems = new[] { ItemProvider.Brick, ItemProvider.Tool, ItemProvider.Wood };
    const int MaxStackSize = 50;
    private MockUtil mockUtil;
    [SetUp]
    public void SetupUp() {
        mockUtil = new MockUtil();

        inventory = new CityInventory(1);
    }
    [Test]
    public void BaseStackSizeIs50() {
        Assert.AreEqual(MaxStackSize, inventory.MaxStackSize);
    }
    [Test]
    public void GetAmountForItem() {
        inventory.Items[ItemProvider.Wood.ID] = ItemProvider.Wood_10;
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
    }

    private void IsInventoryEqual(string id, int amount) {
        AssertThat(inventory.GetAmountFor(id)).IsEqualTo(amount);
    }

    [Test]
    public void GetAllItemsAndRemoveThem() {
        var items = new[] { ItemProvider.Wood_5, ItemProvider.Brick_25, ItemProvider.Fish_25 };
        var additems = new[] { ItemProvider.Wood_5, ItemProvider.Brick_25, ItemProvider.Fish_25 };
        inventory.AddItems(additems);
        var removedItems = inventory.GetAllItemsAndRemoveThem().ToList();
        Assert.IsTrue(items.All(x => removedItems.Exists(y => x.ID == y.ID && x.count == y.count)));
    }

    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, MaxStackSize)]
    [TestCase(100, MaxStackSize)]
    [TestCase(-1, 0)]
    public void AddItem(int amount, int inventoryAmount) {
        Item item = ItemProvider.Wood;
        item.count = amount;
        inventory.AddItem(item);
        IsInventoryEqual(item.ID, inventoryAmount);
    }

    [Test]
    public void AddItem_SingleTwice() {
        inventory.AddItem(ItemProvider.Wood_10);
        inventory.AddItem(ItemProvider.Wood_10);
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count * 2);
    }

    [Test]
    public void AddItem_TwoSingle() {
        inventory.AddItem(ItemProvider.Wood_10);
        inventory.AddItem(ItemProvider.Tool_12);
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
        IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count);
    }

    [Test]
    public void AddItem_TwoSameSingle_ShouldBeMaxStackSize() {
        inventory.AddItem(ItemProvider.Wood_5);
        inventory.AddItem(ItemProvider.Wood_50);
        IsInventoryEqual(ItemProvider.Wood.ID, MaxStackSize);
    }

    [Test]
    public void AddItems_Multiple() {
        Item[] items = { ItemProvider.Wood_10, ItemProvider.Tool_12 };
        inventory.AddItems(items);
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
        IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count);
    }

    [Theory]
    [TestCase(0, 0, 0, 0)]
    [TestCase(MaxStackSize, 25, 49, 14)]
    [TestCase(MaxStackSize, 42, 42, 15)]
    [TestCase(MaxStackSize, MaxStackSize * 2, MaxStackSize, MaxStackSize * 2)]
    public void RemoveItems(int firstInInventory, int firstRemoveAmount, int secondInInventory, int secondRemoveAmount) {
        Item[] items = { ItemProvider.Wood_N(firstInInventory),
                         ItemProvider.Stone_N(secondInInventory),
                         ItemProvider.Tool_12 };
        inventory.AddItems(items);
        bool removed = inventory.RemoveItemsAmount(new[]{ ItemProvider.Wood_N(firstRemoveAmount),
                                            ItemProvider.Stone_N(secondRemoveAmount),
                                            ItemProvider.Tool_5 });
        bool doNotRemove = firstInInventory < firstRemoveAmount || secondInInventory < secondRemoveAmount;
        if(doNotRemove) {
            Assert.IsFalse(removed);
            IsInventoryEqual(ItemProvider.Wood.ID, firstInInventory);
            IsInventoryEqual(ItemProvider.Stone.ID, secondInInventory);
            IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count);
            return;
        } else {
            Assert.IsTrue(removed);
        }
        IsInventoryEqual(ItemProvider.Wood.ID, firstInInventory - firstRemoveAmount);
        IsInventoryEqual(ItemProvider.Stone.ID, secondInInventory - secondRemoveAmount);
        IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count - ItemProvider.Tool_5.count);
    }

    [Test]
    public void GetItemWithMaxAmount_Less() {
        inventory.AddItem(ItemProvider.Wood_50);
        Assert.AreEqual(25, inventory.GetItemWithMaxAmount(ItemProvider.Wood, 25).count);
    }

    [Test]
    public void GetItemWithMaxAmount_More() {
        inventory.AddItem(ItemProvider.Wood_50);
        Assert.AreEqual(50, inventory.GetItemWithMaxAmount(ItemProvider.Wood, 100).count);
    }

    [Test]
    public void HasAnythingOf_Empty() {
        Assert.IsFalse(inventory.HasAnythingOf(ItemProvider.Wood));
    }

    [Test]
    public void HasAnythingOf() {
        inventory.AddItem(ItemProvider.Wood_1);
        Assert.IsTrue(inventory.HasAnythingOf(ItemProvider.Wood));
    }

    [Test]
    public void GetAllAndRemoveItem() {
        Item item = ItemProvider.Wood;
        item.count = inventory.MaxStackSize;
        inventory.AddItem(item);
        Assert.AreEqual(inventory.MaxStackSize, inventory.GetAllAndRemoveItem(ItemProvider.Wood).count);
        Assert.IsFalse(inventory.HasAnythingOf(item));
    }

    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, 25)]
    [TestCase(50, 75)]
    [TestCase(50, -10)]
    public void MoveItem_ToCity(int firstInventory, int moveAmount) {

        CityInventory otherInventory = new CityInventory(42);
        Item item = ItemProvider.Wood_N(firstInventory);
        inventory.AddItem(item);
        inventory.MoveItem(otherInventory, ItemProvider.Wood, moveAmount);
        AssertThat(inventory.GetAmountFor(item)).IsEqualTo((firstInventory - moveAmount.ClampZero()).ClampZero());
        AssertThat(otherInventory.GetAmountFor(item)).IsEqualTo(Mathf.Min(firstInventory, moveAmount.ClampZero()));
    }
    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, 25)]
    [TestCase(50, 42)]
    [TestCase(50, 101)]
    [TestCase(50, -10)]
    public void MoveItem_ToUnit(int firstInventory, int moveAmount) {

        UnitInventory otherInventory = new UnitInventory(6, 50);
        Item item = ItemProvider.Wood;
        item.count = firstInventory;
        inventory.AddItem(item);
        inventory.MoveItem(otherInventory, ItemProvider.Wood, moveAmount);
        Assert.AreEqual((firstInventory - moveAmount.ClampZero()).ClampZero(), inventory.GetAmountFor(item));
        Assert.AreEqual(Mathf.Min(moveAmount.ClampZero(), firstInventory), otherInventory.GetAmountFor(item));
    }
    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, 25)]
    [TestCase(50, 42)]
    [TestCase(50, 160)]
    [TestCase(50, -10)]
    public void RemoveItemAmount(int inInventory, int removeAmount) {
        inventory.AddItem(ItemProvider.Wood_N(inInventory));
        Item remove = ItemProvider.Wood_N(removeAmount);
        bool canBeRemoved = inInventory >= removeAmount && removeAmount > 0;
        Assert.AreEqual(canBeRemoved, inventory.RemoveItemAmount(remove));
        Assert.AreEqual(canBeRemoved ? (inInventory - removeAmount.ClampZero()).ClampZero() : inInventory, inventory.GetAmountFor(remove));
    }

    [Theory]
    [TestCase(0, 0)]
    [TestCase(42, 42)]
    [TestCase(50, 55)]
    public void HasEnoughOfItem(int inInventory, int amount) {
        inventory.AddItem(ItemProvider.Wood_N(inInventory));
        Assert.AreEqual(inInventory >= amount, inventory.HasEnoughOfItem(ItemProvider.Wood_N(amount)));
    }
    [Test]
    public void HasEnoughOfItems_Multiplied_True() {
        inventory.AddItems(new[] { ItemProvider.Wood_N(MaxStackSize), ItemProvider.Stone_N(MaxStackSize) });
        Assert.IsTrue(inventory.HasEnoughOfItems(new[] { ItemProvider.Wood_N(MaxStackSize / 10), ItemProvider.Stone_N(MaxStackSize / 10) }, times: 10));
    }
    [Test]
    public void HasEnoughOfItems_Multiplied_False() {
        inventory.AddItems(new[] { ItemProvider.Wood_N(MaxStackSize), ItemProvider.Stone_N(MaxStackSize) });
        Assert.IsFalse(inventory.HasEnoughOfItems(new[] { ItemProvider.Wood_N(MaxStackSize), ItemProvider.Stone_N(MaxStackSize) }, times: 100));
    }
    [Test]
    public void HasAnything_Yes() {
        inventory.AddItems(new[] { ItemProvider.Wood_N(MaxStackSize), ItemProvider.Stone_N(MaxStackSize) });
        Assert.IsTrue(inventory.HasAnything());
    }
    [Test]
    public void HasAnything_No() {
        Assert.IsFalse(inventory.HasAnything());
    }
    [Test]
    public void GetBuildMaterial() {
        inventory.AddItems(new[] { ItemProvider.Wood_10, ItemProvider.Fish_25, ItemProvider.Tool_12 });
        Assert.IsTrue(inventory.GetBuildMaterial().All(x => buildItems.ToList().Exists(y => x.ID == y.ID)));
    }

    [Test]
    public void AddInventory() {

        UnitInventory otherInventory = new UnitInventory(6, 50);
        var items = new[] { ItemProvider.Wood_50, ItemProvider.Brick_25 };
        otherInventory.AddItems(items.CloneArrayWithCounts());
        inventory.AddInventory(otherInventory);
        Assert.IsTrue(items.All(x => inventory.BaseItems.ToList().Exists(y => x.ID == y.ID && x.count == y.count)));
    }

    [Test]
    public void Load_ItemNotExisting() {
        mockUtil.PrototypControllerMock
            .Setup(p => p.GetItemPrototypDataForID("NOT REAL ANYMORE"))
            .Returns(new ItemPrototypeData() { type = ItemType.Missing });
        inventory.Items["NOT REAL ANYMORE"] = new Item("NOT REAL ANYMORE");
        inventory.Load();

        AssertThat(inventory.Items.Keys).DoesNotContain("NOT REAL ANYMORE");
        AssertThat(inventory.Items.Keys).Contains(ItemProvider.Stone.ID);
    }
    [Test]
    public void Load_NewItem() {
        mockUtil.AllItems.Add("REAL", new Item("REAL", new ItemPrototypeData()));

        inventory.Load();

        AssertThat(inventory.Items.Keys).Contains("REAL");
        AssertThat(inventory.Items.Keys).Contains(ItemProvider.Stone.ID);
    }

    [Test]
    public void GetRemainingSpaceForItem() {
        inventory.Items[ItemProvider.Wood.ID].count = 25;

        AssertThat(inventory.GetRemainingSpaceForItem(ItemProvider.Wood)).IsEqualTo(25);
    }
}

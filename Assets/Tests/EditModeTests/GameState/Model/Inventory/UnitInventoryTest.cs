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
using static AssertNet.Moq.Assertions;

public class UnitInventoryTest {
    const int INVENTORY_MAX_STACK_SIZE = 50;
    const byte INVENTORY_NUMBER_SPACES = 6;
    const int INVENTORY_MAX_ITEM_AMOUNT = INVENTORY_NUMBER_SPACES * INVENTORY_MAX_STACK_SIZE;
    UnitInventory inventory;
    private MockUtil mockUtil;

    [SetUp]
    public void SetupUp() {
        mockUtil = new MockUtil();

        inventory = new UnitInventory(INVENTORY_NUMBER_SPACES, INVENTORY_MAX_STACK_SIZE);
    }

    [Test]
    public void GetAmountForItem() {
        inventory.Items[0] = ItemProvider.Wood_10;
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
    }

    private void IsInventoryEqual(string id, int amount) {
        Assert.AreEqual(amount, inventory.GetAmountFor(id));
    }

    [Test]
    public void GetAllItemsAndRemoveThem(){
        var items = new[] { ItemProvider.Wood_5, ItemProvider.Brick_25, ItemProvider.Fish_25, ItemProvider.Wood_50, ItemProvider.Wood_50 };
        var additems = new[] { ItemProvider.Wood_5, ItemProvider.Brick_25, ItemProvider.Fish_25, ItemProvider.Wood_50, ItemProvider.Wood_50 };
        inventory.AddItems(additems);
        var removedItems = inventory.GetAllItemsAndRemoveThem().ToList();
        Assert.IsTrue(items.All(x=>removedItems.Exists(y=> x.ID == y.ID && x.count == y.count)));
    }

    [Theory]
    [TestCase(0, 0, 0)]
    [TestCase(50, 50, 1)]
    [TestCase(100, 100, 2)]
    [TestCase(INVENTORY_MAX_ITEM_AMOUNT * 2, INVENTORY_MAX_ITEM_AMOUNT, INVENTORY_NUMBER_SPACES)]
    [TestCase(-1, 0, 0)]
    public void AddItem(int amount, int inventoryAmount, int itemCount) {
        Item item = ItemProvider.Wood;
        item.count = amount;
        inventory.AddItem(item);
        IsInventoryEqual(item.ID,inventoryAmount);
        Assert.AreEqual(itemCount, inventory.BaseItems.Count());
    }

    [Test]
    public void AddItem_Empty_ShouldNotHaveItem() {
        inventory.AddItem(ItemProvider.Wood_N(0));
        AssertThat(inventory.Items).AllSatisfy((item) => item == null);
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
    public void AddItem_TwoSameSingle_ShouldBeTwoSlots() {
        inventory.AddItem(ItemProvider.Wood_5);
        inventory.AddItem(ItemProvider.Wood_50);
        IsInventoryEqual(ItemProvider.Wood.ID, 55);
        Assert.AreEqual(2, inventory.BaseItems.Count());
    }

    [Test]
    public void AddItems_Multiple() {
        Item[] items = { ItemProvider.Wood_10, ItemProvider.Tool_12 };
        inventory.AddItems(items);
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
        IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count);
    }

    [Theory]
    [TestCase(0, 0,0,0)]
    [TestCase(50, 25,49,14)]
    [TestCase(50, 42, 42, 15)]
    [TestCase(150, 101, 5,5)]
    public void RemoveItems(int firstInInventory, int firstRemoveAmount, int secondInInventory, int secondRemoveAmount) {
        Item[] items = { ItemProvider.Wood_N(firstInInventory), 
                         ItemProvider.Stone_N(secondInInventory),
                         ItemProvider.Tool_12 };
        inventory.AddItems(items);
        inventory.RemoveItemsAmount(new []{ ItemProvider.Wood_N(firstRemoveAmount),
                                            ItemProvider.Stone_N(secondRemoveAmount), 
                                            ItemProvider.Tool_5 });
        IsInventoryEqual(ItemProvider.Wood.ID, firstInInventory-firstRemoveAmount);
        IsInventoryEqual(ItemProvider.Stone.ID, secondInInventory-secondRemoveAmount);
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
        AssertThat(inventory.Items).AllSatisfy((item) => item == null);
    }

    [Test]
    public void IsFullWithItems() {
        inventory.AddItems(new[] { ItemProvider.Wood_5, ItemProvider.Brick_25, ItemProvider.Fish_25, ItemProvider.Wood_50, ItemProvider.Wood_50 });
        Assert.AreEqual(5, inventory.BaseItems.Count());
        Assert.IsFalse(inventory.AreSlotsFilledWithItems());
        inventory.AddItem(ItemProvider.Tool_5);
        Assert.IsTrue(inventory.AreSlotsFilledWithItems());
        Assert.AreEqual(6, inventory.BaseItems.Count());
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
        item.count = INVENTORY_MAX_ITEM_AMOUNT;
        inventory.AddItem(item);
        Assert.AreEqual(INVENTORY_MAX_ITEM_AMOUNT, inventory.GetAllAndRemoveItem(ItemProvider.Wood).count);
        Assert.IsFalse(inventory.HasAnythingOf(item));
        AssertThat(inventory.Items).AllSatisfy((item) => item == null);
    }

    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, 25)]
    [TestCase(150, 42)]
    [TestCase(150, 101)]
    [TestCase(150, 160)]
    public void MoveItem_Unit(int firstInventory, int moveAmount) {
        UnitInventory otherInventory = new UnitInventory(INVENTORY_NUMBER_SPACES, INVENTORY_MAX_ITEM_AMOUNT);
        Item item = ItemProvider.Wood;
        item.count = firstInventory;
        inventory.AddItem(item);
        inventory.MoveItem(otherInventory, ItemProvider.Wood, moveAmount);
        Assert.AreEqual((firstInventory - moveAmount.ClampZero()).ClampZero(), inventory.GetAmountFor(item));
        Assert.AreEqual(Mathf.Min(firstInventory, moveAmount.ClampZero()), otherInventory.GetAmountFor(item));
    }
    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, 25)]
    [TestCase(50, 75)]
    [TestCase(50, -10)]
    public void MoveItem_ToCity(int firstInventory, int moveAmount) {
        MockUtil mockUtil = new MockUtil();
        var prototypeControllerMock = mockUtil.PrototypControllerMock;
        var buildItems = new Dictionary<string, Item>() {
            { ItemProvider.Brick.ID, ItemProvider.Brick.Clone() },
            { ItemProvider.Tool.ID, ItemProvider.Tool.Clone()   },
            { ItemProvider.Wood.ID, ItemProvider.Wood.Clone()   },
            { ItemProvider.Fish.ID, ItemProvider.Fish.Clone()   },
            { ItemProvider.Stone.ID, ItemProvider.Stone.Clone()   },
        };
        prototypeControllerMock.Setup(p => p.GetCopieOfAllItems()).Returns(buildItems);
        CityInventory otherInventory = new CityInventory(42);
        Item item = ItemProvider.Wood_N(firstInventory);
        inventory.AddItem(item);
        inventory.MoveItem(otherInventory, ItemProvider.Wood, moveAmount);
        Assert.AreEqual((firstInventory - moveAmount.ClampZero()).ClampZero(), inventory.GetAmountFor(item));
        Assert.AreEqual(Mathf.Min(firstInventory, moveAmount.ClampZero()), otherInventory.GetAmountFor(item));
    }

    [Theory]
    [TestCase(0, 0)]
    [TestCase(50, 25)]
    [TestCase(150, 42)]
    [TestCase(150, 160)]
    [TestCase(150, -10)]
    public void RemoveItemAmount(int inInventory, int removeAmount) {
        inventory.AddItem(ItemProvider.Wood_N(inInventory));
        Item remove = ItemProvider.Wood_N(removeAmount);
        bool canBeRemoved = inInventory >= removeAmount && removeAmount > 0;
        Assert.AreEqual(canBeRemoved, inventory.RemoveItemAmount(remove));
        Assert.AreEqual(canBeRemoved? (inInventory - removeAmount.ClampZero()).ClampZero() : inInventory, inventory.GetAmountFor(remove));
    }
    [Test]
    public void RemoveItemAmount_CloneAmount() {
        inventory.AddItem(ItemProvider.Wood_N(50));
        Item remove = ItemProvider.Wood_N(12);
        Assert.AreEqual(true, inventory.RemoveItemAmount(remove, 25));
        Assert.AreEqual(25, inventory.GetAmountFor(remove));
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
        inventory.AddItems(new []{ItemProvider.Wood_N(100), ItemProvider.Stone_N(100)});
        Assert.IsTrue(inventory.HasEnoughOfItems(new []{ItemProvider.Wood_N(5), ItemProvider.Stone_N(5)}, times: 10));
    }
    [Test]
    public void HasEnoughOfItems_Multiplied_False() {
        inventory.AddItems(new []{ItemProvider.Wood_N(100), ItemProvider.Stone_N(100)});
        Assert.IsFalse(inventory.HasEnoughOfItems(new []{ItemProvider.Wood_N(5), ItemProvider.Stone_N(5)}, times: 100));
    }
    [Test]
    public void HasAnything_Yes() {
        inventory.AddItems(new []{ItemProvider.Wood_N(100), ItemProvider.Stone_N(100)});
        Assert.IsTrue(inventory.HasAnything());
    }
    [Test]
    public void HasAnything_No() {
        Assert.IsFalse(inventory.HasAnything());
    }
    [Theory]
    [TestCase(0)]
    [TestCase(42)]
    [TestCase(55)]
    public void GetItemInSpace(int inInventory) {
        inventory.Items[3] = ItemProvider.Stone_N(inInventory);
        Assert.AreEqual(inInventory, inventory.GetItemInSpace(3).count);
    }

    [Theory]
    [TestCase(0)]
    [TestCase(42)]
    [TestCase(55)]
    public void AddItemInSpace(int inInventory) {
        inventory.AddItemInSpace(3,ItemProvider.Stone_N(inInventory));
        Assert.AreEqual(inInventory.ClampZero(INVENTORY_MAX_STACK_SIZE), inventory.Items[3].count);
    }
    [Test]
    public void AddItemInSpace_AlreadyHas() {
        inventory.AddItemInSpace(3, ItemProvider.Stone_N(50));
        inventory.AddItemInSpace(3, ItemProvider.Wood_N(45));
        Assert.AreEqual(ItemProvider.Stone.ID, inventory.Items[3].ID);
        Assert.AreNotEqual(ItemProvider.Wood.ID, inventory.Items[3].ID);
    }
    [Test]
    public void RemoveItemInSpace() {
        inventory.Items[3] = ItemProvider.Stone_25;
        inventory.RemoveItemInSpace(3);
        Assert.IsTrue(inventory.Items[3] == null);
    }
    
    [Test]
    public void RemainingSpaceForItem() {
        inventory.AddItem(ItemProvider.Wood_50);
        Assert.AreEqual(INVENTORY_MAX_ITEM_AMOUNT - ItemProvider.Wood_50.count, inventory.GetRemainingSpaceForItem(ItemProvider.Wood));
    }

    [Test]
    public void GetFilledPercentage() {
        inventory.AddItem(ItemProvider.Wood_N(INVENTORY_MAX_ITEM_AMOUNT / 2));
        Assert.AreEqual(0.5f, inventory.GetFilledPercentage());
    }
    [Test]
    public void AddInventory() {

        UnitInventory otherInventory = new UnitInventory(INVENTORY_NUMBER_SPACES, INVENTORY_MAX_ITEM_AMOUNT);
        var items = new[] { ItemProvider.Wood_50, ItemProvider.Brick_25};
        otherInventory.AddItems(items.CloneArrayWithCounts());
        inventory.AddInventory(otherInventory);
        Assert.IsTrue(inventory.BaseItems.All(x=>items.ToList().Exists(y=> x.ID == y.ID && x.count == y.count)));
    }
    [Test]
    public void Load_ItemNotExisting() {
        mockUtil.PrototypControllerMock
            .Setup(p => p.GetItemPrototypDataForID("NOT REAL ANYMORE"))
            .Returns(new ItemPrototypeData() { type = ItemType.Missing });
        inventory.Items[0] = new Item("NOT REAL ANYMORE", new ItemPrototypeData(){ type = ItemType.Missing });
        inventory.Items[2] = ItemProvider.Stone_1;
        inventory.Items[INVENTORY_NUMBER_SPACES - 1] = new Item("NOT REAL ANYMORE", new ItemPrototypeData() { type = ItemType.Missing });

        inventory.Load();

        AssertThat(inventory.Items[0]).IsNull();
        AssertThat(inventory.Items[2]).IsNotNull();
        AssertThat(inventory.Items[INVENTORY_NUMBER_SPACES - 1]).IsNull();
    }

    [Test]
    public void GetRemainingSpaceForItem() {
        inventory.Items[0] = ItemProvider.Brick_25;

        AssertThat(inventory.GetRemainingSpaceForItem(ItemProvider.Brick)).IsEqualTo(INVENTORY_MAX_ITEM_AMOUNT - 25);
    }
    [Test]
    public void GetRemainingSpaceForItem_DifferentItems() {
        inventory.Items[0] = ItemProvider.Brick_25;
        inventory.Items[1] = ItemProvider.Stone_25;
        inventory.Items[2] = ItemProvider.Wood_1;
        inventory.Items[3] = ItemProvider.Fish_1;
        inventory.Items[4] = ItemProvider.Wood_1;
        inventory.Items[5] = ItemProvider.Wood_1;

        AssertThat(inventory.GetRemainingSpaceForItem(ItemProvider.Brick)).IsEqualTo(25);
    }
    [Test]
    public void GetRemainingSpaceForItem_MultipleSame() {
        inventory.Items[0] = ItemProvider.Brick_25;
        inventory.Items[1] = ItemProvider.Fish_1;
        inventory.Items[2] = ItemProvider.Brick_25;
        inventory.Items[3] = ItemProvider.Fish_1;
        inventory.Items[4] = ItemProvider.Brick_25;
        inventory.Items[5] = ItemProvider.Wood_1;

        AssertThat(inventory.GetRemainingSpaceForItem(ItemProvider.Brick)).IsEqualTo(75);
    }
}

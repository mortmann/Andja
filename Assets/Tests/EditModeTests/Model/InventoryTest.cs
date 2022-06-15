using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Andja.Model;

public class InventoryTest {
    const int INVENTORY_STACK_SIZE = 50;
    const int INVENTORY_NUMBER_SPACES = 6;
    const int INVENTORY_MAX_ITEM_AMOUNT = INVENTORY_NUMBER_SPACES * INVENTORY_STACK_SIZE;
    Inventory inventory;


    [SetUp]
    public void SetupUp() {
        inventory = new Inventory(INVENTORY_NUMBER_SPACES, INVENTORY_STACK_SIZE);
    }

    [Test]
    public void GetAmountForItem() {
        inventory.Items[0+""] = ItemProvider.Wood_10;
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
    }

    private void IsInventoryEqual(string id, int amount) {
        Assert.AreEqual(amount, inventory.GetAmountFor(id));

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
        Assert.AreEqual(itemCount, inventory.Items.Count);
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
        Assert.AreEqual(2, inventory.Items.Count);
    }
    [Test]
    public void AddItems_Multiple() {
        Item[] items = { ItemProvider.Wood_10, ItemProvider.Tool_12 };
        inventory.AddItems(items);
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count);
        IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count);
    }

    [Test]
    public void RemoveItem_Single() {
        Item[] items = { ItemProvider.Wood_10, ItemProvider.Tool_12 };
        inventory.AddItems(items);
        inventory.RemoveItemAmount(ItemProvider.Wood_5);
        IsInventoryEqual(ItemProvider.Wood.ID, ItemProvider.Wood_10.count - ItemProvider.Wood_5.count);
        IsInventoryEqual(ItemProvider.Tool_12.ID, ItemProvider.Tool_12.count);
    }

    [Test]
    public void RemoveItem_Multiple() {
        Item[] items = { ItemProvider.Wood_10, ItemProvider.Tool_12 };
        inventory.AddItems(items);
        inventory.RemoveItemsAmount(new []{ ItemProvider.Wood_5,ItemProvider.Wood_5, ItemProvider.Tool_5 });
        IsInventoryEqual(ItemProvider.Wood.ID, 0);
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
    public void IsFullWithItems() {
        inventory.AddItems(new[] { ItemProvider.Wood_5, ItemProvider.Brick_25, ItemProvider.Fish_25, ItemProvider.Wood_50, ItemProvider.Wood_50 });
        Assert.AreEqual(5, inventory.Items.Count);
        Assert.IsFalse(inventory.IsFullWithItems());
        inventory.AddItem(ItemProvider.Tool_5);
        Assert.IsTrue(inventory.IsFullWithItems());
        Assert.AreEqual(6, inventory.Items.Count);

    }
}

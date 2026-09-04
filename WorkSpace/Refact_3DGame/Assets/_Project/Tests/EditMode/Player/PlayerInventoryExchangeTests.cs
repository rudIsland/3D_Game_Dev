using Characters.Player.Inventory;
using Items;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Player
{
    public sealed class PlayerInventoryExchangeTests
    {
        private ItemDefinition book;
        private ItemDefinition scroll;
        private ItemDefinition potion;

        [SetUp]
        public void SetUp()
        {
            book = CreateItem("Book");
            scroll = CreateItem("Scroll");
            potion = CreateItem("Potion");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(book);
            Object.DestroyImmediate(scroll);
            Object.DestroyImmediate(potion);
        }

        [Test]
        public void CanExchangeItem_WithoutBook_ReturnsFalse()
        {
            var inventory = new PlayerInventory();

            Assert.That(
                inventory.CanExchangeItem(book, scroll),
                Is.False);
        }

        [Test]
        public void TryExchangeItem_WithBook_ReplacesBookWithScroll()
        {
            var inventory = new PlayerInventory();
            Assert.That(inventory.TryAdd(book), Is.True);

            bool exchanged = inventory.TryExchangeItem(book, scroll);

            Assert.That(exchanged, Is.True);
            Assert.That(inventory.HasItem(book), Is.False);
            Assert.That(inventory.HasItem(scroll), Is.True);
            Assert.That(inventory.ItemCount, Is.EqualTo(1));
        }

        [Test]
        public void TryExchangeItem_WithTwoFullSlots_UsesSlotFreedByBook()
        {
            var inventory = new PlayerInventory();
            Assert.That(inventory.TryAdd(book), Is.True);
            Assert.That(inventory.TryAdd(potion), Is.True);

            bool exchanged = inventory.TryExchangeItem(book, scroll);

            Assert.That(exchanged, Is.True);
            Assert.That(inventory.HasItem(book), Is.False);
            Assert.That(inventory.HasItem(scroll), Is.True);
            Assert.That(inventory.HasItem(potion), Is.True);
            Assert.That(inventory.ItemCount, Is.EqualTo(2));
        }

        [Test]
        public void CanExchangeItem_WhenScrollStackIsFull_ReturnsFalse()
        {
            var inventory = new PlayerInventory();
            Assert.That(inventory.TryAdd(book), Is.True);
            Assert.That(inventory.TryAdd(scroll), Is.True);

            Assert.That(
                inventory.CanExchangeItem(book, scroll),
                Is.False);
        }

        [Test]
        public void TryExchangeItem_WhenExchangeFails_DoesNotChangeInventory()
        {
            var inventory = new PlayerInventory();
            Assert.That(inventory.TryAdd(book), Is.True);
            Assert.That(inventory.TryAdd(scroll), Is.True);

            bool exchanged = inventory.TryExchangeItem(book, scroll);

            Assert.That(exchanged, Is.False);
            Assert.That(inventory.GetItem(0), Is.SameAs(book));
            Assert.That(inventory.GetCount(0), Is.EqualTo(1));
            Assert.That(inventory.GetItem(1), Is.SameAs(scroll));
            Assert.That(inventory.GetCount(1), Is.EqualTo(1));
            Assert.That(inventory.ItemCount, Is.EqualTo(2));
        }

        [Test]
        public void TryExchangeItem_WhenExchangeSucceeds_RaisesChangedOnce()
        {
            var inventory = new PlayerInventory();
            Assert.That(inventory.TryAdd(book), Is.True);
            int changedCount = 0;
            inventory.Changed += _ => changedCount++;

            bool exchanged = inventory.TryExchangeItem(book, scroll);

            Assert.That(exchanged, Is.True);
            Assert.That(changedCount, Is.EqualTo(1));
        }

        private static ItemDefinition CreateItem(string name)
        {
            ItemDefinition item =
                ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = name;
            return item;
        }
    }
}

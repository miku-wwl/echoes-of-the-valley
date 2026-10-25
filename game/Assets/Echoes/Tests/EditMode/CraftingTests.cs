using NUnit.Framework;

namespace Echoes.Tests
{
    public class CraftingTests
    {
        static GameSession SessionWith(params ItemAmount[] items)
        {
            GameSession session = new GameSession();
            foreach (ItemAmount item in items) Assert.AreEqual(Outcome.Ok, session.Inventory.TryAdd(item.Id, item.Count));
            return session;
        }

        [Test]
        public void RecipeBook_HasTheThreeDesignedRecipes_WithKnownItems()
        {
            Assert.AreEqual(3, RecipeBook.All.Count);
            Assert.IsNotNull(RecipeBook.Get(RecipeBook.SpringTonic));
            Assert.IsNotNull(RecipeBook.Get(RecipeBook.ChickenFeed));
            Assert.IsNotNull(RecipeBook.Get(RecipeBook.WindmillBraces));
            Assert.IsNull(RecipeBook.Get("recipe_nothing"));

            foreach (Recipe recipe in RecipeBook.All)
            {
                Assert.IsNotEmpty(recipe.Inputs, recipe.Id);
                foreach (ItemAmount input in recipe.Inputs)
                {
                    Assert.IsTrue(ItemCatalog.IsKnown(input.Id), recipe.Id + " input " + input.Id);
                    Assert.Greater(input.Count, 0);
                }
                Assert.IsTrue(ItemCatalog.IsKnown(recipe.Output.Id), recipe.Id + " output");
                Assert.Greater(recipe.Output.Count, 0);
            }
        }

        [Test]
        public void SpringTonic_NeedsTwoHerbsAndOneBlossom_ForTheDesignedQuest()
        {
            Recipe tonic = RecipeBook.Get(RecipeBook.SpringTonic);
            Assert.AreEqual(ItemIds.SpringTonic, tonic.Output.Id);
            Assert.AreEqual(1, tonic.Output.Count);
            Assert.AreEqual(2, System.Array.Find(tonic.Inputs, i => i.Id == ItemIds.Herb).Count, "docs/01: gather 2 herbs");
            Assert.AreEqual(4, RecipeBook.Get(RecipeBook.WindmillBraces).Inputs[0].Count, "docs/01: mine 4 ore");
        }

        [Test]
        public void ValidCraft_ConsumesExactlyTheIngredients_AndGivesTheOutput()
        {
            GameSession session = SessionWith(new ItemAmount(ItemIds.Herb, 3), new ItemAmount(ItemIds.Flower, 2));

            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Flower));
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.SpringTonic));
        }

        [Test]
        public void MissingIngredients_AreRejected_AndNothingIsConsumed()
        {
            GameSession session = SessionWith(new ItemAmount(ItemIds.Herb, 1), new ItemAmount(ItemIds.Flower, 1));

            Assert.AreEqual(Outcome.Insufficient, session.TryCraft(RecipeBook.SpringTonic));

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Flower));
            Assert.AreEqual(0, session.Inventory.Count(ItemIds.SpringTonic));
        }

        [Test]
        public void OutputThatDoesNotFit_IsRejected_AndTheIngredientIsKept()
        {
            // feed is at 8 of 9: crafting three more would overflow the stack, so the blossom must not be spent
            GameSession session = SessionWith(new ItemAmount(ItemIds.Flower, 1), new ItemAmount(ItemIds.ChickenFeed, 8));

            Assert.AreEqual(Outcome.NoRoom, session.TryCraft(RecipeBook.ChickenFeed));

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.Flower));
            Assert.AreEqual(8, session.Inventory.Count(ItemIds.ChickenFeed));
        }

        [Test]
        public void RepeatedSubmission_CannotCraftMoreThanTheIngredientsAllow()
        {
            GameSession session = SessionWith(new ItemAmount(ItemIds.Herb, 2), new ItemAmount(ItemIds.Flower, 1));

            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.SpringTonic));
            Assert.AreEqual(Outcome.Insufficient, session.TryCraft(RecipeBook.SpringTonic), "second press: nothing left to spend");
            Assert.AreEqual(Outcome.Insufficient, session.TryCraft(RecipeBook.SpringTonic));

            Assert.AreEqual(1, session.Inventory.Count(ItemIds.SpringTonic));
            Assert.AreEqual(0, session.Inventory.Count(ItemIds.Herb));
            Assert.AreEqual(0, session.Inventory.Count(ItemIds.Flower));
        }

        [Test]
        public void UnknownRecipe_IsRejected()
        {
            GameSession session = SessionWith(new ItemAmount(ItemIds.Herb, 2));
            Assert.AreEqual(Outcome.UnknownTarget, session.TryCraft("recipe_nothing"));
            Assert.AreEqual(Outcome.UnknownTarget, session.TryCraft(null));
            Assert.AreEqual(2, session.Inventory.Count(ItemIds.Herb));
        }

        [Test]
        public void ChickenFeed_AndWindmillBraces_CraftToTheirDesignedAmounts()
        {
            GameSession session = SessionWith(new ItemAmount(ItemIds.Flower, 1), new ItemAmount(ItemIds.Ore, 4));

            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.ChickenFeed));
            Assert.AreEqual(3, session.Inventory.Count(ItemIds.ChickenFeed));

            Assert.AreEqual(Outcome.Ok, session.TryCraft(RecipeBook.WindmillBraces));
            Assert.AreEqual(1, session.Inventory.Count(ItemIds.WindmillBraces));
            Assert.AreEqual(0, session.Inventory.Count(ItemIds.Ore));
        }

        [Test]
        public void Crafting_NotifiesOnceOnSuccess_AndNotAtAllOnFailure()
        {
            GameSession session = SessionWith(new ItemAmount(ItemIds.Herb, 2), new ItemAmount(ItemIds.Flower, 1));
            int changed = 0;
            int messages = 0;
            session.Changed += () => changed++;
            session.Message += _ => messages++;

            session.TryCraft(RecipeBook.WindmillBraces);   // fails: no ore
            Assert.AreEqual(0, changed);
            Assert.AreEqual(0, messages);

            session.TryCraft(RecipeBook.SpringTonic);
            Assert.AreEqual(1, changed, "one committed craft, one notification");
            Assert.AreEqual(1, messages);
        }
    }
}

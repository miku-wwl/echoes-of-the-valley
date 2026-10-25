using System.Collections.Generic;

namespace Echoes
{
    public sealed class Recipe
    {
        public Recipe(string id, string name, string description, ItemAmount[] inputs, ItemAmount output)
        {
            Id = id;
            Name = name;
            Description = description;
            Inputs = inputs;
            Output = output;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ItemAmount[] Inputs { get; }
        public ItemAmount Output { get; }
    }

    /// <summary>
    /// The three recipes from docs/01-GAME-DESIGN.md. The design fixes the 2 herbs for the Spring Tonic and the 4 ore
    /// for the Windmill Braces; the rest are first-pass tuning: a blossom is the catalyst for tonic, and one blossom
    /// mixes three portions of feed. Windmill Braces are crafted now and delivered in a later phase.
    /// </summary>
    public static class RecipeBook
    {
        public const string SpringTonic = "recipe_spring_tonic";
        public const string ChickenFeed = "recipe_chicken_feed";
        public const string WindmillBraces = "recipe_windmill_braces";

        static readonly Recipe[] all =
        {
            new Recipe(SpringTonic, "Spring Tonic", "Wakes sleeping water. Pour it into the ruined spring.",
                new[] { new ItemAmount(ItemIds.Herb, 2), new ItemAmount(ItemIds.Flower, 1) },
                new ItemAmount(ItemIds.SpringTonic, 1)),
            new Recipe(ChickenFeed, "Chicken Feed", "One blossom makes three portions for Clover.",
                new[] { new ItemAmount(ItemIds.Flower, 1) },
                new ItemAmount(ItemIds.ChickenFeed, 3)),
            new Recipe(WindmillBraces, "Windmill Braces", "Ringing ore struts. Kept for the windmill, a later task.",
                new[] { new ItemAmount(ItemIds.Ore, 4) },
                new ItemAmount(ItemIds.WindmillBraces, 1))
        };

        public static IReadOnlyList<Recipe> All => all;

        public static Recipe Get(string id)
        {
            foreach (Recipe recipe in all)
                if (recipe.Id == id) return recipe;
            return null;
        }
    }
}

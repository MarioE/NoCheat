using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Provides extension methods.
    /// </summary>
    public static class Extensions
    {
        private const string ActiveShopKey = "NoCheat_ItemSpawning_Shop";
        private const string BalanceSheetKey = "NoCheat_ItemSpawning_BalanceSheet";
        private const string ChestItemKey = "NoCheat_ItemSpawning_ChestItem";
        private const string SoldItemsKey = "NoCheat_ItemSpawning_SoldItems";

        private static readonly int[] FragmentItemIds =
        {
            ItemID.FragmentVortex, ItemID.FragmentNebula, ItemID.FragmentSolar, ItemID.FragmentStardust
        };

        private static readonly int[] IronItemIds = {ItemID.IronBar, ItemID.LeadBar};

        private static readonly int[] PressurePlateIds =
        {
            ItemID.RedPressurePlate, ItemID.GreenPressurePlate, ItemID.GrayPressurePlate, ItemID.BrownPressurePlate,
            ItemID.BluePressurePlate, ItemID.YellowPressurePlate, ItemID.LihzahrdPressurePlate
        };

        private static readonly int[] SandItemIds =
        {
            ItemID.SandBlock, ItemID.EbonsandBlock, ItemID.PearlsandBlock, ItemID.CrimsandBlock, ItemID.HardenedSand
        };

        private static readonly int[] WoodItemIds =
        {
            ItemID.Wood, ItemID.Ebonwood, ItemID.RichMahogany, ItemID.Pearlwood, ItemID.Shadewood, ItemID.SpookyWood,
            ItemID.BorealWood, ItemID.PalmWood
        };

        /// <summary>
        ///     Gets the active shop. This is a mechanism for retrieving the shop that a player saw when speaking to an NPC.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The active shop, which may be <c>null</c>.</returns>
        [CanBeNull]
        public static Chest GetActiveShop([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            return player.GetData<Chest>(ActiveShopKey);
        }

        /// <summary>
        ///     Gets the chest item at the specified index. This is a mechanism for retrieving the items for a chest sent to a
        ///     client.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="index">The index, which must be valid.</param>
        /// <returns>The chest item.</returns>
        public static NetItem GetChestItem([NotNull] this TSPlayer player, int index)
        {
            Debug.Assert(player != null, "Player must not be null.");
            Debug.Assert(0 <= index && index < Chest.maxItems, "Index must be valid.");

            return player.GetData<NetItem>(ChestItemKey + index);
        }

        /// <summary>
        ///     Gets or creates the balance sheet for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The balance sheet.</returns>
        [NotNull]
        public static BalanceSheet GetOrCreateBalanceSheet([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            var balanceSheet = player.GetData<BalanceSheet>(BalanceSheetKey);
            if (balanceSheet == null)
            {
                balanceSheet = new BalanceSheet(player);
                player.SetData(BalanceSheetKey, balanceSheet);
            }
            return balanceSheet;
        }

        /// <summary>
        ///     Gets the sold items. This is a mechanism for keeping track of the items that a player has potentially sold to an
        ///     NPC.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The sold items, which may be <c>null</c>.</returns>
        [CanBeNull]
        public static List<NetItem> GetSoldItems([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            return player.GetData<List<NetItem>>(SoldItemsKey);
        }

        /// <summary>
        ///     Gets the possible substitute IDs for the specified ingredient ID.
        /// </summary>
        /// <param name="recipe">The recipe, which must not be <c>null</c>.</param>
        /// <param name="ingredientId">The ingredient ID, which must valid for the recipe.</param>
        /// <returns>The possible substitute IDs.</returns>
        [NotNull]
        public static IEnumerable<int> GetSubstituteIds([NotNull] this Recipe recipe, int ingredientId)
        {
            Debug.Assert(recipe != null, "Recipe must not be null.");
            Debug.Assert(recipe.requiredItem.Any(i => i.type == ingredientId), "Ingredient ID must be valid.");

            if (recipe.anyFragment && FragmentItemIds.Contains(ingredientId))
            {
                return FragmentItemIds;
            }
            if (recipe.anyIronBar && IronItemIds.Contains(ingredientId))
            {
                return IronItemIds;
            }
            if (recipe.anyPressurePlate && PressurePlateIds.Contains(ingredientId))
            {
                return PressurePlateIds;
            }
            if (recipe.anySand && SandItemIds.Contains(ingredientId))
            {
                return SandItemIds;
            }
            if (recipe.anyWood && WoodItemIds.Contains(ingredientId))
            {
                return WoodItemIds;
            }
            foreach (var group in recipe.acceptedGroups)
            {
                var validItemIds = RecipeGroup.recipeGroups[group].ValidItems;
                if (validItemIds.Contains(ingredientId))
                {
                    return validItemIds;
                }
            }
            return new[] {ingredientId};
        }

        /// <summary>
        ///     Sets the active shop. This is a mechanism for storing the shop that a player sees when speaking to an NPC.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="shop">The shop.</param>
        public static void SetActiveShop([NotNull] this TSPlayer player, [CanBeNull] Chest shop)
        {
            Debug.Assert(player != null, "Player must not be null.");

            player.SetData(ActiveShopKey, shop);
        }

        /// <summary>
        ///     Sets the chest item at the specified index. This is a mechanism for storing the items for a chest sent to a client.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="index">The index, which must be valid.</param>
        /// <param name="item">The item.</param>
        public static void SetChestItem([NotNull] this TSPlayer player, int index, NetItem item)
        {
            Debug.Assert(player != null, "Player must not be null.");
            Debug.Assert(0 <= index && index < Chest.maxItems, "Index must be valid.");

            player.SetData(ChestItemKey + index, item);
        }

        /// <summary>
        ///     Sets the sold items. This is a mechanism for keeping track of the items that a player has potentially sold to an
        ///     NPC.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="items">The sold items, which must not be <c>null</c>.</param>
        public static void SetSoldItems([NotNull] this TSPlayer player, [NotNull] List<NetItem> items)
        {
            Debug.Assert(player != null, "Player must not be null.");
            Debug.Assert(items != null, "Items must not be null.");

            player.SetData(SoldItemsKey, items);
        }
    }
}

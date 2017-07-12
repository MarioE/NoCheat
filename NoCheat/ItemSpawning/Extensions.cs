using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using NoCheat.ItemSpawning.Accounting;
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
        private const string BalanceSheetKey = "NoCheat_ItemSpawning_BalanceSheet";
        private const string ChestItemKey = "NoCheat_ItemSpawning_ChestItem";
        private const string DestroyedProjectileIdKey = "NoCheat_ItemSpawning_DestroyedProjectileId";
        private const string LastUpdatedItemKey = "NoCheat_ItemSpawning_LastUpdatedItem";
        private const string ShopKey = "NoCheat_ItemSpawning_Shop";
        private const string WeaponRackItemIdKey = "NoCheat_ItemSpawning_WeaponRackItemId";

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
        ///     Gets the chest item at the specified index.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="index">The index, which must be valid.</param>
        /// <returns>The chest item.</returns>
        [Pure]
        public static NetItem GetChestItem([NotNull] this TSPlayer player, int index)
        {
            Debug.Assert(player != null, "Player must not be null.");
            Debug.Assert(0 <= index && index < Chest.maxItems, "Index must be valid.");

            return player.GetData<NetItem>(ChestItemKey + index);
        }

        /// <summary>
        ///     Gets the destroyed projectile ID for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The destroyed projectile ID.</returns>
        public static int GetDestroyedProjectileId([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            return player.GetData<int>(DestroyedProjectileIdKey);
        }

        /// <summary>
        ///     Gets the last updated item sent by the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The last updated item.</returns>
        public static NetItem GetLastUpdatedItem([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            return player.GetData<NetItem>(LastUpdatedItemKey);
        }

        /// <summary>
        ///     Gets or creates the balance sheet for the specified player.
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
        ///     Gets the shop for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The active shop, which may be <c>null</c>.</returns>
        [CanBeNull]
        [Pure]
        public static Chest GetShop([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            return player.GetData<Chest>(ShopKey);
        }

        /// <summary>
        ///     Gets the possible substitute IDs for the specified ingredient ID in the recipe.
        /// </summary>
        /// <param name="recipe">The recipe, which must not be <c>null</c>.</param>
        /// <param name="ingredientId">The ingredient ID, which must valid for the recipe.</param>
        /// <returns>The possible substitute IDs.</returns>
        [NotNull]
        [Pure]
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
        ///     Gets the weapon rack item ID for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The weapon rack item ID.</returns>
        public static int GetWeaponRackItemId([NotNull] this TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            return player.GetData<int>(WeaponRackItemIdKey);
        }

        /// <summary>
        ///     Sets the chest item at the specified index.
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
        ///     Sets the destroyed projectile ID for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="destroyedProjectileId">The destroyed projectile ID.</param>
        public static void SetDestroyedProjectileId([NotNull] this TSPlayer player, int destroyedProjectileId)
        {
            Debug.Assert(player != null, "Player must not be null.");

            player.SetData(DestroyedProjectileIdKey, destroyedProjectileId);
        }

        /// <summary>
        ///     Sets the last updated item sent by the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="item">The lst updated item.</param>
        public static void SetLastUpdatedItem([NotNull] this TSPlayer player, NetItem item)
        {
            Debug.Assert(player != null, "Player must not be null.");

            player.SetData(LastUpdatedItemKey, item);
        }

        /// <summary>
        ///     Sets the shop for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="shop">The shop.</param>
        public static void SetShop([NotNull] this TSPlayer player, [CanBeNull] Chest shop)
        {
            Debug.Assert(player != null, "Player must not be null.");

            player.SetData(ShopKey, shop);
        }

        /// <summary>
        ///     Sets the weapon rack item ID for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="weaponRackItemId">The weapon rack item ID.</param>
        public static void SetWeaponRackItemId([NotNull] this TSPlayer player, int weaponRackItemId)
        {
            Debug.Assert(player != null, "Player must not be null.");

            player.SetData(WeaponRackItemIdKey, weaponRackItemId);
        }
    }
}

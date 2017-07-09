using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Terraria;
using TShockAPI;
using static Terraria.ID.ItemID;

namespace NoCheat.ItemSpawning
{
    /// <summary>
    ///     Provides extension methods.
    /// </summary>
    public static class Extensions
    {
        private const string BalanceSheetKey = "NoCheat_ItemSpawning_BalanceSheet";
        private const string ChestItemsKey = "NoCheat_ItemSpawning_ChestItems";
        private const string CurrentShopIdKey = "NoCheat_ItemSpawning_CurrentShopId";
        private const string ShopKey = "NoCheat_ItemSpawning_Shop";
        private const string SoldItemsKey = "NoCheat_ItemSpawning_SoldItems";

        private static readonly int[] FragmentItemIds =
        {
            FragmentVortex, FragmentNebula, FragmentSolar, FragmentStardust
        };

        private static readonly int[] IronItemIds = {IronBar, LeadBar};

        private static readonly int[] PressurePlateIds =
        {
            RedPressurePlate, GreenPressurePlate, GrayPressurePlate, BrownPressurePlate, BluePressurePlate,
            YellowPressurePlate, LihzahrdPressurePlate
        };

        private static readonly int[] SandItemIds =
        {
            SandBlock, EbonsandBlock, PearlsandBlock, CrimsandBlock, HardenedSand
        };

        private static readonly int[] WoodItemIds =
        {
            Wood, Ebonwood, RichMahogany, Pearlwood, Shadewood, SpookyWood, BorealWood, PalmWood
        };

        /// <summary>
        ///     Adds a sold item for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        public static void AddSoldItem([NotNull] this TSPlayer player, NetItem item)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var soldItems = player.GetData<List<NetItem>>(SoldItemsKey);
            if (soldItems == null)
            {
                soldItems = new List<NetItem>();
                player.SetData(SoldItemsKey, soldItems);
            }
            soldItems.Add(item);
        }

        /// <summary>
        ///     Gets the chest item for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="index">The index.</param>
        /// <returns>The chest item.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        public static NetItem GetChestItem([NotNull] this TSPlayer player, int index)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            return player.GetData<NetItem>(ChestItemsKey + index);
        }

        /// <summary>
        ///     Gets the current shop ID for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The shop ID.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        public static int GetCurrentShopId([NotNull] this TSPlayer player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            return player.GetData<int>(CurrentShopIdKey);
        }

        /// <summary>
        ///     Gets the possible ingredient IDs for the specified ingredient ID in a recipe.
        /// </summary>
        /// <param name="recipe">The recipe, which must not be <c>null</c>.</param>
        /// <param name="ingredientId">The ingredient ID.</param>
        /// <returns>The ingredient IDs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="recipe" /> is <c>null</c>.</exception>
        [NotNull]
        public static IEnumerable<int> GetIngredientIds([NotNull] this Recipe recipe, int ingredientId)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

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
        ///     Gets or creates the balance sheet for the specified player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The balance sheet.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        [NotNull]
        public static BalanceSheet GetOrCreateBalanceSheet([NotNull] this TSPlayer player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var balanceSheet = player.GetData<BalanceSheet>(BalanceSheetKey);
            if (balanceSheet == null)
            {
                balanceSheet = new BalanceSheet(player);
                player.SetData(BalanceSheetKey, balanceSheet);
            }
            return balanceSheet;
        }

        /// <summary>
        ///     Gets the shop for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The shop, or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        [CanBeNull]
        public static Chest GetShop([NotNull] this TSPlayer player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            return player.GetData<Chest>(ShopKey);
        }

        /// <summary>
        ///     Gets the list of sold items for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <returns>The list of sold items.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        [NotNull]
        public static List<NetItem> GetSoldItems([NotNull] this TSPlayer player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }
            
            return player.GetData<List<NetItem>>(SoldItemsKey) ?? new List<NetItem>();
        }

        /// <summary>
        ///     Sets the chest item for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>The chest item.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        public static void SetChestItem([NotNull] this TSPlayer player, int index, NetItem item)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.SetData(ChestItemsKey + index, item);
        }

        /// <summary>
        ///     Sets the current shop ID for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="shopId">The shop ID.</param>
        /// <exception cref="ArgumentNullException"><paramref name="player" /> is <c>null</c>.</exception>
        public static void SetCurrentShopId([NotNull] this TSPlayer player, int shopId)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.SetData(CurrentShopIdKey, shopId);
        }

        /// <summary>
        ///     Sets the shop for the player.
        /// </summary>
        /// <param name="player">The player, which must not be <c>null</c>.</param>
        /// <param name="shop">The shop.</param>
        /// <exception cref="ArgumentNullException">
        ///     Either <paramref name="player" /> or <paramref name="shop" /> is <c>null</c>.
        /// </exception>
        public static void SetShop([NotNull] this TSPlayer player, Chest shop)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.SetData(ShopKey, shop);
            player.GetSoldItems().Clear();
        }
    }
}

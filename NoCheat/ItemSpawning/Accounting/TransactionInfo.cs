using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Terraria;
using TShockAPI;

namespace NoCheat.ItemSpawning.Accounting
{
    /// <summary>
    ///     Provides extra information about a transaction.
    /// </summary>
    public sealed class TransactionInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransactionInfo" /> class based on the specified player.
        /// </summary>
        /// <param name="player">The player.</param>
        public TransactionInfo([NotNull] TSPlayer player)
        {
            Debug.Assert(player != null, "Player must not be null.");

            NearAlchemyTable = player.TPlayer.alchemyTable;
            QuestFishId = Main.anglerQuestItemNetIDs[Main.anglerQuest];
            var npcIndex = player.TPlayer.talkNPC;
            SelectedItemId = player.SelectedItem.type;
            SelectedSlot = player.TPlayer.selectedItem;
            TalkingToNpcId = npcIndex < 0 ? 0 : Main.npc[npcIndex].type;
        }

        /// <summary>
        ///     Gets or sets the last update.
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        /// <summary>
        ///     Gets a value indicating whether the player was near an alchemy table at the time of the transaction.
        /// </summary>
        public bool NearAlchemyTable { get; }

        /// <summary>
        ///     Gets the quest fish ID at the time of the transaction.
        /// </summary>
        public int QuestFishId { get; }

        /// <summary>
        ///     Gets the selected item ID at the time of the transaction.
        /// </summary>
        public int SelectedItemId { get; }

        /// <summary>
        ///     Gets the selected slot at the time of the transaction.
        /// </summary>
        public int SelectedSlot { get; }

        /// <summary>
        ///     Gets or sets the stage.
        /// </summary>
        public PipelineStage Stage { get; set; } = PipelineStage.Simplifying;

        /// <summary>
        ///     Gets the ID of the NPC the player was talking to at the time of the transaction.
        /// </summary>
        public int TalkingToNpcId { get; }
    }
}

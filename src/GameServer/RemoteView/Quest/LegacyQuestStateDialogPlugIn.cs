﻿// <copyright file="LegacyQuestStateDialogPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest
{
    using System.Linq;
    using System.Runtime.InteropServices;
    using MUnique.OpenMU.GameLogic.Views.Quest;
    using MUnique.OpenMU.GameServer.MessageHandler.Quests;
    using MUnique.OpenMU.Network.Packets.ServerToClient;
    using MUnique.OpenMU.PlugIns;

    /// <summary>
    /// The default implementation of the <see cref="ILegacyQuestStateDialogPlugIn"/> which is forwarding everything to the game client with specific data packets.
    /// </summary>
    [PlugIn("Quest - Status Dialog", "The default implementation of the ILegacyQuestStateDialogPlugIn which is forwarding everything to the game client with specific data packets.")]
    [Guid("F43DA7A9-1ACC-4FBB-9E15-8BE977F4CAF9")]
    public class LegacyQuestStateDialogPlugIn : ILegacyQuestStateDialogPlugIn
    {
        private readonly RemotePlayer player;

        /// <summary>
        /// Initializes a new instance of the <see cref="LegacyQuestStateDialogPlugIn"/> class.
        /// </summary>
        /// <param name="player">The player.</param>
        public LegacyQuestStateDialogPlugIn(RemotePlayer player)
        {
            this.player = player;
        }

        /// <inheritdoc />
        public void Show()
        {
            var questState = this.player.SelectedCharacter.QuestStates.FirstOrDefault(s => s.Group == QuestConstants.LegacyQuestGroup);
            var quest = questState?.ActiveQuest ?? this.player.GetNextLegacyQuest();
            this.player.Connection.SendLegacyQuestStateDialog((byte)(quest?.Number ?? 0), this.player.GetLegacyQuestStateByte());

            if (quest?.RequiredMonsterKills.Any() ?? false)
            {
                using var writer = this.player.Connection.StartWriteLegacyQuestMonsterKillInfo();
                var packet = writer.Packet;
                packet.QuestIndex = (byte)quest.Number;
                int i = 0;
                foreach (var requirement in quest.RequiredMonsterKills)
                {
                    var monsterState = packet[i];
                    monsterState.MonsterNumber = (uint)requirement.Monster.Number;
                    monsterState.KillCount = (uint)(questState?.RequirementStates.FirstOrDefault(r => r.Requirement == requirement)?.KillCount ?? 0);
                    i++;
                }

                writer.Commit();
            }
        }
    }
}
// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugin;

namespace Rulealize.Plugin.Deck
{
    /// <summary>
    /// Piles of cards, in the <c>deck</c> namespace: how many of each are where, and what it
    /// takes to move one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pile is a count per kind of card and never an order. What is on top of a shuffled
    /// deck is not something a rule set may know — a draw enumerates what could come out and
    /// how likely each is, and an order written into the state would be a secret carried in
    /// plain sight. Counts say exactly as much as is knowable, and no less.
    /// </para>
    /// <para>
    /// <b>A deck, a hand, a discard pile and the table are the same kind of field.</b> What
    /// tells them apart is which one a card is in, so every operation here is about moving
    /// cards between two of them rather than about what any of them means.
    /// </para>
    /// <para>
    /// This plugin draws, so it needs a runtime that resolves draws — the same requirement
    /// <see href="https://github.com/reny-develop/Rulealize.Plugin.Chance">Chance</see> has,
    /// and for the same reason: nothing here chooses which card comes out.
    /// </para>
    /// </remarks>
    public sealed class DeckPlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        public PluginManifest Manifest { get; } =
            new("Rulealize.Plugin.Deck", new Version(1, 0, 0), "deck");

        /// <inheritdoc />
        public void Register(IPluginRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            registry.AddSchema("of", DeckSchemaNode.Build);
            registry.AddExpression("count", CountNode.Build);
            registry.AddExpression("size", SizeNode.Build);
            registry.AddExpression("cards", CardsNode.Build);
            registry.AddDraw("draw", DrawNode.Build);
            registry.AddEffect("move", MoveNode.Build);
        }
    }
}

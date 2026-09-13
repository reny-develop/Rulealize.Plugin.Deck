// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Deck
{
    /// <summary>A card off a pile, weighted by what is left in it.</summary>
    /// <remarks>
    /// <para>
    /// <b>Which card, and not the moving of it.</b> Dealing is this inside
    /// <c>deck.move</c>'s <c>card</c>, so the card that comes out is the card that moves —
    /// one draw, evaluated once, used by the one effect that does both halves of taking it
    /// from a pile and putting it in another.
    /// </para>
    /// <para>
    /// That separation is what keeps the two questions apart. A card leaving a pile because
    /// somebody played it is <c>deck.move</c> with an argument, has one outcome, and is not
    /// chance at all; a card leaving because it was drawn is the same effect with this in
    /// place of the argument, and the outcomes are what the pile could have given up.
    /// </para>
    /// <para>
    /// Nothing here chooses. The candidates are the kinds of card the pile holds, weighted by
    /// how many of each are left, and the runtime says which of them this evaluation is for —
    /// so drawing from four aces and one king is two outcomes at four fifths and one fifth,
    /// and a recorded hand replays to the state it was recorded against.
    /// </para>
    /// </remarks>
    internal sealed class DrawNode(ExpressionNode pile) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new DrawNode(context.RequireExpression("of"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            RecordValue held = DeckArguments.RequirePile(pile.Evaluate(context), "deck.draw.of");
            List<DrawCandidate> candidates = [];

            foreach (string card in held.Fields.Keys)
            {
                int count = DeckArguments.Count(held, card, "deck.draw.of");

                if (count > 0)
                {
                    candidates.Add(new DrawCandidate(RuleValue.Text(card), count));
                }
            }

            // Drawing from an empty pile is a fault raised by the runtime, which is where
            // "there is nothing that could come out" is already said for every draw there is.
            return context.Draw(candidates.ToArray(), "deck.draw.of");
        }
    }
}

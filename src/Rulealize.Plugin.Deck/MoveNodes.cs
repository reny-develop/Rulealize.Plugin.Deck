// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Deck
{
    /// <summary>Moves a named card from one pile to another.</summary>
    /// <remarks>
    /// <para>
    /// Playing a card, rather than drawing one: which card is the mover's decision and
    /// arrives as an argument, so nothing is drawn and the transition has one outcome.
    /// </para>
    /// <para>
    /// Moving more than the pile holds is an evaluation fault. A pile going negative is not a
    /// state anybody could mean, and unlike an out-of-range read there is no reading of it
    /// that is useful — so this is strict where <c>deck.count</c> is forgiving, which is the
    /// same split reads and writes have everywhere in this ecosystem.
    /// </para>
    /// </remarks>
    internal sealed class MoveNode(StatePath from, StatePath to, ExpressionNode card, ExpressionNode? count) : EffectNode
    {
        public static EffectNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            (StatePath from, StatePath to) = PileMove.Resolve(context);
            return new MoveNode(from, to, context.RequireExpression("card"), context.OptionalExpression("count"));
        }

        public override void Apply(IEvaluationContext context, IStateDraft draft)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(draft);

            string moved = card.Evaluate(context).AsText("deck.move.card");
            int many = count is null ? 1 : count.Evaluate(context).AsInt32("deck.move.count");

            if (many < 0)
            {
                throw new RuleEvaluationException("deck.move.count", $"A count cannot be negative, but is {many}.");
            }

            RecordValue source = DeckArguments.RequirePile(draft.Get(from), "deck.move.from");
            int held = DeckArguments.Count(source, moved, "deck.move.from");

            if (held < many)
            {
                throw new RuleEvaluationException(
                    "deck.move.card",
                    $"The pile holds {held} of '{moved}', and {many} were moved.");
            }

            draft.Set(from, DeckArguments.With(source, moved, held - many));

            RecordValue target = DeckArguments.RequirePile(draft.Get(to), "deck.move.to");
            draft.Set(to, DeckArguments.With(target, moved, DeckArguments.Count(target, moved, "deck.move.to") + many));
        }
    }

    /// <summary>Resolving the two fields a move is between.</summary>
    internal static class PileMove
    {
        public static (StatePath From, StatePath To) Resolve(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            (StatePath from, DeckSchemaNode source) = DeckArguments.RequirePileField(context, "from");
            (StatePath to, DeckSchemaNode target) = DeckArguments.RequirePileField(context, "to");

            if (from.Equals(to))
            {
                throw context.Error("to", $"'{to}' is the pile the cards are coming from.");
            }

            // A pile may only hold cards it declares, so a move between piles that do not
            // agree about the cards is a state that cannot be written. Said here rather than
            // discovered by the one deal that happens to draw the odd card out.
            foreach (string card in source.Cards)
            {
                if (!target.Declares(card))
                {
                    throw context.Error("to", $"'{to}' does not declare the card '{card}', which '{from}' may hold.");
                }
            }

            return (from, to);
        }
    }
}

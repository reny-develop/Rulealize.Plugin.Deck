// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Deck
{
    /// <summary>How many of one card a pile holds.</summary>
    /// <remarks>
    /// Zero for a card the pile is out of, and zero is a real answer rather than an absence —
    /// which is what lets a guard be written as a comparison instead of a null check.
    /// </remarks>
    internal sealed class CountNode(ExpressionNode pile, ExpressionNode card) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new CountNode(context.RequireExpression("of"), context.RequireExpression("card"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            RecordValue held = DeckArguments.RequirePile(pile.Evaluate(context), "deck.count.of");
            string wanted = card.Evaluate(context).AsText("deck.count.card");

            return RuleValue.Number(DeckArguments.Count(held, wanted, "deck.count.of"));
        }
    }

    /// <summary>How many cards there are in a pile altogether.</summary>
    internal sealed class SizeNode(ExpressionNode pile) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new SizeNode(context.RequireExpression("of"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            RecordValue held = DeckArguments.RequirePile(pile.Evaluate(context), "deck.size.of");
            int total = 0;

            foreach (string card in held.Fields.Keys)
            {
                total += DeckArguments.Count(held, card, "deck.size.of");
            }

            return RuleValue.Number(total);
        }
    }

    /// <summary>The kinds of card a pile actually holds, in declaration order.</summary>
    /// <remarks>
    /// <para>
    /// Cards the pile is out of are left out, which is what makes this the domain of "play a
    /// card from your hand": the parameter offers what is there and the guard does not have
    /// to say so a second time.
    /// </para>
    /// <para>
    /// Declaration order, because <c>GetValidInputs</c> walks a domain to build candidates and
    /// what it returns should not depend on which run it was.
    /// </para>
    /// </remarks>
    internal sealed class CardsNode(ExpressionNode pile) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new CardsNode(context.RequireExpression("of"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            RecordValue held = DeckArguments.RequirePile(pile.Evaluate(context), "deck.cards.of");
            List<RuleValue> present = [];

            foreach (string card in held.Fields.Keys)
            {
                if (DeckArguments.Count(held, card, "deck.cards.of") > 0)
                {
                    present.Add(RuleValue.Text(card));
                }
            }

            return RuleValue.Sequence(present);
        }
    }
}

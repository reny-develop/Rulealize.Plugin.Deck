// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Deck
{
    /// <summary>Reading the arguments every node here takes.</summary>
    internal static class DeckArguments
    {
        /// <summary>Requires a value to be a pile.</summary>
        /// <param name="value">The value.</param>
        /// <param name="origin">Where it came from, for a fault message.</param>
        /// <returns>The pile.</returns>
        /// <exception cref="RuleEvaluationException">The value is not a pile.</exception>
        public static RecordValue RequirePile(RuleValue value, string origin)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value as RecordValue
                ?? throw new RuleEvaluationException(
                    origin,
                    $"Expected a pile of cards but got {RuleValue.Describe(value)}.");
        }

        /// <summary>Reads how many of one card a pile holds.</summary>
        /// <param name="pile">The pile.</param>
        /// <param name="card">The card.</param>
        /// <param name="origin">Where the question came from, for a fault message.</param>
        /// <returns>The count, which is zero for a card the pile is out of.</returns>
        /// <exception cref="RuleEvaluationException">The pile holds something that is not a count.</exception>
        public static int Count(RecordValue pile, string card, string origin)
        {
            ArgumentNullException.ThrowIfNull(pile);
            return pile[card] is NullValue ? 0 : pile[card].AsInt32(origin);
        }

        /// <summary>Resolves a state field that has to be a pile.</summary>
        /// <param name="context">The surrounding build state.</param>
        /// <param name="key">The key naming the field, <c>from</c> or <c>to</c>.</param>
        /// <returns>The path, and the schema that says which cards it may hold.</returns>
        public static (StatePath Path, DeckSchemaNode Schema) RequirePileField(INodeBuildContext context, string key)
        {
            ArgumentNullException.ThrowIfNull(context);

            ExpressionNode target = context.RequireExpression(key);

            if (target is not IStateLocation location)
            {
                throw context.Error(key, "must denote a state field, such as \"$deck\".");
            }

            if (location.Path.Schema is not DeckSchemaNode schema)
            {
                throw context.Error(key, $"'{location.Path}' is not a pile of cards.");
            }

            return (location.Path, schema);
        }

        /// <summary>Writes a pile back with one card's count changed.</summary>
        /// <param name="pile">The pile as it stands.</param>
        /// <param name="card">The card.</param>
        /// <param name="count">What it becomes.</param>
        /// <returns>The new pile.</returns>
        public static RuleValue With(RecordValue pile, string card, int count)
        {
            ArgumentNullException.ThrowIfNull(pile);

            Dictionary<string, RuleValue> counts = new(pile.Fields, StringComparer.Ordinal)
            {
                [card] = RuleValue.Number(count),
            };

            return RuleValue.Record(counts);
        }
    }
}

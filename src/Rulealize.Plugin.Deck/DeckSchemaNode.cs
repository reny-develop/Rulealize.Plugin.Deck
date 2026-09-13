// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Collections.Immutable;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Deck
{
    /// <summary>The schema of a field holding a pile of cards.</summary>
    /// <remarks>
    /// <para>
    /// A pile is <b>how many of each card are in it</b>, and not a stack in an order. Which
    /// card comes off next is not something a rule set may know: a draw enumerates what could
    /// come out and how likely each of those is, and a shuffled order written into the state
    /// would be either a secret the document is carrying in plain sight or a decision nobody
    /// made. Counts say exactly what is knowable.
    /// </para>
    /// <para>
    /// The kinds of card are declared and closed, as <c>rec.map</c>'s keys are and for the
    /// same reason: what is declared can be checked, and a mistyped card is then an error
    /// rather than a pile that silently gained a kind.
    /// </para>
    /// <para>
    /// <b>A deck and a hand are the same thing.</b> So is a discard pile, and so is the
    /// table. What separates them is which field a card is in, which is why the effects here
    /// are written as moves from one field to another rather than as taking and giving.
    /// </para>
    /// <para>
    /// Zero counts are left out of the JSON, the way a board leaves out its empty squares.
    /// A hand of two cards out of a deck of fifty-two then reads as two keys rather than
    /// thirteen, and a state document stays something a person can look at.
    /// </para>
    /// </remarks>
    internal sealed class DeckSchemaNode(ImmutableArray<string> cards) : SchemaNode
    {
        /// <summary>Gets the kinds of card this pile may hold, in declaration order.</summary>
        public ImmutableArray<string> Cards => cards;

        /// <inheritdoc />
        /// <remarks>An empty pile is a pile with nothing in it, which is a value and not an absence.</remarks>
        public override bool IsNullable => false;

        /// <summary><c>deck.of</c>: the kinds of card, and nothing about how many.</summary>
        /// <param name="context">The surrounding build state.</param>
        /// <returns>The schema node.</returns>
        public static SchemaNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            ImmutableArray<string> declared = context.RequireStringArray("cards");

            if (declared.IsEmpty)
            {
                throw context.Error("cards", "must declare at least one kind of card.");
            }

            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (string card in declared)
            {
                if (!seen.Add(card))
                {
                    throw context.Error("cards", $"'{card}' is declared more than once.");
                }
            }

            return new DeckSchemaNode(declared);
        }

        /// <summary>Says whether a kind of card belongs to this pile.</summary>
        /// <param name="card">The card.</param>
        /// <returns><see langword="true"/> when it was declared.</returns>
        public bool Declares(string card) => cards.Contains(card, StringComparer.Ordinal);

        /// <inheritdoc />
        public override void Validate(RuleValue value, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(sink);

            if (value is not RecordValue pile)
            {
                sink.Violation($"Expected a pile of cards but got {RuleValue.Describe(value)}.");
                return;
            }

            foreach (string card in cards)
            {
                if (pile[card] is not NumberValue count)
                {
                    sink.Violation(card, $"Expected a count but got {RuleValue.Describe(pile[card])}.");
                    continue;
                }

                if (count.Value < 0 || decimal.Truncate(count.Value) != count.Value)
                {
                    sink.Violation(card, $"A count is a whole number that is not negative, but is {RuleValue.FormatNumber(count.Value)}.");
                }
            }

            foreach (string card in pile.Fields.Keys)
            {
                if (!Declares(card))
                {
                    sink.Violation(card, "is not a card this pile declares.");
                }
            }
        }

        /// <inheritdoc />
        public override RuleValue ReadJson(JsonElement element, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(sink);

            Dictionary<string, RuleValue> counts = new(cards.Length, StringComparer.Ordinal);

            foreach (string card in cards)
            {
                counts[card] = RuleValue.Number(0);
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                sink.Violation("Expected an object of card counts.");
                return RuleValue.Record(counts);
            }

            foreach (JsonProperty held in element.EnumerateObject())
            {
                if (!Declares(held.Name))
                {
                    sink.Violation(held.Name, "is not a card this pile declares.");
                    continue;
                }

                if (held.Value.ValueKind != JsonValueKind.Number || !held.Value.TryGetDecimal(out decimal count))
                {
                    sink.Violation(held.Name, "Expected a count.");
                    continue;
                }

                counts[held.Name] = RuleValue.Number(count);
            }

            return RuleValue.Record(counts);
        }

        /// <inheritdoc />
        public override void WriteJson(Utf8JsonWriter writer, RuleValue value)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);

            RecordValue pile = DeckArguments.RequirePile(value, "deck");

            writer.WriteStartObject();

            foreach (string card in cards)
            {
                if (pile[card] is NumberValue count && count.Value != 0)
                {
                    writer.WriteNumber(card, count.Value);
                }
            }

            writer.WriteEndObject();
        }
    }
}

# Rulealize.Plugin.Deck

Piles of cards for [Rulealize](https://github.com/reny-develop/Rulealize) rule sets.

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.Deck` |
| Namespace | `deck` |
| Reserved prefix | none |
| Depends on | `Rulealize.Abstraction` |
| Specification | [doc/specification.md](doc/specification.md) |

A pile is **how many of each card are in it**. `deck.of` declares which cards exist,
`deck.move` moves a card from one pile to another, `deck.draw` is the card a pile gives up,
and `deck.count`, `deck.size` and `deck.cards` read a pile.

```jsonc
"state": {
  "schema": {
    "deck": { "op": "deck.of", "cards": ["A", "2", "3", "4", "5", "6", "7", "8", "9", "T"] },
    "hand": { "op": "deck.of", "cards": ["A", "2", "3", "4", "5", "6", "7", "8", "9", "T"] }
  },
  "initial": { "deck": { "A": 4, "2": 4, "3": 4 }, "hand": {} }
},

"inputs": {
  "draw":  { "effects": [ { "op": "deck.move", "from": "$deck", "to": "$hand",
                            "card": { "op": "deck.draw", "of": "$deck" } } ] },
  "play":  { "params": { "card": { "domain": { "op": "deck.cards", "of": "$hand" } } },
             "effects": [ { "op": "deck.move", "from": "$hand", "to": "$table", "card": "@card" } ] }
}
```

## A pile is counts, not an order

What is on top of a shuffled deck is not something a rule set may know. A draw enumerates
what could come out and how likely each of those is; an order written into the state would
be either a secret carried in plain sight or a decision nobody made. **Counts say exactly
what is knowable**, and `deck.draw` weights its candidates by them — so drawing from a deck
holding four aces and one king is five outcomes with the right probabilities, and a search
over the position is the same two calls as a search over a game with no cards in it.

## A deck and a hand are the same kind of field

So are a discard pile and the table. What separates them is which field a card is in, which
is why every operation here moves cards **between two fields** rather than taking and giving.
A rule set with four piles declares four fields of the same shape.

## Why dealing is a draw inside a move

A draw evaluated twice is two draws. "Put the drawn card in the hand" and "take the drawn
card out of the deck" written as two effects would therefore move two different cards — so
the draw is written where the card is wanted, as the `card` of the move, and is evaluated
once. It is also why rule sets written before this plugin did not store a deck at all, and
recomputed what was left from what had been dealt.

## It does not shuffle anything

There is no random number generator in this assembly. `deck.draw` works out the candidates
and their weights and hands them to the runtime, which says which one this evaluation is
for — the same arrangement
[Chance](https://github.com/reny-develop/Rulealize.Plugin.Chance) has, and it needs a runtime
that resolves draws for the same reason.

## Building

`dotnet build`. `Rulealize.Abstraction` restores from nuget.org like any other package, so
this repository builds on its own.

## License

Apache-2.0.

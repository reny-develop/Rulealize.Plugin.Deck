# Rulealize.Plugin.Deck

| | |
| --- | --- |
| Identifier | `Rulealize.Plugin.Deck` |
| Namespace | `deck` |
| Version | `1.0.0` |
| Reserved prefix | none |
| Depends on | [the value model](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/value-model.md), and nothing else |
| Requires | a runtime that resolves draws — `Rulealize.Abstraction` 0.4.0 or later, and a host built on it |
| Notation | [how a plugin specification is written](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/specification-notation.md) |

Piles of cards: how many of each are where, and what it takes to move one.

**Not one card game's vocabulary.** There is no trump, no suit, no rank order and no hand
value — a card is a name the rule set chose, and what any of them means is the rule set's.

## Nodes

| Node | Kind | Form |
| --- | --- | --- |
| `deck.of` | schema | `{ "op": "deck.of", "cards": ["<card>", …] }` — the cards are static |
| `deck.count` | expression | `{ "op": "deck.count", "of": <expression:pile>, "card": <expression:Text> }` |
| `deck.size` | expression | `{ "op": "deck.size", "of": <expression:pile> }` |
| `deck.cards` | expression | `{ "op": "deck.cards", "of": <expression:pile> }` |
| `deck.draw` | draw | `{ "op": "deck.draw", "of": <expression:pile> }` |
| `deck.move` | effect | `{ "op": "deck.move", "from": <state field:pile>, "to": <state field:pile>, "card": <expression:Text>, "count": <expression:Number> }` — `count` optional |

---

## `deck.of`

The schema of a field holding a pile.

`cards` declares which kinds of card the pile may hold. The set is closed and declared, as
[Record](https://github.com/reny-develop/Rulealize.Plugin.Record/blob/main/doc/specification.md)'s
`rec.map` keys are and for the same reason: what is declared can be checked, so a mistyped
card is an error rather than a pile that silently gained a kind.

### The JSON form

A pile is an object of counts, and **zero counts are left out**:

```jsonc
"deck": { "A": 4, "2": 4, "3": 3 },
"hand": { "3": 1 },
"discard": {}
```

The way a board leaves out its empty squares, and for the same reason: a hand of two cards
out of a deck of fifty-two reads as two keys rather than fifty-two, and a state document
stays something a person can look at.

A count that is negative, fractional, or not a number is a violation reported when the state
document is read, along with every other one.

### Why counts and not an order

**What is on top of a shuffled deck is not knowable.** A rule set says what a position is,
and a position where the next card is already decided is one of two things: a secret the
document is carrying in plain sight, or a decision that nobody made and the document
invented. Counts are exactly what is knowable, and they are what a draw needs — the
alternatives and their weights.

This is the same judgement the runtime makes about chance in general: **the alternatives are
enumerated and picking one is the host's**, which is what lets a search walk a deck and a
recorded hand replay to the state it was recorded against.

---

## `deck.count` / `deck.size` / `deck.cards`

```jsonc
{ "op": "deck.count", "of": "$hand", "card": "@card" }   // how many of one card
{ "op": "deck.size",  "of": "$deck" }                    // how many altogether
{ "op": "deck.cards", "of": "$hand" }                    // which cards are in it, in declaration order
```

`deck.count` answers `0` for a card the pile is out of. Zero is a real answer and not an
absence, which is what lets a guard be a comparison rather than a null check first.

`deck.cards` leaves out what the pile is out of, which is what makes it the domain of "play a
card from your hand":

```jsonc
"play": {
  "params": { "card": { "domain": { "op": "deck.cards", "of": "$hand" } } },
  "effects": [ { "op": "deck.move", "from": "$hand", "to": "$table", "card": "@card" } ]
}
```

The parameter then offers what is there, so the guard does not have to say it a second time
and `GetValidInputs` produces one move per card actually held.

The order is the order `deck.of` declared, because `GetValidInputs` walks a domain to build
its candidates and what it returns should not depend on which run it was.

---

## `deck.draw`

A card off a pile, weighted by how many of each are left. **A draw**, so it may only be
written inside an input's `effects` — which is where dealing happens anyway.

### Dealing is `deck.draw` inside `deck.move`

```jsonc
{ "op": "deck.move", "from": "$deck", "to": "$hand",
  "card": { "op": "deck.draw", "of": "$deck" } }
```

One draw, evaluated once, feeding the one effect that does both halves of taking a card from
a pile and putting it in another. **That is why drawing is not an effect of its own**: a draw
evaluated twice is two draws, so "put the drawn card in the hand" and "take the drawn card
out of the deck" as two effects would move two different cards.

Keeping the two apart is worth more than the node it saves. Playing a card is
`deck.move` with an argument and has one outcome; drawing one is the same effect with this in
place of the argument, and the outcomes are what the pile could have given up. The rule set
says which of the two a rule is, in the one place it matters.

### How it draws

The candidates are the kinds of card the pile holds, weighted by how many of each are left,
handed to the runtime. Drawing from four aces and one king is **two** outcomes at four fifths
and one fifth, not five at a fifth each — it is the card that is drawn, and two aces are not
two things that could happen.

Drawing from an empty pile is the fault every draw with nothing to draw from raises.

### Two draws in one input read the same pile

Expressions read the state snapshot, so two `deck.draw` nodes in one set of effects both draw
from the pile as it stood before the input. Dealing two cards at once is therefore an input
that can, in the corner case where both draws land on the last copy of one card, fail the
second `deck.move` — the pile holds none of it by then.

Live with it, and deal one card per input. The alternative was an effect that draws, which
would need the runtime to know an effect can draw, and dealing two cards in one turn is rare
enough that a rule set wanting it can take two turns over it.

---

## `deck.move`

Moves `count` of a named card from `from` to `to`. `count` defaults to one.

Playing a card rather than drawing one: which card is the mover's decision and arrives as an
argument, so **nothing is drawn and the transition has exactly one outcome.**

Moving more than the pile holds is an evaluation fault. A pile going negative is not a state
anybody could mean, and unlike an out-of-range read there is no reading of it that is useful —
so this is strict where `deck.count` is forgiving, which is the split reads and writes have
everywhere in this ecosystem.

---

## Decided

- **No shuffling operation.** A shuffle would have to write an order into the state, which is
  the one thing this plugin refuses to hold. `deck.draw` is a shuffle that never has to be
  written down.
- **No card ordering, ranking or comparison.** What a card is worth is the rule set's, and
  the vocabulary for saying it — `branch.match` over the card, or a `rec.map` from card to
  value — is already there and belongs to the game rather than to the pile.
- **No hand limit on a pile.** A rule that says how many cards a hand may hold is a guard,
  and a guard is where a rule set can say what happens when the limit is reached.
- **No face-up and face-down.** Which cards a player may see is
  [Secret](https://github.com/reny-develop/Rulealize.Plugin.Secret)'s question, and a pile
  that nobody may look into is a pile in a field the screen does not show. Cards do not need
  a second mechanism for it.
- **No node for dealing to several piles at once.** Two deals are two nodes and read better
  than one node taking a sequence of targets would, which is the same call
  [Grid](https://github.com/reny-develop/Rulealize.Plugin.Grid/blob/main/doc/specification.md)
  made about `grid.setEach`.

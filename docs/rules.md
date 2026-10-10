# Qal'a (قلعة): Wells & Walls, Official Rules

**Rules version 0.6** (chosen by the balance lab; see `docs/balance-log.md` for why each rule is the way it is). Numbers marked ⚙ may still change after playtesting. The reference implementation is `packages/game_core`. If this document and the code ever disagree, that is a bug; please report it.

---

## 1. The idea in one paragraph

Two desert fortresses face each other across the sand. Between them lie two wells. **An army fights only while it has water.** A soldier linked to the fortress or the Amir by a chain of comrades can strike. A soldier who wanders off alone can still walk, but cannot attack. Hold the wells with a linked army to collect **water points**. You win by **capturing the enemy Amir**, **planting a supplied piece on the enemy fortress**, or **collecting 10 water points**.

## 2. Components

- A 7×7 board. Files are **a–g** (left to right) and ranks are **1–7** (from South's side).
- Two **Qal'a** squares (fortress keeps): **d1** for South and **d7** for North.
- Two **Wells** (آبار): **c4** and **e4** ⚙.
- A water counter for each player (0–10), starting at 0.
- Two armies of 8 pieces. **South** (light, upper-case letters) moves first. **North** (dark, lower-case letters) moves second.

| Piece | Letter | Count | In one line |
|---|---|---|---|
| **Amir** (أمير), leader | A | 1 | 1 step in any direction. Lose it and you lose |
| **Jundi** (جندي), soldier | J | 4 | 1 step forward, back or sideways |
| **Faris** (فارس), horseman | F | 2 | Slides up to 3 squares ⚙ forward, back or sideways |
| **Rami** (رامي), archer | R | 1 | Steps 1 square diagonally. Shoots enemies 2 squares ⚙ away in a straight line |

## 3. Setup

```
      a   b   c   d   e   f   g
    ┌───┬───┬───┬───┬───┬───┬───┐
  7 │   │ f │ j │ a │ j │ f │   │  ← North home rank; d7 = North Qal'a
    ├───┼───┼───┼───┼───┼───┼───┤
  6 │   │   │ j │ r │ j │   │   │
    ├───┼───┼───┼───┼───┼───┼───┤
  5 │   │   │   │   │   │   │   │
    ├───┼───┼───┼───┼───┼───┼───┤
  4 │   │   │ ≈ │   │ ≈ │   │   │  ≈ = Well (c4, e4)
    ├───┼───┼───┼───┼───┼───┼───┤
  3 │   │   │   │   │   │   │   │
    ├───┼───┼───┼───┼───┼───┼───┤
  2 │   │   │ J │ R │ J │   │   │
    ├───┼───┼───┼───┼───┼───┼───┤
  1 │   │ F │ J │ A │ J │ F │   │  ← South home rank; d1 = South Qal'a
    └───┴───┴───┴───┴───┴───┴───┘
```

Each Amir starts on its own Qal'a. The two armies mirror each other.

## 4. Turns

Players alternate turns, and South moves first. On your turn you **must make exactly one move** with one of your pieces. You cannot pass.

A move is one of three things:
- a **step**: move to an empty square;
- a **capture**: move onto a square with an enemy piece, which is removed from the board;
- a **shot**: your Rami removes an enemy piece at range without moving.

You may never move onto a square occupied by one of your own pieces. Pieces never jump over other pieces.

## 5. How pieces move

In the diagrams, `●` marks squares the piece can reach in an open position.

### Amir (A): 1 step in any of 8 directions

```
  . . . . .
  . ● ● ● .
  . ● A ● .
  . ● ● ● .
  . . . . .
```

### Jundi (J): 1 step orthogonally (no diagonals). It may step backwards.

```
  . . . . .
  . . ● . .
  . ● J ● .
  . . ● . .
  . . . . .
```

### Faris (F): slides 1, 2 or 3 squares in a straight orthogonal line

It stops when it reaches a piece. It may capture the **first** enemy piece in its path (if it is supplied, see §6), but never one behind it.

```
  . . . ● . . .
  . . . ● . . .
  . . . ● . . .
  ● ● ● F ● ● ●
  . . . ● . . .
  . . . ● . . .
  . . . ● . . .
```

### Rami (R): steps 1 square diagonally, and only to an empty square

The Rami **never captures by moving**. Instead it **shoots**: it removes an enemy piece exactly **2 squares away in a straight line** (forward, back or sideways; **not diagonally**), provided the square in between is **empty**. The Rami stays where it is. A shot uses your whole turn.

It moves diagonally but shoots straight, so it is never covering the same squares it can step to.

```
  . . ✕ . .        ✕ = squares the Rami can shoot (exactly 2 away, straight)
  . ● . ● .        ● = squares it can step to (diagonal)
  ✕ . R . ✕
  . ● . ● .
  . . ✕ . .
```

```
Example: the shot is blocked

  . . j . .
  . . J . .    ← own Jundi in between: the Rami cannot shoot the j
  . . R . .
```

## 6. Water: the Supply rule (the heart of the game)

**Only supplied pieces may capture or shoot.** Unsupplied pieces may still step to empty squares.

### Water sources (for your side)
1. **Your Qal'a.** Your pieces standing **on or next to** your Qal'a square (including diagonally) are supplied. Exception: while an **enemy piece stands on your Qal'a**, it gives you no water.
2. **Your Amir.** "Where the Amir camps, there is water." Your Amir is always supplied ⚙.

Wells are **not** sources. A piece on a Well is supplied only if it is linked to your Qal'a or Amir like any other piece (see §7 for what Wells do).

### Chains
Water flows from piece to piece. **Any piece of yours touching a supplied piece of yours is also supplied.** Touching means the 8 surrounding squares, diagonals included. Enemy pieces never pass water along.

```
Example (South):

      a   b   c   d   e   f   g
  5 │   │   │   │   │   │   │ J'│   J' on g5: no chain to any source → UNSUPPLIED
  4 │   │   │ ≈ │   │ F │   │   │   F on the e4 Well: linked e3 → d2 → A → supplied
  3 │   │ J'│   │   │ J │   │   │   J' on b3 touches nothing → UNSUPPLIED
  2 │   │   │   │ J │   │   │   │   J on d2 touches the Amir → supplied
  1 │   │   │   │ A │   │   │   │   A is a source → supplied
```

In the app, supplied pieces glow, so players never have to calculate supply themselves.

### When supply is checked
- **Before your move:** the piece you move must be supplied *at that moment* to capture or shoot.
- **After your move:** supply is recalculated from the new position. A capture can **cut** an enemy chain and leave the pieces beyond the cut without water from the next turn on.

```
Cutting the line (North to move; North's own chain to its Amir is not shown)

      a   b   c   d   e   f   g
  5 │   │   │   │ J │   │   │   │
  4 │   │   │ j │ J │   │   │   │   North's c4 and c3 are linked to the North Amir
  3 │   │   │ j │ J │   │   │   │
  2 │   │   │   │ J │   │   │   │
  1 │   │   │   │ A │   │   │   │

North plays c3xd3. South's d4 and d5 are now cut off from the Amir and the
Qal'a: they can still move, but they cannot capture until they reconnect.
```

## 7. Wells and water points

At the **start of each of your turns**, you gain **1 water point for each Well held by a supplied piece of yours**. Water points are never lost.

Two exceptions:
- A Well held by an **unsupplied** piece earns nothing.
- A Well held by your **Amir** earns nothing. The leader commands; soldiers carry the water.

```
Start of South's turn:
      a   b   c   d   e   f   g
  4 │   │   │ J │   │ J'│   │   │   c4: linked c3 → Amir → +1
  3 │   │   │ J │   │   │   │   │   e4: touches nothing → +0
  2 │   │   │   │ A │   │   │   │
South gains 1 water point.
```

You only score a Well that you still hold after your opponent's turn, so grabbing one is not enough: you must defend it for a full turn.

## 8. How to win

The game ends **immediately** when one of these happens:

1. **Capture the Amir.** If you capture or shoot the enemy Amir, you win.
2. **Take the Qal'a.** If at the **end of your turn** one of your **supplied** pieces stands on the **enemy Qal'a**, you win.
   - An unsupplied piece standing there does not win yet. If a later move of yours brings water to it, you win at the end of that turn.
   - Your Amir is always supplied, so your Amir walking into the enemy Qal'a wins.
3. **Water victory.** If at the start of your turn your water reaches **10 points** ⚙, you win.
4. **No moves.** If it is your opponent's turn and they have **no legal move**, you win. (This is rare.)

If your move wins by 1 or 2, that win counts even if your opponent would reach 10 water at the start of their turn.

### Time limit: the 60-ply rule ⚙
If nobody has won after **60 plies** (30 moves each), the game ends and is decided in this order:
1. The player with **more water points** wins.
2. If water is tied, the player holding **more Wells** wins.
3. If Wells are tied, the player with **more pieces** on the board wins.
4. If pieces are also tied, the game is a **draw**.

In the balance lab about 3 games in 4 end before the limit, and fewer than 1% are draws.

## 9. Quick-reference card

```
┌──────────────────────────────── QAL'A ────────────────────────────────┐
│ Turn:  move ONE piece. South first. No passing.                       │
│ A Amir   1 step any way          J Jundi  1 step + (no diagonal)      │
│ F Faris  slide 1–3, + only       R Rami   step 1 diagonal;            │
│                                           SHOOT 2 away straight (+),  │
│                                           gap empty                   │
│ SUPPLY:  only supplied pieces capture/shoot.                          │
│          Sources = your Qal'a (on/next to it) and your Amir.          │
│          Touching friends pass water on.                              │
│ WELLS:   start of your turn: +1 water per Well held by a supplied     │
│          piece (not the Amir).                                        │
│ WIN:     capture the Amir  •  supplied piece on enemy Qal'a           │
│          •  10 water  •  opponent has no move                         │
│ PLY 60:  more water → more Wells → more pieces → draw                 │
└───────────────────────────────────────────────────────────────────────┘
```

## 10. Notation

**Squares:** `a1` … `g7`.

**Moves:**
| Kind | Format | Example |
|---|---|---|
| Step | `from-to` | `b1-b4` |
| Capture | `fromxto` | `c3xd3` |
| Shot | `from*target` | `d2*d4` |

**Positions:** ranks 7 to 1 separated by `/`. Upper case is South and lower case is North; a digit is a run of empty squares. After a space come the side to move (`s`/`n`), the number of plies played, and the water points as `south:north`. The opening position is:

```
1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0
```

## 11. Rulings and FAQ

- **Can a piece stand on its own Qal'a?** Yes. It is supplied there.
- **Can an enemy piece stand on my Qal'a without winning?** Yes, if it is unsupplied. While it is there, your Qal'a gives you no water. Capture it!
- **Does the Rami need water to step?** No. Only captures and shots need water.
- **Can the Faris capture an enemy hidden behind another piece?** No. It stops at the first piece it reaches.
- **Does standing on a Well make my piece supplied?** No (since v0.6). It must be linked to your Qal'a or Amir. If it isn't, the Well earns you no water points either.
- **Do I lose water points when I lose a Well?** No. Points are never lost; you just stop earning them.
- **Can my Amir hold a Well?** It can stand there (and block the enemy from it), but it earns no water.
- **Can the Rami shoot diagonally?** No (since v0.6). It steps diagonally and shoots straight.
- **Does a move that leaves my Amir attacked count as illegal (like "check")?** No. There is no check. If you leave your Amir where it can be captured, the opponent may simply take it.
- **Can I capture my own pieces?** No.
- **Is there any luck?** None. Both players see everything.
- **First-player advantage?** In the balance lab South (first) scores between 45% and 54%, depending on how the openings are chosen. The best openings (a Jundi toward a Well) need more study in playtests. If South turns out to be favoured, the **pie rule** is ready for ranked play: after South's first move, North may choose to swap sides.

## 12. Teaching script (≈5 minutes)

1. *"Capture their Amir, walk into their castle, or fill your water to 10. That's how you win."* Point to the Amirs, the two Qal'a squares and the Wells.
2. *"Four pieces."* Show the Amir, Jundi and Faris moves on an empty board. Then show the Rami step and shot.
3. *"Here's the twist: water."* Show a chain glowing from the Amir and the Qal'a, then break it with one capture. *"Without water you can walk, but you can't attack."*
4. *"Hold a Well with a linked piece and you earn a water point each turn. Ten points wins."*
5. Play.

## 13. Version history

| Version | Date | Change |
|---|---|---|
| 0.6 | 2026-10-10 | Adopted after the balance lab (details in `docs/balance-log.md`): water points (+1 per Well held by a supplied piece at the start of your turn, 10 wins); Wells are no longer supply sources; an Amir on a Well earns no water; the Rami shoots straight only; the ply limit is decided by water first |
| 0.2–0.5, 0.7 | 2026-10-10 | Tried in the balance lab, not adopted; see `docs/balance-log.md` |
| 0.1 | 2026-10-08 | First complete ruleset, from Concept 1 in `docs/concepts.md`. Changes from the concept draft: 8 pieces with 1 Rami; Qal'a supply is "on or next to"; the ply limit is 60 with a Wells → pieces → draw tiebreak; a player with no legal move loses |

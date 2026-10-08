# Qal'a (قلعة): Wells & Walls, Official Rules

**Rules version 0.1** (pre-balance). The balance lab (step 3) will tune the numbers marked ⚙ and record every change in `docs/balance-log.md`. The reference implementation is `packages/game_core`. If this document and the code ever disagree, that is a bug; please report it.

---

## 1. The idea in one paragraph

Two desert fortresses face each other across the sand. Between them lie two wells. **An army fights only while it has water.** A soldier linked to water by a chain of comrades can strike. A soldier who wanders off alone can still walk, but cannot attack. You win by **capturing the enemy Amir** or by **planting a supplied piece on the enemy fortress**.

## 2. Components

- A 7×7 board. Files are **a–g** (left to right) and ranks are **1–7** (from South's side).
- Two **Qal'a** squares (fortress keeps): **d1** for South and **d7** for North.
- Two **Wells** (آبار): **c4** and **e4** ⚙.
- Two armies of 8 pieces. **South** (light, upper-case letters) moves first. **North** (dark, lower-case letters) moves second.

| Piece | Letter | Count | In one line |
|---|---|---|---|
| **Amir** (أمير), leader | A | 1 | 1 step in any direction. Lose it and you lose |
| **Jundi** (جندي), soldier | J | 4 | 1 step forward, back or sideways |
| **Faris** (فارس), horseman | F | 2 | Slides up to 3 squares ⚙ forward, back or sideways |
| **Rami** (رامي), archer | R | 1 | Steps 1 square diagonally. Shoots enemies 2 squares ⚙ away |

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

The Rami **never captures by moving**. Instead it **shoots**: it removes an enemy piece exactly **2 squares away** in any of the 8 directions, provided the square in between is **empty**. The Rami stays where it is. A shot uses your whole turn.

```
  ✕ . ✕ . ✕        ✕ = squares the Rami can shoot (exactly 2 away)
  . ● . ● .        ● = squares it can step to
  ✕ . R . ✕
  . ● . ● .
  ✕ . ✕ . ✕
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
2. **A Well you occupy.** A piece of yours standing on a Well is supplied. An empty Well, or one held by the enemy, gives you nothing.
3. **Your Amir.** "Where the Amir camps, there is water." Your Amir is always supplied ⚙.

### Chains
Water flows from piece to piece. **Any piece of yours touching a supplied piece of yours is also supplied.** Touching means the 8 surrounding squares, diagonals included. Enemy pieces never pass water along.

```
Example (South):

      a   b   c   d   e   f   g
  5 │   │   │   │   │   │   │ J'│   J' on g5: no chain to any source → UNSUPPLIED
  4 │   │   │ ≈ │   │ F │   │   │   F on e4 stands on a Well → supplied
  3 │   │   │   │ J │   │   │   │   J on d3 touches F (diagonal) → supplied
  2 │   │ J'│   │   │   │   │   │   J' on b2 touches nothing → UNSUPPLIED
  1 │   │   │   │ A │   │   │   │   A is a source → supplied
```

In the app, supplied pieces glow, so players never have to calculate supply themselves.

### When supply is checked
- **Before your move:** the piece you move must be supplied *at that moment* to capture or shoot.
- **After your move:** supply is recalculated from the new position. A capture can **cut** an enemy chain and leave the pieces beyond the cut without water from the next turn on.

```
Cutting the line (North to move)

      a   b   c   d   e   f   g
  5 │   │   │   │ J │   │   │   │
  4 │   │   │ j │ J │   │   │   │   c4 = Well held by North → j on c3 is supplied
  3 │   │   │ j │ J │   │   │   │
  2 │   │   │   │ J │   │   │   │
  1 │   │   │   │ A │   │   │   │

North plays c3xd3. South's d4 and d5 are now cut off from the Amir and the
Qal'a: they can still move, but they cannot capture until they reconnect.
```

## 7. How to win

The game ends **immediately** when one of these happens:

1. **Capture the Amir.** If you capture or shoot the enemy Amir, you win.
2. **Take the Qal'a.** If at the **end of your turn** one of your **supplied** pieces stands on the **enemy Qal'a**, you win.
   - An unsupplied piece standing there does not win yet. If a later move of yours brings water to it, you win at the end of that turn.
   - Your Amir is always supplied, so your Amir walking into the enemy Qal'a wins.
3. **No moves.** If it is your opponent's turn and they have **no legal move**, you win. (This is rare.)

### Time limit: the 60-ply rule ⚙
If nobody has won after **60 plies** (30 moves each), the game ends and is decided in this order:
1. The player holding **more Wells** wins.
2. If Wells are tied, the player with **more pieces** on the board wins.
3. If pieces are also tied, the game is a **draw**.

Most games should finish well before the limit. The rule is there so that a mobile game always ends within 5–10 minutes, and so that the Wells matter from the first move.

## 8. Quick-reference card

```
┌──────────────────────────────── QAL'A ────────────────────────────────┐
│ Turn:  move ONE piece. South first. No passing.                       │
│ A Amir   1 step any way          J Jundi  1 step + (no diagonal)      │
│ F Faris  slide 1–3, + only       R Rami   step 1 diagonal;            │
│                                           SHOOT 2 away, gap empty     │
│ WATER:   only supplied pieces capture/shoot.                          │
│          Sources = your Qal'a (on/next to it), Wells you hold,        │
│          your Amir. Touching friends pass water on.                   │
│ WIN:     capture the Amir  •  supplied piece on enemy Qal'a           │
│          •  opponent has no move                                      │
│ PLY 60:  more Wells → more pieces → draw                              │
└───────────────────────────────────────────────────────────────────────┘
```

## 9. Notation

**Squares:** `a1` … `g7`.

**Moves:**
| Kind | Format | Example |
|---|---|---|
| Step | `from-to` | `b1-b4` |
| Capture | `fromxto` | `c3xd3` |
| Shot | `from*target` | `d2*d4` |

**Positions:** ranks 7 to 1 separated by `/`. Upper case is South and lower case is North; a digit is a run of empty squares. After a space comes the side to move (`s`/`n`), then the number of plies played. The opening position is:

```
1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0
```

## 10. Rulings and FAQ

- **Can a piece stand on its own Qal'a?** Yes. It is supplied there.
- **Can an enemy piece stand on my Qal'a without winning?** Yes, if it is unsupplied. While it is there, your Qal'a gives you no water. Capture it!
- **Does the Rami need water to step?** No. Only captures and shots need water.
- **Can the Faris capture an enemy hidden behind another piece?** No. It stops at the first piece it reaches.
- **Can my piece on an enemy-held Well get water from it?** No. Only *your* piece on a Well makes it your source, and two pieces can never share a square.
- **Does a move that leaves my Amir attacked count as illegal (like "check")?** No. There is no check. If you leave your Amir where it can be captured, the opponent may simply take it.
- **Can I capture my own pieces?** No.
- **Is there any luck?** None. Both players see everything.
- **First-player advantage?** The balance lab will measure it. If needed, the **pie rule** is ready for ranked play: after South's first move, North may choose to swap sides.

## 11. Teaching script (≈5 minutes)

1. *"Capture their Amir, or walk into their castle. That's how you win."* Point to the Amirs and the two Qal'a squares.
2. *"Four pieces."* Show the Amir, Jundi and Faris moves on an empty board. Then show the Rami step and shot.
3. *"Here's the twist: water."* Show a chain glowing from the Amir and from a Well, then break it with one capture. *"Without water you can walk, but you can't attack."*
4. *"Wells matter twice: they give water, and they break ties at move 30."*
5. Play.

## 12. Version history

| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-10-08 | First complete ruleset, from Concept 1 in `docs/concepts.md`. Changes from the concept draft: 8 pieces with 1 Rami; Qal'a supply is "on or next to"; the ply limit is 60 with a Wells → pieces → draw tiebreak; a player with no legal move loses |

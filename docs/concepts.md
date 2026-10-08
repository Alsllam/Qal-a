# Qal'a (قلعة): Game Concepts (Step 1)

Status: **proposal, waiting for a pick.** Nothing below is final. The balance lab (step 3) will change numbers such as piece counts, ranges and turn limits.

## Design constraints (from the brief)

| Goal | What it means for the design |
|---|---|
| Learn in 5 minutes | At most 3–4 piece types, one special rule, and a win condition you can explain in one sentence |
| Master over months | Strategy that interacts on two levels (local tactics plus a global structure), with no solved opening |
| 5–10 min on mobile | Small board (6×6 to 7×7), 6–8 pieces per side, a win condition that ends the game decisively, and a hard turn cap |
| No luck | Perfect information and no dice in all three concepts. First-move advantage is handled by balancing, with the **pie rule** (swap rule, public domain, from Hex) kept as a backup |
| Arab heritage theme | Fortress and wells, monsoon sea trade, the incense caravan |
| 2 players first, 4 later | Each concept notes how it could scale |

Notation: files `a–g` run left to right and ranks `1–7` run bottom (South, first player) to top (North).

---

## Concept 1: **Qal'a: Wells & Walls** (supply lines)

**Theme.** Two desert fortresses face each other across open sand. Between them are two wells. An army fights only while it has water. A soldier cut off from the fortress and the wells can still walk, but cannot fight.

**Board.** 7×7. Each side has a **Qal'a** (keep) square in the center of its home rank (d1 and d7). There are two neutral **Wells** at c4 and e4.

```
    a   b   c   d   e   f   g
7 | r | f | s | A | s | f | r |   ← North; Qal'a at d7
6 | . | s | . | . | . | s | . |
5 | . | . | . | . | . | . | . |
4 | . | . | W | . | W | . | . |   W = Well
3 | . | . | . | . | . | . | . |
2 | . | S | . | . | . | S | . |
1 | R | F | S | A | S | F | R |   ← South; Qal'a at d1
```

**Pieces (8 per side, 4 types).**

| Piece | Count | Moves | Captures |
|---|---|---|---|
| **Amir** (أمير, leader) | 1 | 1 step, any of 8 directions | Same as move |
| **Jundi** (جندي, soldier) | 4 | 1 step orthogonally (forward, back or sideways) | Same as move |
| **Faris** (فارس, horseman) | 2 | 1–3 squares in a straight orthogonal line, no jumping | Same as move |
| **Rami** (رامي, archer) | 1 | 1 step diagonally | **Shoots**: removes an enemy exactly 2 squares away in a straight line (orthogonal or diagonal) if the square between is empty. The archer does not move when it shoots |

(The corner pieces marked `R` are the archer and a 5th Jundi in the final layout. Whether to use 1 or 2 archers is a balance-lab question.)

**Special mechanic: Supply.**
- A piece is **supplied** if it is linked by a chain of friendly pieces, each touching the next (8-neighbour), to a **water source**.
- Your water sources are your own Qal'a square, any Well that one of your pieces stands on, and **your Amir** ("where the Amir camps, there is water").
- **Only supplied pieces may capture or shoot.** Unsupplied pieces can still move, so they can reconnect.
- The app highlights supplied pieces with a glow so the player never has to work it out.

```
Supply example (South; Qal'a at d1)

    a   b   c   d   e   f   g
4 | . | . | W | . | W | . | . |
3 | . | . | . | F | . | . | S |    F supplied: d3 → d2 → d1 (Qal'a)
2 | . | . | . | S | . | . | . |    S on g3 is NOT supplied: no chain to a source,
1 | . | . | . | A | . | . | . |    so it can move but cannot capture
```

**Win condition.** You win if either:
1. you capture the enemy **Amir**, or
2. one of your **supplied** pieces ends a turn on the enemy **Qal'a** square.

A hard cap of 60 plies ends the game. If neither side has won by then, the player who holds more Wells wins. If that is tied, the player with more supplied pieces wins. Only then is it a draw. (The balance lab will tune this.)

**Why it's fun.**
- It works on two levels at once. Chess-style tactics happen on the surface, and a Go-like "cut the line" structure sits underneath. The big moments are cutting an enemy chain so that three of their pieces lose their teeth at once.
- The Amir is both the target and a mobile water source. Pushing your leader forward to supply an attack is a real, readable risk.
- The Wells give the middle of the board a purpose from move one.

**What could go wrong.**
- **Turtling.** Players may clump into one blob that is always supplied. Possible fixes: give the Qal'a less supply range, or make the Wells worth points.
- **Cognitive load at the table.** Supply is easy on a screen with the glow, but harder on a physical board. That matters for a future boxed edition.
- **Amir too central.** If the Amir as a water source is too strong, the leader always marches. Possible fix: the Amir supplies only adjacent pieces and does not extend chains.
- **The archer's ranged capture** can make defense feel unfair to beginners. Keep it to one archer, or allow shooting only straight forward.

**Originality check.**
- *Capture the leader* (chess, xiangqi, shogi) and *reach a target square* (tafl, Camelot) are generic, centuries-old ideas. Nobody owns them.
- *Capture without moving* exists in chess variants such as Rifle Chess. *Pieces need connection* appears in DVONN (cut-off pieces are removed), Meridians (unconnected pieces die) and Lines of Action (connection is the goal). In each of those, connection decides **survival or victory**.
- In Wells & Walls, connection decides **whether a piece may attack**, and the leader is a movable source. I found no game built on that. A web search for "capture only when connected to supply" found nothing equivalent.

**Scaling to 4 players.** A plus-shaped board with 4 Qal'as and 4 Wells, played 2v2. Your partner's chain carries your water. That is a very natural team mechanic.

---

## Concept 2: **Mawsim (موسم): Monsoon Traders** (you choose your opponent's wind)

**Theme.** Dhows sail the Gulf and the Arabian Sea between trading ports. The sailors who master the monsoon (الموسم) win the trade.

**Board.** A 7×7 sea. There are 3 neutral **Ports** on the middle rank at b4, d4 and f4, and one home harbor per side at d1 and d7. A shared **Wind compass** sits beside the board and points N, E, S or W. It starts pointing East, which is crosswind for both sides.

```
            Wind ➜ E
    a   b   c   d   e   f   g
7 | . | d | d | N | d | d | . |   ← North fleet; home harbor d7
6 | . | . | . | . | . | . | . |
5 | . | . | . | . | . | . | . |
4 | . | P | . | P | . | P | . |   P = Port
3 | . | . | . | . | . | . | . |
2 | . | . | . | . | . | . | . |
1 | . | D | D | N | D | D | . |   ← South fleet; N = Nakhoda (flagship)
```

**Pieces (5 per side, 2 types).**

| Piece | Count | Moves |
|---|---|---|
| **Dhow** (sail) | 4 | Straight orthogonal line, no jumping. **Downwind** (in the direction the wind blows): up to 3. **Crosswind**: up to 2. **Upwind**: 1. Diagonally: 1 square, in any wind |
| **Nakhoda** (نوخذة, flagship, oars) | 1 | 1 square in any direction, whatever the wind. Cannot ram |

**Capture (ramming).** A Dhow captures an enemy ship by ending its move on it, but **only when that move was downwind**.

**Special mechanic: you set your opponent's wind.**
Each turn has two parts:
1. Move one ship.
2. **Turn the wind compass**: 90° left, 90° right, or leave it.

Your opponent then sails with the wind you chose. Every move is both an attack and a gift (or a curse) to the other side. The wind is public and nothing is random, so the game stays luck-free.

```
Wind = S (blowing toward South). North's dhows sail toward South downwind:
  up to 3 squares and able to ram.
South's dhows moving North are upwind: 1 square, cannot ram.
South must now decide: leave S (bad), or turn it to E or W (crosswind for both).
```

**Win condition.** You win if either:
1. **Trade monopoly**: at the start of your turn you hold **2 of the 3 Ports** (a ship of yours sits on each), or
2. you **sink the enemy Nakhoda**.

Holding the ports at the *start* of your turn gives your opponent exactly one reply. That makes it a king-of-the-hill race that ends quickly.

**Why it's fun.**
- One dial, many consequences. The rules fit on a card, but working out "if I give them East, they reach b4, but then I get…" is deep.
- The theme comes through in how it plays. You feel like a sailor reading the season, not like a chess player with ships painted on.
- The decisive port-holding goal keeps games short (an estimated 20–40 plies).

**What could go wrong.**
- **Stalemate wind.** Each player might always turn the wind to "crosswind for both", so ramming never happens. Possible fixes: the wind must change every turn, or each player gets 2 *Monsoon* tokens per game that set any direction.
- **First-player tempo.** Setting the wind is strong, so the first-player edge may be large. The balance lab will measure it, and the pie rule is ready if needed.
- **Analysis paralysis.** Every move has 3 wind choices, so new players can freeze. The UI should preview "what the opponent can reach" for each wind option.
- **Feel.** It is the furthest of the three from "chess family", which could matter for marketing.

**Originality check.**
- Wind that affects sailing speed is common in sailing wargames (Jolly Roger, Seeschlacht, Wooden Ships & Iron Men). In those games the wind is **random or shared**.
- The closest design I found is **Regati**, where a player may spend a turn changing the wind for everyone. Mawsim is different: changing the wind is a mandatory second half of every turn, and it decides only the **opponent's** next move. That is closer in spirit to **Kamisado**, where your move decides which piece the opponent must move, but the mechanism is completely different.
- Port control is a generic king-of-the-hill idea.

**Scaling to 4 players.** Natural fit: 4 harbors (N, E, S, W) with the compass pointing to whoever is "favored". The wind becomes a diplomacy tool, and 2v2 works well.

---

## Concept 3: **Qafila (قافلة): The Incense Caravan** (the caravan never travels alone)

**Theme.** Incense caravans cross the desert from Dhofar to the markets of the north. A camel train cannot cross alone. It moves from escort to escort, and raiders try to break the chain.

**Board.** 7×7 desert. Each side has a home rank. The opponent's back rank is your **Souq** (market).

```
    a   b   c   d   e   f   g
7 | . | g | g | C | g | g | . |   ← North; C = Camel
6 | . | . | g | . | g | . | . |
5 | . | . | . | . | . | . | . |
4 | . | . | . | . | . | . | . |
3 | . | . | . | . | . | . | . |
2 | . | . | G | . | G | . | . |
1 | . | G | G | C | G | G | . |   ← South
```

**Pieces (7 per side, 2 types). This is the easiest of the three to learn.**

| Piece | Count | Moves | Captures |
|---|---|---|---|
| **Haris** (حارس, guard) | 6 | 1 step in any of 8 directions | Moves onto an enemy piece (Camel included) |
| **Camel** (جمل) | 1 | **Cannot step.** It moves only by **hopping over an adjacent friendly guard** (orthogonal or diagonal) to the empty square directly beyond. It may chain several hops in one turn and change direction between hops | Never captures |

```
Hop chain example (South Camel at d2, guards on d3 and d5)

    a   b   c   d   e   f   g
7 | . | . | . | . | . | . | . |   (Souq rank: reachable only from a 5th-rank landing over a 6th-rank guard)
6 | . | . | . | ② | . | . | . |   ② = lands here after hop 2 (over d5)
5 | . | . | . | G | . | . | . |
4 | . | . | . | ① | . | . | . |   ① = lands here after hop 1 (over d3)
3 | . | . | . | G | . | . | . |
2 | . | . | . | C | . | . | . |
```

Guards spaced with one empty square between them form a **ladder** the Camel can run up in a single turn.

**Turn.** Move one guard **or** make one Camel hop chain.

**Win condition.**
1. Your Camel reaches **any square of the enemy back rank** (the Souq), or
2. you **capture the enemy Camel** (raid).

**Special mechanic: escort relay.** All of the Camel's movement comes from your formation. Your guards build ladders, and a long ladder lets you win from across the board in one turn. Your opponent must read your ladders and break them by capturing or blocking a landing square. Defense means spotting the threat, and attack means building two ladders at once.

**Why it's fun.**
- It has the fastest learn-to-fun time: 2 piece types and 1 rule, and kids get it immediately.
- Dramatic swings. "Ladder threats" are visible, readable, and give huge comeback moments, which suit 5-minute mobile sessions.
- The board shows the theme. A line of guards with the camel hopping along it looks like a caravan.

**What could go wrong.**
- **Too sharp.** Long hop chains could make the first player win by force. Possible fixes: cap the chain at 3 hops, or don't allow a hop to land on the rank in front of the Souq.
- **Shallow mid-game.** With only one guard type, the depth may plateau sooner than in Concepts 1 and 2. A second guard type (e.g., a "Scout" that moves 2) could be unlocked as an advanced rule.
- **Closest to prior art** of the three (see below), so it needs the clearest differentiation.

**Originality check.**
- **Camelot** (Parker Brothers, 1930) has a "canter": leaping over your own pieces in chains, plus jump-captures and a castle goal. Halma and Chinese checkers also chain-hop. These are the closest relatives.
- Differences: in Qafila **only one VIP piece hops, and hopping is its only way to move**. Guards never hop. Capture is by displacement, not by jumping, and the game is won by the VIP alone.
- Game mechanics generally aren't protected by copyright. Names, artwork and rulebook text are. Even so, if you pick Qafila, I'd keep the rules text and presentation clearly distinct from Camelot's.

**Scaling to 4 players.** 2v2, where a Camel may hop over **partner** guards too. Team play becomes literally building each other's roads.

---

## Side-by-side

| | 1. Wells & Walls | 2. Mawsim | 3. Qafila |
|---|---|---|---|
| Learn time | ~5 min (4 piece types + supply) | ~4 min (2 types + wind) | ~3 min (2 types + hop) |
| Mastery depth | **Highest**: tactics plus network structure | High: tempo and prediction | Medium (likely needs an advanced variant) |
| Feels like chess family | **Yes** | Somewhat | Yes (checkers/Camelot cousin) |
| Expected length | 30–50 plies | 20–40 plies | 20–35 plies |
| Main balance risk | Turtling, draws | Wind stalemate, first-player tempo | First-player sharpness |
| Originality | **Strongest** | Strong | Weakest (Camelot/Halma cousin) |
| Matches the name "Qal'a" | **Literally** | No | No |
| 4-player potential | Good (shared water) | **Best** (wind diplomacy) | Good (shared roads) |
| AI coach friendliness | Excellent: "your Faris is unsupplied" is a clear, teachable lesson | Good | Good |

## My recommendation

**Concept 1, Wells & Walls.** It fits the name, it is clearly part of the chess family (good for marketing and for player intuition), it is the most original of the three, and the supply rule gives the AI coach a concrete concept to teach ("cut the line", "the Amir is your water"). Its main risks, turtling and draws, are exactly what the balance lab is built to measure and fix with numbers (well scoring, turn cap, Amir supply radius).

If you like the wind idea, it could come back later as a **"Sea" variant/mode** of the same app, or as the 4-player mode.

## Legal note

I'm not a lawyer. In most countries (US, EU and others), game *mechanics and rules as ideas* are not protected by copyright. Rulebook **text**, artwork, names/trademarks and trade dress are, and some games have held patents. Before launch, do a proper **trademark search for "Qal'a / قلعة"** in your target markets (a quick web search found only an unrelated 1996 memory game called *Qal* and the abstract game *Qawale*). For extra certainty, have a games/IP lawyer review the final rules.

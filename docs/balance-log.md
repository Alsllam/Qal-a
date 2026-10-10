# Balance log

This is the changelog of rule versions for **Qal'a: Wells & Walls**. Each entry gives the change, why it was made, and the numbers from the balance lab (`tools/balance`). The full reports are in `tools/balance/reports/v<version>.md`.

## Method

- **AI:** negamax alpha-beta with capture quiescence and a heuristic evaluation (material, supplied pieces, Wells, advance toward the enemy Qal'a). The evaluation is tested to be mirror-symmetric, so the AI favours neither side.
- **Self-play:** 2,000 games per version at search depth 3. The first 2 plies are random, so many openings are explored. Each move is chosen at random among the moves scoring within 0.15 of the best, so games don't repeat.
- **Skill gradient:** 200 games of depth 4 against depth 2, with sides alternating. If the stronger AI wins clearly, the game rewards skill rather than being decided by the first move.
- **Reproducible:** every game has its own seed. Re-running `dart run bin/balance.dart --rules <v> --depth 3` gives the same report, whatever the number of CPU cores.

## Targets

| Check | Target | Why |
|---|---|---|
| First-player balance | South scores 50% ± 5% | Fair for both players |
| Draws | ≤ 10% | Every game should have a winner |
| Game length | 20–50 plies on average | 5–10 minutes on mobile |
| Varied endings | No end reason above 75% | More than one way to win |
| Every piece matters | Each type makes 8–45% of moves | No useless or dominant piece |
| Supply matters | ≥ 5% of pieces unsupplied on average | The core mechanic must actually bite |
| No dominant opening | No first move scores more than 15 points above average (with ≥ 40 games) | No single opening wins by force |
| Skill is rewarded | Depth 4 scores ≥ 70% against depth 2 | Depth worth months of mastery |

## Versions

Reports from full runs (2,000 games at depth 3, plus the skill gradient) are in `tools/balance/reports/`. Exploratory runs (fewer games, or depth 2) are in `tools/balance/reports/explore/`.

### Summary

| Version | Status | South score | Draws | Mean plies | Checks | Main problem |
|---|---|---|---|---|---|---|
| 0.1 | replaced | 39.5% | 8.5% | 58.9 | 6/8 | 87% of games run to the ply limit; North wins by grabbing a Well on the last move |
| 0.2 / 0.2b | explored | 58% | 0.5% | 29–34 | 3/7 | Pure race for the Wells; supply irrelevant |
| 0.3 | replaced | 56.3% | 0.1% | 39.4 | 6/8 | Rami opening plus Amir camping on a Well (~80% for the first Rami) |
| 0.3b, 0.4 | explored | 46–55% | ~1% | 43–46 | 5–6/7 | See below |
| 0.5 / 0.5b | explored | 55–57% | 0.2% | 42–43 | 5–6/7 | Rami opening still dominant |
| **0.6** | **adopted** | **54.2%** (44.8% with AI openings) | **0.3%** | **45.4** | **7/8** | Best openings (Jundi toward a Well) need playtesting |
| 0.7 | fallback | 52.1% (42.4% with AI openings) | 0.3% | 44.6 | 7/8 | Over-compensates North when openings are played well |

---

### v0.1: first complete ruleset (2026-10-08)

Rules as in step 2: Wells are supply sources, a 60-ply limit decided by Wells → pieces → draw, a Rami that shoots in 8 directions, and the Amir as a water source.

**Results** (`reports/v0.1.md`): South 35.2% / North 56.3% / draws 8.5%. Mean length 58.9 plies, with **87% of games reaching the 60-ply limit**: 63% decided by Wells, 16% by pieces, 8.5% drawn. Only 3% of games ended by capturing the Amir and 10% by taking the Qal'a. Skill gradient 88% ✅.

**Diagnosis.**
1. Nothing pushes players to attack, so the AI trades a few pieces and then sits.
2. **North makes the last move (ply 60)** and can step onto a Well with no reply. That is a structural second-player advantage inside the tiebreak itself.

**Proposed change:** make the Wells score *during* the game (water points), so holding them is a race that ends games early, and score them at the start of the holder's turn so the opponent always gets one chance to contest.

### v0.2 / v0.2b: water points (explored)

+1 water point per Well held at the start of your turn; 8 (v0.2) or 6 (v0.2b) wins. The ply limit is decided by water first. 400 games at depth 2.

**Results:** games became short (29–34 plies) and draws vanished (0.5%), but 86–88% of games ended by water, South scored 58% because it reaches the Wells first, and only 1.5% of pieces were ever unsupplied. A piece on a Well was its own water source, so **supply stopped mattering**: you just ran a piece onto a Well.

**Proposed change:** tie water to supply. Wells stop being sources, and a Well only scores while the piece on it is linked to your Qal'a or Amir.

### v0.3: Wells score only when supplied (replaced)

Wells are no longer supply sources; a Well earns water only while its holder is supplied; 10 water points wins.

**Results:** at depth 2 (400 games) it looked balanced: South 50.0%, 6/7 checks. **At depth 3 (2,000 games, `reports/v0.3.md`) it was not:** South 56.3%, and the Rami openings `d2-c3`/`d2-e3` scored **80–85%**. Replays showed the pattern: the Rami steps to c3 and covers c5 (the approach to the c4 Well), then the Amir walks onto the Well and collects water. The Amir is always supplied and hard to attack. Lesson: **the depth-2 AI was too weak to find the strongest plan**, so every later decision was checked at depth 3.

Variants explored alongside:
- **v0.3b** (North starts with 1 water point): 46.5% at depth 2. It shifts the balance, but doesn't fix the cause.
- **v0.4** (the Amir is not a water source either): South 55%, and no Qal'a wins at all. Rejected.

**Proposed change:** an Amir standing on a Well earns no water.

### v0.5 / v0.5b: the Amir earns no water (explored)

600 games at depth 3.

**Results:** endings became much more varied (Amir captured 21%, Qal'a taken 7%, water 57%), but South still scored 55–57%, and `d2-e3` still scored ~80%. A probe showed that **whoever develops the Rami first wins about 80%**, even when the opponent copies the move. The Rami's 8-direction shot lets it cover the Well approaches diagonally from a safe square.

**Proposed change:** the Rami shoots orthogonally only (4 directions). It still steps diagonally, so its moves and its threats cover different squares.

### v0.6: the Rami shoots straight (ADOPTED, 2026-10-10)

Rules: v0.5, plus the Rami shoots orthogonally only. Full rules in `docs/rules.md`.

**Results** (`reports/v0.6.md`, 2,000 games at depth 3):

| Check | Result |
|---|---|
| First-player balance | ✅ South 54.2% ± 2.2% |
| Draws | ✅ 0.3% |
| Length | ✅ mean 45.4 plies (median 45, p10 29, p90 60), about 6–8 minutes at 8–10 s per move |
| Varied endings | ✅ water 50.6%, ply limit (water) 20.6%, Amir captured 15.0%, Qal'a taken 10.5% |
| Every piece matters | ✅ Jundi 40.9%, Faris 31.6%, Amir 19.3%, Rami 8.2% of moves |
| Supply matters | ✅ 6.3% of pieces unsupplied on average; 8.9% of moves are made by unsupplied pieces |
| No dominant opening | ⚠️ `c2-c3` 73.6%, `e2-e3` 70.9% (a Jundi toward a Well) |
| Skill is rewarded | ✅ depth 4 scores 93.5% against depth 2 |

The Rami openings dropped from 80–85% (v0.3) to 62–68% (`d2-c3` 62.1%, `d2-e3` 68.4%), now in line with other good developing moves such as `b1-b3` (63.4%).

**About the remaining ⚠️.** With random opening plies, the strong opening moves are compared against an average that includes *bad* random first moves, so some gap is expected. To check, v0.6 was also run with **AI-chosen openings** (`--openings 0`, 600 games, `reports/explore/v0.6-ai-openings.md`): there **South scores 44.8%**, and the AI itself often picks a weak Rami opening (`d2-c3`: 27%). The true first-player balance is therefore somewhere between **45% and 54%**, within the target on either side, but the Jundi-to-Well opening is clearly the main line and needs watching in playtests.

### v0.7: v0.6 + North starts with 1 water point (fallback, not adopted)

**Results** (`reports/v0.7.md`): South 52.1% with random openings, but **42.4% with AI-chosen openings** (`reports/explore/v0.7-ai-openings.md`). The head start over-compensates when both sides open well. It also adds a rule to learn. Kept as a ready fallback if playtests show South winning clearly more than half the games (the pie rule is the other option).

## Open questions for playtesting (step 4)

1. **The opening `c2-c3` / `e2-e3`** (a Jundi toward a Well). Is it obvious to humans, and does North have a comfortable answer?
2. **First-player balance** with humans: if South wins clearly more than half, try v0.7 or the pie rule.
3. **The 60-ply limit:** about 1 game in 4 still reaches it (decided by water). With human speed, does that feel like a natural ending or a cut-off?
4. **Rami usage** is the lowest of the four pieces (8.2% of moves). Do players find it fun, or forget it?
5. **Is supply readable?** The glow on supplied pieces is essential. Watch whether players understand why a capture is not allowed.

## How to reproduce

```sh
cd tools/balance
dart pub get
dart run bin/balance.dart --rules 0.6 --games 2000 --depth 3 --gradient 200
dart run bin/balance.dart --rules 0.6 --games 600 --depth 3 --openings 0 --gradient 0
```

# Playtest guide: Qal'a prototype (rules v0.6)

This guide is for running playtest sessions with the prototype in `prototype/`. The balance lab already showed that the AI finds the game fair and deep. Playtests answer what the AI cannot: **is it easy to learn, readable, and fun for humans?**

---

## 1. What this round must answer

| # | Question | Where the answer comes from |
|---|---|---|
| Q1 | Can a new player learn the game in **5 minutes**? | Time the teaching; watch game 1 |
| Q2 | Do players **understand supply** (why a capture is not allowed)? | Observation + "explain it to a friend" question |
| Q3 | Is the game **fun** enough to want a rematch? | Rematch requests, ratings, body language |
| Q4 | Is the **first player** favoured with humans? | Results table (aim for 20+ human games) |
| Q5 | Do humans find the **Jundi-to-Well opening** (`c2-c3` / `e2-e3`), and can North answer it? | Game records |
| Q6 | Does the **60-ply limit** feel natural or abrupt? | Interview; how many games reach it |
| Q7 | Is the **Rami** fun or forgotten? | Moves per game by the Rami; interview |
| Q8 | Do games last **5–10 minutes**? | Game record timings |

## 2. People

- **8–12 players** per round, in pairs.
- Mix them, and note each person's group:
  - **chess/strategy players**: they will look for depth and exploits;
  - **casual mobile gamers**: the main audience; watch learning and fun;
  - **2–4 teenagers (13–17)**, with a parent's consent: the fastest learners and the most honest.
- At least half should be **Arabic speakers**, to check that the theme and names feel right.
- Avoid friends who already know the rules, except for one pilot session to test this guide.

## 3. Setup

- **Device:** one phone or tablet per pair (the prototype is pass-and-play), plus a second device for the vs-AI game. A web link works on any phone (see `prototype/README.md`).
- **Before each session,** check: rules **v0.6** is selected on the home screen, and the volume is off.
- **Roles:** a **facilitator** teaches and asks the questions; an **observer** only watches and takes notes. Alone? Record the screen and audio (with consent) and take notes afterwards.
- **Print** one observation sheet (section 7) per game, and the quick-reference card from `docs/rules.md` §9 (but don't hand it out unless a player asks; note if they do).
- **Consent:** explain what you record and why, collect no personal data beyond first name, age group and player type, and get a parent's consent for anyone under 18.

## 4. Session plan (about 50 minutes per pair)

| Time | Step | Notes |
|---|---|---|
| 0–2 min | Welcome | "We're testing the game, not you. There are no wrong answers. Please think aloud." |
| 2–7 min | **Teach** with the 5-minute script (`docs/rules.md` §12) | **Start a timer.** Stop teaching at 5 minutes, even if the player has questions; note the questions. |
| 7–20 min | **Game 1**, pass-and-play | Don't help unless they are stuck for more than 30 seconds. Note every question they ask. |
| 20–32 min | **Game 2**, sides swapped | Who wins with which side? Do they play differently? |
| 32–40 min | **Game 3** (optional): each player alone against the AI (Easy or Medium) | Who asks for Hard? |
| 40–50 min | **Interview** (section 6) | Ratings first, then open questions |

After each game, tap **Copy game record** on the end screen and paste it into the session log (section 8).

## 5. What to watch for

Note the **time** (from the game clock) and the **move number** next to each observation.

**Learning (Q1, Q2)**
- Questions asked during game 1. Write them down word for word; each one points to a rule that is unclear.
- Attempts at an illegal move, especially **trying to capture with an unsupplied piece**. Does the player read the "no water" message? Do they understand it?
- Do they look at the **glow** (supplied) and the **faded** pieces (unsupplied) before moving?
- Do they notice the **water counter** going up? When do they first deliberately go for a Well?

**Fun and feel (Q3)**
- Moments of **delight**: laughing, "oh!", leaning in, a "cut the line" moment, a surprise shot.
- Moments of **frustration**: sighs, long pauses, "that's not fair", giving up.
- **Analysis paralysis:** a move taking more than 60 seconds. Note what the player was deciding.
- Do they ask for a **rematch** without being prompted? This is the strongest signal of fun.

**Strategy (Q4–Q7)**
- **First moves.** Which piece moves first? Does anyone play `c2-c3` or `e2-e3`?
- **How the game ends:** Amir captured, Qal'a taken, water 10, or ply limit. Was the loser surprised?
- **The Rami.** How many times was it moved, and how many shots? Did anyone forget it existed?
- **The Amir.** Is it hidden at home, or used as a water carrier at the front?
- **Exploits.** Does anyone find a repeated trick that always works? Write the exact moves.

**Pace (Q8)**
- The total game time (shown in the record) and the slowest single move.

## 6. Interview questions

Ask in this order. **Don't explain or defend the game while interviewing.** If they misunderstood a rule, note it; that is data.

### A. Ratings (1 = not at all, 5 = very much)

| # | English | العربية |
|---|---|---|
| R1 | How easy was it to learn? | ما مدى سهولة تعلّم اللعبة؟ |
| R2 | How fun was it? | ما مدى استمتاعك باللعبة؟ |
| R3 | How fair did the result feel? | ما مدى عدالة النتيجة في رأيك؟ |
| R4 | How much do you want to play again? | ما مدى رغبتك في اللعب مرة أخرى؟ |
| R5 | How much does the theme (fortresses, wells, water) fit the game? | ما مدى انسجام الفكرة (القلاع، الآبار، الماء) مع اللعبة؟ |

### B. Open questions

| # | English | العربية |
|---|---|---|
| O1 | Explain to a friend, in one sentence, how you win. | اشرح لصديق في جملة واحدة: كيف تفوز؟ |
| O2 | Explain the water rule in your own words. *(Comprehension check: right / partly / wrong.)* | اشرح قاعدة الماء بكلماتك. |
| O3 | What was your favourite moment? | ما أكثر لحظة أعجبتك؟ |
| O4 | When did you feel confused or stuck? | متى شعرت بالحيرة أو التوقف؟ |
| O5 | Which piece did you like most, and which least? Why? | أي قطعة أحببتها أكثر وأيها أقل؟ ولماذا؟ |
| O6 | Did the game end at the right time? Too early, too late? | هل انتهت اللعبة في الوقت المناسب؟ مبكرًا أم متأخرًا؟ |
| O7 | Did you feel the first or the second player had an advantage? | هل شعرت أن اللاعب الأول أو الثاني لديه أفضلية؟ |
| O8 | If you could change one rule, what would it be? | لو استطعت تغيير قاعدة واحدة، ماذا ستغيّر؟ |
| O9 | What other game does this remind you of? | بماذا تذكّرك هذه اللعبة من الألعاب الأخرى؟ |
| O10 | Would you play this on your phone? When, and against whom (friends, AI, strangers online)? | هل ستلعبها على هاتفك؟ متى، وضد من (أصدقاء، الذكاء الاصطناعي، لاعبين عبر الإنترنت)؟ |

O9 tells you about positioning and about any originality concerns. O10 informs the GDD (step 5): AI levels, online play and session length.

## 7. Observation sheet (one per game)

```
Session #___  Game #___  Date ______  Observer ______
South: name/age group/type ______    North: name/age group/type ______
Teaching time: ___ min   Questions during teaching: __________________

Time | Move | Who | Observation (quote / behaviour)           | Tag
-----+------+-----+--------------------------------------------+-----
     |      |     |                                            |
     |      |     |                                            |
Tags: L = learning  S = supply confusion  F = fun/delight  X = frustration
      A = analysis paralysis  E = exploit  R = Rami  W = Wells/water

Result: winner ______  reason: Amir / Qal'a / water / ply limit
Rematch asked without prompting?  yes / no
```

## 8. Session log (one row per game)

Keep a spreadsheet with these columns (most come from the copied game record):

```
session, game, date, rules_version, mode (pvp/ai-easy/ai-medium/ai-hard),
south_type, north_type, winner, end_reason, plies, minutes, slowest_move_s,
first_move, rami_moves, rami_shots, illegal_capture_attempts, rematch_asked,
R1, R2, R3, R4, R5, O2_comprehension (right/partly/wrong), notes
```

## 9. Decision rules (after the round)

Agree on these **before** testing, so the results decide and not opinion.

| If… | Then… |
|---|---|
| South wins **≥ 60%** of 20+ human pvp games | Switch to rules **v0.7** (North starts with 1 water), or add the pie rule. Re-run the balance lab |
| **> 30%** of players get O2 wrong after game 1 | Supply is not readable: improve the UI (stronger glow, chain lines, "no water" explanation) **before** changing rules |
| Teaching takes **> 7 minutes** for most players | Simplify: consider removing the Rami from the "first game" setup (a learning path), and keep it for later levels |
| **> 30%** of games hit the 60-ply limit, or O6 says "too long" | Test 8 water points to win in the lab |
| The Rami averages **< 2 moves per game**, or is the least liked piece by most | Revisit its role (e.g. range, start square) in the lab |
| Average game **> 12 minutes** | Look at slow moves: a UI problem (unclear options) or a rules problem (too many options)? |
| R2 (fun) averages **< 3.5** | Stop and rethink before step 5; read O3/O4/O8 closely |
| Anyone finds a repeatable **exploit** | Reproduce it in the lab (`GameState.fromNotation`) and fix it |

## 10. After the round

1. Fill in the session log and add a short summary to `docs/balance-log.md` ("Playtest round 1").
2. Turn every confusing question from game 1 into either a UI fix or a teaching-script change.
3. Bring the ratings, the decision-rule outcomes and the top 5 quotes to the step 5 review. They feed directly into the GDD: learning path, AI levels and MVP scope.

---
name: self-learning
description: >
  A disciplined self-study workflow for learning a course, a book, or a mindset with the AI
  as a Socratic tutor, quizzer, and reflection partner — built on active recall, spaced
  review, and teach-back, not passive summarizing. Use when the user runs
  `/self-learning <topic>`, or wants to study and actually retain material.
---

# Self-Learning

Learn something and actually retain it. The AI's job here is **not to summarize the material
for you** — that produces the *feeling* of learning without the learning. Its job is to make
**you** retrieve, explain, and get tested, then fill the gaps it exposes.

> **Golden rule:** the human does the recall and the teach-back; the AI asks, quizzes,
> critiques, and tracks. If the AI is doing most of the talking, no learning is happening.

## 1. Frame the goal — ask one at a time

- **What** are you learning, and **why** — what will you do with it?
- **Target depth:** *awareness* (recognize it) · *working* (use it with a reference) ·
  *mastery* (teach it / build with it unaided).
- **"Done" test:** one concrete check — "I can build X", "I can explain Y to a beginner",
  "I can make decision Z correctly." Everything aims at passing this.

## 2. Pick the material branch

- **Course** — has a syllabus → follow its sequence; set a cadence you can sustain.
- **Book** — go chapter by chapter; each chapter is a unit.
- **Mindset / principle** — no fixed syllabus (e.g. "clean-architecture judgment", "senior
  review taste"). Learn by **principle → worked examples → your own cases**.

## 3. Survey & map (before deep study)

Get the structure (syllabus / TOC / outline). Extract the **learning objectives**. Find the
high-leverage 20% that unlocks most of the value, then sequence the units. Skimming the map
first isn't cheating — it's how experts orient.

## 4. Active study loop (per unit) — the core

Run each unit through this loop. Techniques in [TECHNIQUES.md](TECHNIQUES.md).

1. **Preview** — write 2–3 questions you should be able to answer after this unit.
2. **Engage** — read / watch / do the unit.
3. **Teach-back (Feynman)** — you explain it in plain words *from memory*; the AI critiques
   for gaps, hand-waving, and wrong analogies (reflection / self-critique).
4. **Self-quiz (active recall)** — the AI generates a short quiz; you answer *from memory*;
   it grades and names exactly what you missed.
5. **Socratic dig** — the AI asks "why / how / what-if / where does this break?" to expose
   shallow spots.
6. **Fill gaps** — restudy only the exposed gaps, then re-quiz until they close.

## 5. Connect & apply

- **Elaborate** — how does this relate to what you already know? Where does it conflict?
- **Deliberate practice** — do one small real exercise/project, or apply it to current work.
  For a mindset, run *your own* scenario past the principle and have the AI critique the call.

## 6. Retain — where learning actually sticks

- **Spaced review** — schedule quick recall checks (next day → +3 days → +1 week → +1 month).
  Each review is a quiz *from memory*, never a re-read.
- Track **corrected misconceptions** — what you were wrong about is the highest-value note.

## 7. Capture — a learning log (ask before creating files)

A persistent log turns "read once" into "verified and revisitable", and lets you resume
after `/clear`. **Do not create files unprompted** — ask the user **whether to save one and
where** (e.g. `learning/{topic}.md`, a personal notes vault, or not at all). Keep it lean:

- Goal + "done" test · Objectives · Notes (in your own words) · **Quiz history** (date,
  score, what was missed) · **Open questions** · **Corrected misconceptions** ·
  **Next spaced-review date**.

Skip the log for a quick one-off; use it for anything you intend to retain or resume.

## Anti-patterns

- **AI summarizes; you nod.** Passive reading feels productive and isn't. Force retrieval.
- **No "done" test.** Without it, you study forever or stop too early.
- **Re-reading instead of recall.** Re-reading is the weakest study method; quiz from memory.
- **Auto-saving files.** Ask where the log goes; don't litter the repo.

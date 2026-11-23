# Button colours — one meaning per colour

## Why

Players missed the host's turn in a Plynling visit: its choices were grey and read as decoration (8.2.5
made them green). The owner wants every colour to mean one thing across the bot, so a player reads a
card at a glance and a new button's colour is a rule, not a guess.

## The theme

| Colour | Style | Means | Examples |
|---|---|---|---|
| Green | `Success` | yes, I'm in, commit | Rejoindre, Accepter, Publier, Terminer, Acheter, Encaisser |
| Red | `Danger` | no, stop, or it can hurt | Refuser, Annuler, Clôturer, Abandonner, Allumer la mèche, Frapper |
| Blurple | `Primary` | do this; pick one of these; the selected tab | Modifier, Corriger, Deviner, event and visit choices, quiz answers, active tab |
| Grey | `Secondary` | neutral | pagers, unselected tabs, « Mes votes », disabled leftovers |

- **A "pick one" choice is blurple** (owner's call), whoever it waits on: visit options and reactions,
  Plynling event picks, quiz answers, 🅰️/🅱️.
- **« 💌 Lui avouer ses sentiments » is green** (owner's call): the one special move among blurple
  options. It is the theme's single exception and says so in a comment.
- A button that opens a form or the next step (« Créer une session », « Préparer le vote ») is blurple;
  green is for the answer or the final commit inside that flow.

## Goals, non-goals

- Goal: the rule written where every new button is decided (`CLAUDE.md`, *Conventions*), and the
  existing buttons that break it brought in line.
- Non-goal: changing labels, emojis, layouts or select menus. Non-goal: a style for embeds' accent colours.
- Anti-goal: a recolour pass that changes buttons already following the theme.

## Approaches

1. **Rule in `CLAUDE.md` + fix the outliers** — chosen. Small, and the rule sits where buttons get decided.
2. Rule + a `ButtonTheme` helper (`Yes`, `No`, `Choice`, `Neutral`) replacing every `ButtonStyle` — 174
   call sites over ~40 files for no visible change, and a semantic name can still be picked wrong.
   Rejected (Claude's call; the owner talked outcomes, not mechanism).

## The changes

- `PlynlingVisitCards`: options and reactions → `Primary`; « 💌 » → `Success`.
- `PollCards`: both « Créer une session » (`poll:tosession`, `poll:plan`) → `Primary`, like `/tonight`'s
  « Créer la session » and the chat request's « Préparer la session ».
- `PartyCards` blackjack « Doubler » → `Danger` (it doubles the stake: a risk, not a yes).
- `PlynlingModule.Rendering` « Événement (n) » → `Primary`, like « Événement suivant ».
- `CLAUDE.md`: a short *Button colours* rule under *Conventions*.
- Patch bump (8.2.6) and a changelog line.

## Left to the builder

Anything not listed above stays as it is. « Soigner » stays green (healing reads as a yes here).

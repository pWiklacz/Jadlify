# Plan posiłków — build notes (design tokens extracted from Strona główna v5)

## Fonts
- Body: `'Archivo', system-ui, sans-serif` (weights 400/500/600/700)
- Display/serif: `'Instrument Serif', serif` (headings, logo, big numbers)
- Google fonts link: `https://fonts.googleapis.com/css2?family=Instrument+Serif:ital@0;1&family=Archivo:wght@400;500;600;700&display=swap`

## Colors
- App bg (dark): `#1F1712`; hero radial: `radial-gradient(1100px 500px at 50% -120px,#2C2015 0%,#1F1712 70%)`
- Text on dark: `#F2E9D8`; muted `rgba(242,233,216,0.55)`; hairline `rgba(242,233,216,0.08–0.16)`
- Card cream panel: bg `#F8F1E3`, text `#2A2018`, muted `#8C7F6C`, dotted divider `#DCCFB2`
- Accent terracotta: `#D97E57` (links/icons), stronger `#C75B38` (primary btn), hover `#B04E2E`
- Error: `#B3402E`
- Primary btn: bg `#C75B38`, color `#FFF8EC`, radius 999px; hover `#B04E2E`
- Ghost/outline btn: border `1px solid rgba(217,126,87,0.55)`, color `#D97E57`, transparent bg; hover `rgba(217,126,87,0.12)`

## Layout patterns
- Header: centered logo (leaf SVG + "Jadlify" serif + tagline "PLANER POSIŁKÓW I MAKRO"), status pill left, account/wyloguj right. Pill nav below centered.
- Nav items: pill `padding:8px 14px;border-radius:999px`, active `background:rgba(242,233,216,0.1);color:#F2E9D8`, inactive muted. "Plan posiłków" is our active tab.
- Main: `max-width:1180px;margin:0 auto;padding:clamp(18px,3vw,34px) clamp(16px,3.4vw,44px) 100px`
- Cards: cream `#F8F1E3` radius 20–22px, `box-shadow:0 20px 50px rgba(0,0,0,0.25)`
- Date nav control: pill container border `rgba(242,233,216,0.16)`, prev/next round 32px btns + `<input type=date>` (color-scheme:dark), "Wróć do dzisiaj" ghost btn.
- Badges: `font-size:10.5px;font-weight:700;letter-spacing:0.16em;padding:5px 11px;border-radius:999px`

## Animations (in helmet <style>)
- jd-pulse (skeletons), jd-spin (loaders), jd-toast, jd-rise (card entrance 0.4s), jd-bar (scaleX bars), jd-ring (SVG progress)

## Mobile
- isMobile/isDesktop via sc-if; sticky header; horizontal scroll pill nav; single column.
- Determined by JS (matchMedia in componentDidMount, state.isDesktop).

## Meal types (Polish)
Śniadanie, Obiad, Kolacja, Przekąska

## Macro status copy (text, not just color/bars)
"zostało 320 kcal", "cel osiągnięty", "przekroczono o 12 g"

## Nav handlers on homepage: goHome, goProdukty, goPrzepisy, goPlan, goCele, goLista, goKonto, goWyloguj

## User answers
- Desktop: row of 7 compact day cards on top + selected-day detail panel below.
- Show 2 VARIANTS of the week-view layout (turn-based options, canvas mode). Variant A = compact cards row; Variant B = agenda/vertical alt. Actually user picked layout A as main; still wants 2 variants of week-view to compare.
- Full mobile in same file (responsive).
- Drag&drop as desktop enhancement + accessible buttons (Przenieś/Kopiuj).
- Build main flow fully + clickable states for the 27 scenarios.
- All copy Polish.

## Flows to build (priority: all selected)
nav+week overview, day details+macro compare, move/copy/repeat (meal prep), add-meal + create-recipe-in-flow, edge states (empty days/no goal/unavailable recipe), loading/errors/bg refresh, shopping-list integration.

## Approach
Single DC `Jadlify - Plan posiłków.dc.html`. Because user wants 2 variants of week view -> use canvas options mode:
`<meta name="design_doc_mode" content="canvas">`, one <section> per turn, ids 1a/1b badges.
Turn 1: 1a (full interactive planner, cards-row + day panel), 1b (agenda-row alt week layout).
Interactive scenario switcher inside 1a to demo the 27 states (buttons to toggle: planned week / empty week / mixed / no goals / add-meal modal / edit / move / copy / copy-day / delete+undo / no recipes / unavailable recipe / loading / bg refresh / errors / mobile).
Mobile shown via responsive + a mobile frame option.

State model in Component logic: entries[] {id,date,recipeId,meal,portions,kcal,p,f,c}, recipes[], goal{kcal,p,f,c}|null, weekStart, selectedDate, modal state, toast/undo.

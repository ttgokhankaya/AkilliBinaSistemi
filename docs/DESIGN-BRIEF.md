# ADLE Web — UI/UX Design Brief

> A brief you can hand to a UI/UX designer **or** paste into an AI design tool
> (v0, Lovable, Figma Make, etc.). It describes the product, the six screens,
> the data each one shows, and the design system we want established. Where a
> screen already exists as raw HTML, treat that as *functional reference only* —
> the visual design is greenfield.

---

## 1. Product in one paragraph

**ADLE** (Activity Driven Life Environments) is a research tool for detecting
anomalies in **Activities of Daily Living** inside smart homes. A researcher
models a home (rooms, devices, daily routines), records or simulates resident
activity as sequences of sensor/room transitions, and then runs analyses —
next-step prediction, dimensionality reduction (t-SNE), and Random Forest
proximity — to find behavior that deviates from the norm. It is the web
successor to a Windows desktop (WPF) app, rebuilt as an ASP.NET Core API + a
React single-page app so the same analysis is available in the browser.

## 2. Who uses it

- **Primary: the researcher / thesis author.** Technically fluent, comfortable
  with matrices, probabilities, and scatter plots. Wants density and control,
  not hand-holding.
- **Secondary: reviewers / readers of the open-sourced project.** They open the
  app to understand what it does. The UI should *teach the domain* through clear
  labels, tooltips, and legible visualizations.
- **Single user.** No login, no multi-tenant, no roles, no notifications. Do not
  design sign-in, onboarding wizards, billing, or account screens.

## 3. Design goals

1. **Clarity over decoration.** This is an analytical tool. Every screen should
   make the domain concept obvious and the data easy to read.
2. **A real design system.** Today everything is inline raw HTML with ad-hoc
   colors and font sizes. We want reusable tokens and components (see §7).
3. **Charts are first-class.** Five of six screens center on a visualization.
   The chrome around a chart (controls, legend, empty/loading/error states)
   deserves as much care as the chart itself.
4. **Responsive, desktop-first.** Optimized for a researcher on a laptop/desktop,
   but must not break on tablet or phone. Charts and wide tables need graceful
   reflow / horizontal scroll.
5. **Trustworthy states.** Loading, empty, error, and confirm-before-delete are
   currently missing or crude. Design them properly.

## 4. Hard constraints (don't fight these)

- **Stack:** React 19 + TypeScript + Vite. Plain CSS today (no Tailwind/MUI),
  but the designer may propose a token/utility approach — deliver design tokens
  as CSS custom properties so they drop straight in.
- **Charting = Plotly.js** (via `react-plotly.js`). Do not design bespoke SVG
  charts that Plotly can't produce. You *can* fully restyle Plotly: colors,
  fonts, margins, hidden/curated mode-bar, legends, hover labels.
- **Language: English only.** No localization, no language switcher.
- **Dark mode required.** The app already honors `prefers-color-scheme`. Design
  **both light and dark palettes**; a manual toggle in the header is a welcome
  addition.
- **No backend changes implied.** Design to the data that already exists (§6).
  If a screen would benefit from a field the API doesn't return, call it out as
  a note rather than assuming it.

## 5. Information architecture

Single-page app with a persistent top navigation. Six primary destinations,
grouped conceptually:

- **Model the home** — Areas, Devices, Operations
- **Simulate & analyze** — Graphs, Prediction, Anomaly

Consider a header with the ADLE wordmark, the six-item nav, and a dark-mode
toggle. A grouped sidebar is an acceptable alternative if it reads better for
this many analytical sections. There is no dashboard/home screen today — propose
one if you think a landing overview (counts of areas/devices/operations, quick
links to analyses) would help orient the user.

## 6. Screen-by-screen brief

Each screen currently exists as a bare CRUD form + table or a chart with inline
controls. For every one below: **purpose**, the **data it shows** (real field
names from the API), the **key interactions**, and **design notes**.

### 6.1 Areas — spatial model of the home
- **Purpose:** define rooms/zones and see their spatial layout.
- **Data (AreaDto):** `Id`, `Name`, `Width`, `Height`, `ParentAreaId` (nesting),
  `AreaTypeId`, `AreaTypeName` (e.g. LivingRoom, Bedroom, Kitchen).
- **Interactions:** list all areas; create/edit via a form (Name, Width, Height,
  parent area dropdown, area-type dropdown); delete with confirm.
- **Visualization:** a 2-D scatter/plan plotting Width × Height, points grouped
  and colored by area type. This is the closest thing to a floor-plan — lean
  into that. Consider rendering areas as sized rectangles rather than points if
  it reads more like a building layout.
- **Notes:** parent/child nesting implies a hierarchy — a tree or breadcrumb
  could complement the flat list.

### 6.2 Devices — sensors & actuators
- **Purpose:** manage the smart-home devices inside areas.
- **Data (DeviceDto):** `Id`, `Name`, `Ip`, `State` (on/off, nullable),
  `Kind` (subtype), `AreaId`, `AreaName`. Kinds are domain-specific and Turkish
  in the data: *Lamba* (light), *Termostat* (thermostat), *Hareket Sensörü*
  (motion sensor), *Akıllı Priz* (smart outlet).
- **Interactions:** list/table with columns above; create/edit (Name, IP, State
  toggle, Kind dropdown, Area dropdown); delete with confirm.
- **Design notes:** device **state** should be an obvious status — green “on” /
  neutral “off” pill, not a raw checkbox in a table. Consider per-Kind icons so a
  motion sensor reads differently from a light at a glance. IP is a technical
  detail — keep it present but de-emphasized.

### 6.3 Operations — daily routines
- **Purpose:** define scheduled routines (morning/noon/evening/night).
- **Data (OperationDto):** `Id`, `Name`, `StartTime` (datetime),
  `DurationSeconds` (double).
- **Interactions:** list; create/edit (Name, start time picker, duration); delete
  with confirm. Duration is stored in seconds but should render human-readable
  (“1h 30m”).
- **Design notes:** a timeline / 24-hour track visualization of when routines
  start and how long they last would be far more legible than a plain table.
  Optional but encouraged.

### 6.4 Graphs — topology + random-walk simulation
- **Purpose:** store a graph of the home’s state-transition topology (nodes =
  rooms/sensors, edges = transitions) and simulate resident movement as random
  walks.
- **Data (GraphDto):** `Id`, `Name`, `MatrixValue` (a **TSV adjacency matrix**).
- **Run controls (RunGraphRequest):** `StartNode`, `EndNode`, `Iterations`
  (default 25), `UseEdgeWeights` (bool), `AllowLoops` (bool).
- **Run output (RunGraphResponse):** `WalkCount` + `Walks` (array of node-name
  sequences, one per iteration).
- **Interactions:** pick/create a graph; edit the matrix; set run parameters;
  run; inspect the resulting walks.
- **Design notes:** the matrix is edited as a raw TSV textarea today — this is
  the roughest screen. Design a clearer matrix editor (grid with row/column
  headers by node name) and, ideally, a **node-link graph preview** so the user
  sees topology, not just numbers. Walk results are sequences — show them as
  readable ordered chips/paths, not just rows of text.

### 6.5 Prediction — next-step forecasting (LCS + softmax)
- **Purpose:** given training sequences of activity, predict the next node and
  which resident it likely is.
- **Request (PredictRequest):** `Training` = list of `{ Tokens: string[],
  Person: string? }`; `Prefix` = ordered node names observed so far;
  `LcsThreshold` (int, default 0).
- **Response (PredictResponse):** `PredictedNextNode`, `PredictedPerson`,
  `MatchCount`, `RuleUsed`, and a `Distribution` = list of
  `{ Node, Probability }`.
- **Interactions:** build/paste training sequences (each a token list + optional
  person label); enter a prefix; set the LCS threshold; run; read the prediction.
- **Visualization:** a **horizontal probability bar chart** of the distribution
  over candidate next nodes (Plotly), plus a headline result card
  (predicted node, predicted person, confidence, which rule fired, match count).
- **Design notes:** sequence entry is the hard part — design an ergonomic way to
  enter/edit multiple token sequences (chips per token, add/remove rows) rather
  than one giant textarea. `LcsThreshold` and `Perplexity`-type parameters need
  inline tooltips explaining what they do.

### 6.6 Anomaly — t-SNE + Random Forest
- **Purpose:** the flagship analysis screen. Two related tools.
- **t-SNE section:**
  - Input (TsneRequestDto): `Observations` (matrix of feature vectors),
    `Perplexity` (default 0.9), `NComponents` (2 or 3).
  - Output (TsneResponseDto): `Output` = reduced 2-D/3-D coordinates.
  - Visualization: a 2-D (or 3-D) **scatter of the projection**, points colored
    by cluster. Perplexity as a slider with a tooltip. Include a way to load
    synthetic sample data so the screen is never empty on first visit.
- **Random Forest section:**
  - Input (RandomForestRequestDto): `S0`, `S1` (training samples),
    `AllObservations`, `NTrees` (default 100).
  - Output (RandomForestResponseDto): `ProximityMatrix` (sample-to-sample
    similarity) + `TreeTexts` (decision trees as text).
  - Visualization: a **proximity heatmap** (Plotly) + a readable presentation of
    the decision-tree text dumps (collapsible, monospaced, one tree per card).
- **Design notes:** this screen has the most controls and the richest output.
  Give it real structure — two clearly separated analysis panels, each with its
  own input controls, run button, and result area with loading/empty/error
  states. This is the screen most likely to appear in the thesis/README, so it
  should look the most polished.

## 7. Design system to establish

Deliver these as reusable tokens + components, not per-screen one-offs.

- **Color:** a full light and dark palette. Semantic roles: primary/accent,
  surface/background layers, borders, text (primary/secondary/muted), and status
  (success/on = green, warning = amber, danger/high-anomaly = red, info = blue).
  Current ad-hoc values to replace: accent `#4f80ff`, danger `#d33`, gray borders
  `#888` variants. Validate text/background pairs for **WCAG AA** contrast in
  both modes.
- **Typography:** a defined type scale (display / h1 / h2 / h3 / body / small /
  mono) replacing today’s ad-hoc 0.85–1.5rem sizes. Keep a system font stack
  (system-ui, Segoe UI, Roboto…) and a monospace stack (Consolas / ui-monospace)
  for matrices, sequences, and tree dumps.
- **Spacing & layout:** a spacing scale, page max-width, card/section container,
  and responsive breakpoints (desktop / tablet / phone).
- **Components (the priority list):**
  - Top navigation + dark-mode toggle
  - Page/section layout wrapper with title + description
  - **Button** variants: primary, secondary, tertiary/ghost, danger, and a
    disabled/loading state (prevents double-submit)
  - **Form field:** label + input + hint + inline error, for text / number /
    datetime-local / checkbox-toggle / select
  - **Table:** with row actions (edit/delete), zebra striping, and horizontal
    scroll on narrow screens
  - **Modal / confirm dialog** for destructive actions (delete is currently
    unconfirmed)
  - **Status pill / badge** (device on/off, anomaly severity)
  - **Card** for headline results and grouped panels
  - **Tooltip** for explaining parameters (perplexity, LCS threshold, edge
    weights)
  - **State components:** loading (spinner/skeleton), empty state, error banner
  - **Chart container:** consistent title, curated Plotly mode-bar, legend,
    responsive sizing (Plotly heights are hardcoded 360–440px today — make them
    responsive)
  - **Toast/inline feedback** for successful create/update/delete

## 8. Accessibility

Currently near-zero. Please design for:
- Visible focus indicators on all interactive elements
- Full keyboard navigation (tab order, escape to close modals, enter to submit)
- ARIA labels on icon-only buttons and form controls
- WCAG AA color contrast (both themes)
- Chart data available in an accessible form (data table fallback or aria labels)
  where practical

## 9. Deliverables

Whatever suits your workflow, but ideally:
1. A **design token sheet** (color, type, spacing) — deliverable as CSS custom
   properties for both light and dark themes.
2. A **component library** covering §7.
3. **High-fidelity mockups** of all six screens in both themes, including the
   loading / empty / error / confirm states — not just the happy path.
4. **Responsive behavior** shown for at least the two most complex screens
   (Graphs and Anomaly).
5. Redesigned **Plotly chart styling** (color sequences, fonts, layout defaults)
   we can apply globally.

## 10. Tone & references

Think “analytical research console” — the calm, dense, legible feel of tools
like Grafana, Observable, or a well-designed Jupyter/ML dashboard. Not a
consumer smart-home app (no big glossy device cards), not a marketing site.
Restrained color, strong typographic hierarchy, charts that do the talking.

---

*Context notes for the designer:* the app is single-user and offline-friendly;
data volumes are small (tens of areas/devices, not thousands); the domain mixes
English and Turkish terms in the data itself (device kinds are Turkish) but all
UI chrome is English. There is no real-time streaming — everything is
request/response, so design for “configure → run → read result” rather than live
dashboards.

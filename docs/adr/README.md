# Architecture Decision Records

An ADR records one significant technical decision: its context, the choice made and its consequences.
Write one when a decision is hard to reverse or shapes how code is written (assembly layout, data format,
determinism strategy, third-party packages, CI setup).

## Rules

- Copy [`0000-template.md`](0000-template.md) to `NNNN-kebab-case-title.md` using the next free number.
- Numbers are never reused or renumbered.
- Once **Accepted**, an ADR is not rewritten. To change a decision, write a new ADR and mark the old one
  **Superseded by NNNN**.
- Add the ADR to the index below in the same commit.

Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by NNNN`.

## Index

| # | Title | Status |
|---|---|---|
| [0001](0001-separate-core-logic-from-unity.md) | Separate core game logic from Unity | Accepted |
| [0003](0003-effect-representation.md) | Card effect representation | Proposed |

# Architecture and design decisions

For a single-component decision, the pull request that implements it remains authoritative by default. Write an architecture decision record (ADR) here only when a pull request is not a sufficient long-term entry point for the rationale, such as when:

- the decision affects multiple components;
- an important constraint is difficult to discover from code or public documentation;
- engineers repeatedly fail to find the original rationale; or
- a rejected alternative is likely to return.

## Naming

Name each file `YYYY-MM-DD-short-title.md`, using the date the decision was made.

## Structure

Each ADR contains:

- **Context** — the problem and the constraints that shaped it.
- **Decision** — what was decided.
- **Consequences** — the resulting behavior, tradeoffs, and any follow-up work.
- **Alternative approaches** — the alternatives considered and why they were rejected.

Link each ADR to the pull request that implemented the decision. The ADR records the durable rationale; the pull request remains the record of the actual code change.

## Quality bar

An ADR exists to explore the context and the tradeoffs, not to justify a choice already made. Gregor Hohpe's [warning signs for misused ADRs](https://www.linkedin.com/posts/ghohpe_architecturedecisionrecords-activity-7502716054968844288-KVLU) apply here:

- Context states the problem and its constraints, not preferences for the chosen option.
- Every downside listed under Consequences names a mitigation or an explicit acceptance, and quantifies the risk where it can be quantified.
- Alternative approaches are credible options someone could have argued for, not "do nothing"; identify the ones that represent points of leverage.

An ADR that fails this bar is not worth keeping; record the decision in the pull request instead.

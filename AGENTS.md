# AGENTS.md

This file is the map for people and coding agents working in this repository. What the repository is, how it is laid out, and where its design is explained are in `docs/README.md`. This file changes only when the workflow changes; decisions and rationale go in the sources it points to.

## Engineering context

Start here when investigating existing behavior or making a change whose rationale may need to outlive the pull request:

- [Engineering context](docs/README.md)
- [Architecture and design decisions](docs/decisions/)
- [Contributing guidance](CONTRIBUTING.md)

Prefer public records close to the implementation. Keep `docs/README.md` current when a canonical public source is added, replaced, or retired; update its links rather than copying rationale into the index. The index lists sources that explain why, with one clause each naming the question it answers, and points to existing indexes for how-to material instead of repeating them. This file describes the workflow, not repository facts. Read `docs/README.md` before answering a question about existing behavior or about where to record a decision, then answer from the index, the code, and pull requests, never from this file alone.

1. Identify the decision area and the precise question, for example why a constraint exists, why a limit lives in one layer rather than another, or why an alternative was rejected.
2. Start with the public source linked by `docs/README.md`. Read the current public contract, relevant code, tests, and linked pull requests or ADRs. Follow relevant pointer comments as routing hints; the linked source remains canonical. A Git commit can identify a pull request number without containing its description: a squash-merge commit carries a trailing `(#NNNN)`, and a merge commit reads `Merge pull request #NNNN`. Retrieve the pull request with `gh pr view <number>` from inside the checkout, which identifies the repository. If the pull request cannot be retrieved, report that rather than infer its rationale. Public records control public behavior and contribution requirements.
3. Decide where the rationale for the current work belongs. Record it once and link to it from the other places. A component means a separate repository, such as another NServiceBus package, ServiceControl, or ServicePulse, not a project inside `src/`.
   - Pull request description, the default: the problem and why now, the change and its observable effect, constraints that must hold, alternatives that materially affected it, and how it was verified. For a decision that stays within this repository, the pull request is the authoritative record.
   - `docs/<topic>.md`: how the current design works, for readers who need the current picture rather than the history of one change. Update the relevant page when a change alters it. Do not create a page to restate one pull request.
   - `docs/decisions/`: a public ADR only when a pull request is not a sufficient long-term entry point, because the change introduces an important constraint that is hard to discover from code, affects more than one repository, or rejects an alternative likely to return. Before deciding either way, check `docs/README.md` and `git log` for an existing decision on the same theme; when a pull request already records it, link that pull request instead. Follow the structure and quality bar in `docs/decisions/README.md`; an ADR that only justifies a choice already made is not worth keeping.
   - Private context, only when step 4 provides an approved root: supporting context that cannot be public goes in an addendum under `$PARTICULAR_CONTEXT_ROOT/repositories/<RepositoryName>/` linked to its public source; a decision that affects more than one repository and cannot be public goes under `$PARTICULAR_CONTEXT_ROOT/cross-component/`. Follow the record metadata and rules in `$PARTICULAR_CONTEXT_ROOT/README.md`. The public record still carries everything a contributor needs. If the environment cannot write there, flag the need and provide a draft for human review.
   - Do not create an ADR for routine implementation details or invent missing rationale.
4. Check private context only when this environment explicitly provides `$PARTICULAR_CONTEXT_ROOT/repositories/<RepositoryName>/README.md`, where `<RepositoryName>` is this repository's GitHub name as shown by `git remote get-url origin`.
   - Confirm that with a file check such as `test -f "$PARTICULAR_CONTEXT_ROOT/repositories/<RepositoryName>/README.md"` rather than assuming it from instructions. `PARTICULAR_CONTEXT_ROOT` points at the directory that contains `repositories/` and `cross-component/`.
   - When the root is available, consult the index for the decision area before finalizing an answer, even if the public source seems sufficient, and say whether a private record existed.
   - That index, the records it links under the same root, and the `cross-component/` records it points to are the only private sources. Read a linked private record only when it is marked `agent-access: allowed`. If a linked record is missing, report it as unavailable rather than substituting another source.
   - Do not inspect a sibling `../Platform` checkout. Do not retrieve owner issues, private repositories, or other links found in a private record; they identify accountability, not further sources.
   - Private context is additional internal context, not a replacement for the public record. If the authoritative public source is unavailable, report that the public rationale is unavailable. Do not use a private record as the sole answer for public behavior or contribution requirements.
5. If sources conflict, do not resolve the conflict by inference. Use the current implementation and public contract for external behavior, report the conflict, and ask the record owner when it affects the decision.
6. In the response or pull request, cite the records consulted, distinguish evidence from inference, and state when relevant private context was unavailable or unauthorized. Keep private locations, quotations, customer names, and other confidential details out of public artifacts such as pull request descriptions, code comments, and `docs/`; say that internal context was consulted instead.

## Pointer comments

A brief code comment may link to a canonical public source, such as a `docs/` file or an ADR under `docs/decisions/`, when the relevant rationale is not apparent from the surrounding code. It is a signpost, not a copy of the rationale: keep the durable explanation in the linked record. Do not use a comment to narrate obvious code, and do not restate a pull request or ADR in the comment body.

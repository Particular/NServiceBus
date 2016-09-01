# NServiceBus Maintainers

## Managed Repositories

https://github.com/orgs/Particular/teams/nservicebus-maintainers/repositories

## General Rules

* All changes to repositories managed by the maintainers must come in via Pull Request which are merged by a maintainer. Direct commits to master, develop or support branches are not allowed.
* Pull Requests need to provide a proper description about the related changes.
* The Pull Request title needs to be self explanatory.
* Maintainers are responsible to ensure Pull Requests are reviewed and merged in a timely manner
* Spikes should be labeled with the **Spike** label and the title needs to be prefixed with [WIP]. Spikes do not require the same response time as regular Pull Requests.

## Review Guidelines

* A Pull Request should be reviewed by at least two maintainers before merging it.
* Maintainers are assigned to a PR by themselves or other maintainers by adding their label (`Maintainer: @<github-handle>`) to the PR.
* Maintainer should pro-actively monitor PRs they are assigned to and ensure it moves forward as fast as possible.
* Ensure the public API is covered by acceptance tests where feasible. We favour Component Tests over Acceptance Tests if components can be tested in isolation.
* Ensure the public API is documented. A PR which has impact on the public API should link to a related PR on the documentation repository before merging it. The docs PR can be an empty PR having the `[WIP]` tag on it's title to mark it.
* PRs should only contain commits which represent the intent of the PR. Intermediate cleanup, fixing commits should be squashed together. To simplify the review process consider the following:
 * Prefer renames as dedicated commits
 * Prefer formatting and whitespace cleanup as dedicated commits
* Start PRs on downstream projects that are affected by the changes to facilitate knowledge transfer and conversations. Downstream PRs do not have to contain code modifications, could be used as a starting point to communicate changes and ensure knowledge is distributed.

## Other

* Maintainers are strongly encouraged to be subscribed to the repositories managed by the group
* Changes to the Coding Standards and [design rules](https://github.com/Particular/PlatformDevelopment/tree/master/designprinciples/nservicebus) require group consensus
* Weekly meeting with available maintainers to triage issues. Rotate time zones for the meeting.
* Maintainers should frequently check the [NServiceBus Waffleboard](https://waffle.io/particular/nservicebus) to verify all Pull Requests have maintainers assigned.

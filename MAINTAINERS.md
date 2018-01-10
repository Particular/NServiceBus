# NServiceBus Maintainers

## Managed Repositories

https://github.com/orgs/Particular/teams/nservicebus-maintainers/repositories


## General Rules

* All changes to repositories managed by the maintainers must come in via Pull Request which are merged by a maintainer. Direct commits to master, develop or support branches are not allowed.
* Pull Requests require:
  * A title that explains the purpose of the PR
  * A description that explains the changes included in the PR as well as what triggered the submission of the PR (link to an existing issue preferred)
* Maintainers are responsible to ensure Pull Requests are reviewed and merged in a timely manner
* If the Pull Request is a spike, the title of the PR should be prefixed with **[Spike]**. Spikes do not require the same response time from maintainers as regular Pull Requests do.
* If the Pull Request is not ready for review or merging the title of that PR should be prefixed with **[WIP]**.

## Review Guidelines

* A Pull Request should be reviewed by at least two maintainers before merging it.
* Merging can be done as either a `Squash and merge` or a `Create a merge commit`
  * `Squash and merge` is the preferred method for merging Pull Requests. It should be used when the individual commits in a PR do not need to be retained. The PR number should be included in the squashed commit comment.
  * `Create a merge commit` should only be used if there is value in retaining the commit history of the PR.
* The @particular/nservicebus-maintainers group will be automatically added to PRs as a required reviewer. This will notify all of the maintainers.
* Maintainer should pro-actively monitor PRs they are assigned to and ensure it moves forward as fast as possible.
* Ensure the public API is covered by acceptance tests where feasible. We favour Component Tests over Acceptance Tests if components can be tested in isolation.
* Ensure the public API is documented. A PR which has impact on the public API should link to a related PR on the documentation repository before merging it. The docs PR can be an empty PR having the `[WIP]` tag on it's title to mark it.
* PRs should only contain commits which represent the intent of the PR. Intermediate cleanup, fixing commits should be squashed together. To simplify the review process consider the following:
 * Prefer renames of files as dedicated commits
 * Prefer formatting and whitespace cleanup as dedicated commits
* Start PRs on downstream projects that are affected by the changes to facilitate knowledge transfer and conversations. Downstream PRs do not have to contain code modifications, could be used as a starting point to communicate changes and ensure knowledge is distributed.
* Unit test names should follow the convention already applied in a given test fixture. If no existing test fixture is present it should be consistent to the test names used inside the same folder. 


## Other

* Maintainers are strongly encouraged to be subscribed to the repositories managed by the group
* Changes to the Coding Standards and [design rules](https://github.com/Particular/PlatformDevelopment/tree/master/designprinciples/nservicebus) require group consensus
* Weekly meeting with available maintainers to triage issues. Rotate time zones for the meeting.

## Releasing new versions

* Before releasing new versions of the NServiceBus package, potentially affected downstream repositories should be smoke tested against a pre-release (unstable/release-candidate) version. A downstream repository can be considered smoke tested when all test projects of the repository are updated to the pre-release and all tests succeed or the repositories maintainers confirm that failing tests are no blocker for the release.

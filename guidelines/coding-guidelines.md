## Language Features
It is acceptable to use language features in C# 7.0 and lower versions.

## Performance related

For public facing interfaces we favor read-only collections and enumerables. For internal types we favor speed and allocation reduction. Avoid using collection interfaces internally.

### In hot paths

* Avoid allocations
* Avoid using `System.Linq`
* Avoid using `foreach` over collections that do not have a struct enumerator
* Favor local functions over lambdas and delegates

## Runtime dependencies

### 3rd party

#### Package

We want to compile against the lowest compatible version to catch breaking changes.

PackageReference: `[Major.Minor.0, Major+1)`

#### Tests

We want to compile against the latest version to catch breaking changes and bugs. This is the likely version that our users would be on.

PackageReference: `[Major.*, Major+1)`

### Core

#### Package

##### Stable

We want to compile against the lowest compatible version to catch breaking changes.

PackageReference: `[Major.Minor.0, Major+1)`

##### Unstable

We want downstreams to pick up the latest core alpha/beta/rc immediately to quickly catch problems.

PackageReference: `[Major.Minor.0-*, Major+1)`

#### Tests

Examples are container tests, acceptance testing, acceptance tests, transport tests etc

##### Stable

We want to compile against the latest core to make sure to run the latest test suite.

PackageReference: `[Major.*, Major+1)`

##### Unstable

We want downstreams to pick up the latest core alpha/beta/rc immediately to quickly catch problems.

PackageReference: `[Major.Minor.0-*, Major+1)`

## Development dependencies

Should have `PrivateAssets="All"` set. Examples are Fody, GitVersion, ApprovalTests etc.

We want to optimize for staying on the latest version where possible.

PackageReference: `[Major.*, Major+1)`

This means that builds might break due to issues in new releases but compared to getting out of date this is the least of both evils.

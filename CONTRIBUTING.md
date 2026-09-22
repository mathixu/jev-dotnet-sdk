# Contributing

Thanks for helping improve JevNet.

## Before opening a change

- Keep the core package free of runtime package dependencies.
- Preserve the public wire contract of the official TypeSafe SDKs unless an API behavior is confirmed to differ.
- Do not add tests that require an API key to the default test suite.
- Never log or include authorization values in exceptions.

## Workflow

1. Add or change a test first and observe it fail for the expected reason.
2. Implement the smallest coherent behavior that makes it pass.
3. Run formatting, build, tests, package creation, and the external parity harness when the wire format changes.
4. Commit one coherent change with a Conventional Commit message such as `feat:`, `fix:`, `test:`, `docs:`, or `refactor:`.

```shell
dotnet restore --locked-mode
dotnet format JevNet.slnx --no-restore --verify-no-changes
dotnet build JevNet.slnx -c Release --no-restore
dotnet test JevNet.slnx -c Release --no-build
dotnet pack JevNet.slnx -c Release --no-build -o artifacts
```

## Public API changes

Public types need XML documentation, focused tests, and a changelog entry. Avoid adding aliases or overloads without a concrete caller use case: every public member becomes a compatibility commitment.

## Pull requests

Explain the user-visible behavior, tests added, compatibility impact, and any difference from the official JavaScript or Python SDK.

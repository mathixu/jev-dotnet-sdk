# Changelog

All notable changes to this project are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.2.0] - 2026-09-23

### Added

- First-class OpenRouter Decisions API support through `TypeSafeClientOptions.Provider`.
- Provider-specific defaults and environment variables for TypeSafe AI and OpenRouter.
- OpenRouter model discovery mapped to the existing `ModelCard` contract.

## [0.1.0] - 2026-09-22

### Added

- Typed Noul, Choice, and Score questions and answers.
- Async System One client with structured state support.
- Configurable timeouts, retries, retry headers, and cancellation.
- Typed API, connection, timeout, and response-validation exceptions.
- Available-model listing.
- Optional `Microsoft.Extensions.DependencyInjection` integration.
- Offline parity coverage against the official JavaScript and Python SDKs.

[Unreleased]: https://github.com/mathixu/jev-dotnet-sdk/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/mathixu/jev-dotnet-sdk/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/mathixu/jev-dotnet-sdk/releases/tag/v0.1.0

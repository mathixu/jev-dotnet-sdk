# Security policy

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting for this repository. Do not open a public issue containing an API key, authorization header, response body with customer data, or an exploitable proof of concept.

Include the affected version, impact, reproduction steps, and any suggested mitigation. You should receive an acknowledgement within seven days.

## Credential handling

JevNet reads `TYPESAFE_API_KEY` or an explicit `TypeSafeClientOptions.ApiKey`. The SDK protects authentication and content headers from caller overrides and never includes the API key in its own exception messages. Applications remain responsible for redacting request state and response bodies from their logs.

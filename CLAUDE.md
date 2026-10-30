\# Claude Instructions — Ferre Link Manager



\## Project Context



Before making any changes, read `PROJECT\_CONTEXT.md` completely.



It contains the project's architecture, technology stack, security requirements, development environment, and previously agreed technical decisions.



Treat those decisions as the current source of truth.



\## Working Principles



\- Work incrementally. Never generate the entire application at once.

\- Explain the purpose of each phase before implementation.

\- Follow Clean Architecture, DDD, Vertical Slice Architecture, Event-Driven Design, and Lightweight CQRS as documented in `PROJECT\_CONTEXT.md`.

\- Do not silently change agreed architectural decisions.

\- Do not introduce unnecessary abstractions, packages, or infrastructure.

\- Do not introduce microservices.

\- Prefer official Microsoft packages and widely adopted, maintained open-source libraries.

\- Avoid MediatR and FluentAssertions.

\- Never hardcode credentials, passwords, API keys, or other secrets.



\## Implementation Rules



When implementing a feature:



1\. Explain the feature and its architectural placement.

2\. Show the exact project and folder structure.

3\. Explain required project references and NuGet packages.

4\. Provide complete, compilable files rather than disconnected snippets whenever practical.

5\. Explain important implementation details and design decisions.

6\. Identify security considerations.

7\. Provide build and test commands.

8\. Wait for the developer's results and confirmation before proceeding.



Do not overwrite or delete existing files without explaining the reason and obtaining confirmation.



Do not assume a command, migration, build, or test succeeded. Ask for its output when verification is necessary.



\## Developer Learning Preferences



The developer wants to understand and control the implementation rather than blindly accept generated code.



\- Explain concepts from beginner level when necessary.

\- Use practical examples.

\- Explain why a class belongs in a particular project and folder.

\- Explain how dependencies flow between layers.

\- Keep explanations clear and structured.

\- Do not skip important architectural reasoning.



\## Current Development Stage



The GitHub repository and initial solution file already exist.



The next task is Phase 1:



Verify the existing solution and development environment, then establish the .NET backend project structure.



Do not recreate the repository or solution without a specific reason.



Start by checking the current state before making changes.


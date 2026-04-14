## Graph-First Workflow

For questions about this repository's architecture, dependencies, ownership, flows, or relationships, prefer the existing graph in `graphify-out/graph.json` when it is available. Do not block on graph tooling if it is missing.

### Default order of operations

1. Check whether `graphify-out/graph.json` exists.
2. If it exists, start with graph-backed exploration:
   - Use `/graphify explain "<node>"` for "what is this?"
   - Use `/graphify path "<a>" "<b>"` for dependency or flow tracing
   - Use `/graphify query "<question>"` for broader architecture questions
3. Use the graph results to identify the minimum set of source files or symbols to inspect next.
4. Only then open the specific files needed to verify implementation details or make changes.
5. Do not load large parts of the repo into context if the graph can narrow the search first.
6. If the graph is missing or graph tooling is unavailable, fall back to normal repository exploration with targeted search.

### When to refresh the graph

- If `graphify-out/graph.json` is missing, build it before answering repo-wide questions:
  - `graphify .`
- If code, docs, or diagrams changed since the last graph build, refresh first:
  - `graphify . --update`
- If `graphify` is not installed, skip these steps and continue with targeted file search and code inspection.

### Expectations for answers

- Prefer graph-backed answers for repository questions when the graph is sufficient.
- Call out when an answer comes from graph structure versus direct file inspection.
- If the graph is incomplete for the question, say so and then inspect only the files the graph points to.
- Do not fail or stop solely because `graphify` is unavailable on the current machine.

### Scope

Use this workflow for:
- architecture overviews
- dependency tracing
- subsystem explanations
- "where does X connect to Y?"
- "what owns this behavior?"
- change impact analysis

Do not rely on the graph alone for:
- exact line-level behavior
- final code edits
- test fixes that require implementation details

In those cases, use the graph first to narrow scope, then read the relevant files.

Check the following things only when the code has been changed:
Run dotnet format --verify-no-changes *.sln for checking the formatting and fixing formatting issues after code is written.
If CodeRabbit CLI is installed then run a local review after code is written.

The project needs to be AsyncAPI Spec version 3.0.0
check the asyncapi spec from https://github.com/asyncapi/spec/blob/v3.0.0/spec/asyncapi.md
Validate it against the current solution and update the ASYNCAPI_3_0_COMPATIBILITY_AUDIT file with new findings

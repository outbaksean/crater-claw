# Spec: investigate-child-agents

**Checkpoint:** investigate-child-agents
**Branch:** spec/investigate-child-agents
**Type:** Research

## Goal

Determine whether and how CraterClaw should support child agents — model invocations spawned by the primary agentic loop to handle sub-tasks. The output is either a notes file with a recommendation against implementing it (and why), or a notes file plus new checkpoints describing what to build.

## Research Questions

### 1. SK agent orchestration support

- What orchestration primitives does Semantic Kernel provide? (`ChatCompletionAgent`, `AgentGroupChat`, `KernelFunctionSelectionStrategy`, etc.)
- Do these primitives work with the SK Ollama connector, or do they depend on OpenAI-specific features?
- If full SK agent orchestration is not available for Ollama, what lighter alternatives exist within SK?
- What version of SK is currently in use and what is available at that version?

### 2. Feasible child agent unit

- What is the right unit of a child agent in CraterClaw terms? Candidates:
    - A kernel function that internally runs a full `IAgenticExecutionService` loop with a given profile and prompt
    - A separate SK kernel instance with a different plugin set
    - A different model on the same or a different provider
- How does a child agent relate to the behavior profile system? Does it use a profile, or is it configured ad hoc by the parent?
- Can a child agent use a different model than the parent (e.g. a large orchestrator model spawning a smaller task model)?

### 3. Use cases in CraterClaw

- What real tasks in CraterClaw's current behavior profiles are too complex for a single agentic loop?
- What would a concrete example look like — e.g. a parent agent that plans and a child agent that executes searches or file operations?
- Is the value in parallelism, specialization, or decomposition?

### 4. Constraints and risks

- Latency: chained Ollama calls compound. What is the expected overhead of a child invocation?
- Context: does a child agent share conversation history with the parent, or is it isolated?
- Loops: what prevents a child agent from spawning further children indefinitely?
- Error propagation: how does a failure in a child agent surface to the parent?
- Resource usage: running multiple Ollama inference requests concurrently on local hardware — is this practical?

### 5. UX surface

- How should child agent activity be visible in the web UI? (nested output, collapsible blocks, separate stream?)
- How should it appear in the console harness?
- Does the user need to approve or configure child agent invocations, or should they be fully automatic?

## Investigation Plan

1. Check the installed SK version and review the SK changelog/docs for agent orchestration support with non-OpenAI connectors.
2. Search for SK `ChatCompletionAgent` and `AgentGroupChat` usage with Ollama — official docs, GitHub issues, community examples.
3. Review `IAgenticExecutionService` and `SemanticKernelAgenticExecutionService` to assess how easily a child agent kernel function could wrap a nested execution.
4. Identify one or two concrete CraterClaw tasks that would plausibly benefit from child agents and sketch what the prompt/tool flow would look like.
5. Note any open questions that can only be resolved by a prototype.

## Output

`Specs/child-agents-notes.md` containing:

- Findings for each research question
- A recommendation: implement (with proposed checkpoints) or defer/skip (with rationale)
- Any open questions requiring a prototype to resolve

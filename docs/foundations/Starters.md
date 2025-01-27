# Starters

## Goals

* Understand how to integrate `Temporal Client` into an API
* Understand `WorkflowStartOptions` enough to make the right choice for your Use Case
* Understand how to Start OR Execute a Workflow (that doesn't exist!)
* Introduce Web UI and a Workflow Execution history

## Best Practices

#### WorkflowIds should have business meaning.

This identifier can be an AccountID, SessionID, etc. 

* Prefer _pushing_ an WorkflowID down instead of retrieving after-the-fact. 
* Acquaint your self with the "Workflow ID Reuse Policy" to fit your use case
Reference: https://docs.temporal.io/workflows#workflow-id-reuse-policy

#### Do not fail a workflow on intermittent (eg bug) errors; prefer handling failures at the Activity level within the Workflow.

A Workflow will very rarely need one to specify a RetryPolicy when starting a Workflow and we strongly discourage it.
Only Exceptions that inherit from `TemporalFailure` will cause a RetryPolicy to be enforced. Other Exceptions will cause the WorkflowTask
to be rescheduled so that Workflows can continue to make progress once repaired/redeployed with corrections.
Reference: 
* https://javadoc.io/doc/io.temporal/temporal-sdk/latest/io/temporal/client/WorkflowFailedException.html
* https://docs.temporal.io/encyclopedia/detecting-workflow-failures



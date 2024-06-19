# Timers

Our requirements demand we "wait" for approval before registering our entity with our CRM.
The onboarding must be completed within _seven days_ of submission. Let's wait that long before
exiting the Workflow as `Failed`.

Before diving into the matter of "Time" with Temporal Workflow code, be sure you have read over
https://github.com/temporalio/sdk-dotnet/tree/main?tab=readme-ov-file#workflow-logic-constraints.

Be sure you understand the implications Timer's have on Workflow determinism. 
Changing timers after deployment can be a subtle source of Non-Determinism Errors:
> The delay value can be Infinite or InfiniteTimeSpan but otherwise cannot be negative. A server-side timer is not created for infinite delays, so it is non-deterministic to change a timer to/from infinite from/to an actual value.

While some SDKs ignore zero timers, not writing a server-side timer, .NET does this:
> If the delay is 0, it is assumed to be 1 millisecond and still results in a server-side timer. Since Temporal timers are server-side, timer resolution may not end up as precise as system timers.

Dynamic duration values (eg., values received from an Activity or unvalidated input arguments) can sneak in causing
non-determinism errors or surprising results.

_Be sure you place assertions/guards on durations before using dynamic values in Timers._ 

Reference [Docs](https://dotnet.temporal.io/api/Temporalio.Workflows.Workflow.html#Temporalio_Workflows_Workflow_DelayAsync_System_TimeSpan_System_Nullable_System_Threading_CancellationToken___remarks).

## Testing 

How can we test something that is supposed to happen in the future?

## Refactorings

* Introduce an explicit `state` object we can use as a private variable inside our Workflow to track the progress of various changes in our Workflow
  * Here, we are going to wait for `IsApproved` to be flipped to `true` before proceeding
* Extend our `OnboardEntityRequest` to accept an optional input for the `timeout` of approval
  * Why not use `WorkflowExecutionTimeout`? 
    * Primarily because we want to express this as a _business rule_. We may want to extend this time later and we want to allow the workflow, not the caller, to govern execution rules.
    * Note that an approval could come in very late and while the activities that should complete are running they could be terminated due to the execution timeout. This would not be the intent of any time threshold.
  * Why not use a global constant for this delay timeout?
    * You can, but this will force a redeploy to make changes to product rules. We chose an optional input parameter with a fallback value to increase flexibility.
  * What about current tests that were built without this timeout constraint? There are two options:
    * We could use a "magic number" of `-1` to tell our Workflow to skip the approval
    * We could choose to be explicit and add `SkipApproval` to the input argument
      * This is the choice we make because it is clearer to maintainers
* Support an `Errors`  enumeration for raising a timed out OnboardEntity request.

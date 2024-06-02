# Temporal Jump Start -- Starters
We will demonstrate programmatic start/execution of a Workflow in Temporal. We will also discuss decisions that need to be made before you start development. 

## Workflow Execution
There are several ways to execute a Temporal Workflow. What is the best way to execute your Workflow from a UX perspective based on your use case?
* Execute from an API endpoint (for integration with existing applications)
* Execute from the command line
* Integrate with your CI/CD system
* Containerized execution
* Can Serverless be involved?

## Retry Handling
* Unify SDK grpc retry handling (survey across Golang, Java, TypeScript, and Rust SDKs)
    * Cleary dissuade from DIY retry noodling at gRPC level

## Workflow Retries
Retrying at the Workflow level is not necessary in most cases. It is better to handle failures at the Activity or the Child Workflow level than to restart a Workflow from the beginning. This is why our Workflows do not retry by default and the default Retry Policy for an Activity is to retry an unlimited number of times. However, there are a few scenarios where a Workflow Retry might be applicable. For instance, if your Workflow is dealing with processing on a specific host and that host goes down, you might want to Retry on a different host. (Ex. https://github.com/temporalio/samples-java/blob/main/core/src/main/java/io/temporal/samples/fileprocessing/FileProcessingWorkflowImpl.java) 

## Forwarding Authz/n Best Practices
* Forwarding Authz/n best practices

## Workflow ID Reuse Policies
A specific Workflow Execution is uniquely identified by the combination of the Workflow ID, Run ID and Namespace. The Workflow ID Reuse Policy sets the rules by which a Workflow ID may be reused within Temporal. By default, a Workflow ID can be reused in the same Namespace if the previous Workflow Executions have a Closed status within the current Retention Period. The policy is called `Allow Duplicate`. The policy is set to `Allow Duplicate Failed Only` allows duplicate Workflow IDs in the case of previous Workflow Executions resulting in a Failed, Timed Out, Terminated or Cancelled Workflow Execution. If you do not want to allow a duplicate Workflow ID not matter the Closed status during the Retention Period, then use the `Reject Duplicate` policy setting. Setting the policy to `Terminate if Running` will terminate the existing Workflow Execution if another one with the same Workflow ID is started.

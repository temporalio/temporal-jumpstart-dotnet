# Temporal Jump Start -- Starters
In this section, we will discuss some of the topics where decisions need to be made before you start development. These will help guide your development process and keep you from going down an incorrect path.

## Retry Handling
* Unify SDK grpc retry handling (survey across Golang, Java, TypeScript, and Rust SDKs)
    * Cleary dissuade from DIY retry noodling at gRPC level

## Workflow Retries
Retrying at the Workflow level is not necessary in most cases. Most often, it is better to handle failures at the Activity or the Child Workflow level than having to restart a Workflow from the beginning. This is why our Workflows do not retry by default and the default Retry Policy for an Activity is to retry an unlimited number of times. That being said, there are a few scenarios where a Workflow Retry might be applicable. For instance, if your Workflow is dealing with processing on a specific host and that host goes down, you might want to Retry on a different host (Ex. https://github.com/temporalio/samples-java/blob/main/core/src/main/java/io/temporal/samples/fileprocessing/FileProcessingWorkflowImpl.java) -- I didn't see a .NET example.  Any other scenarios?


Do we need to cover Reset and when it should be used?

## Forwarding Authz/n Best Practices
* Forwarding Authz/n best practices
## Workflow ID Reuse Policies
* WorkflowID reuse policies

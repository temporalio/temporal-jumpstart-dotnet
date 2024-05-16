# Starters

## Connection setup

- The `appsettings.Cloud.json` has an example of how to connect to Temporal Cloud.
- The `appsettings.Development.json` has an example of local Temporal connections.

Both support http/https for the `/onboardings` REST API.

Obviously, you need a Temporal Cloud namespace configured beforehand. 

// TODO implement script to bootstrap this when `make namespace` in conjunction with `mkcert`.

## Usage

### Local Temporal service

1. Start local Temporal with `temporal server start-dev`
2. Run `Temporal.Curriculum.Starters: local` application
   1. `dotnet run --launch-profile local`
3. You can issue a request with the Swagger UI that opens up OR
   1. Using your favorite HTTP Client send `PUT` request like
      1. `http PUT http://{HOSTNAME}/onboardings/onboarding-123 value=some-value`
4. Now issue a `GET` to the same resource to see the input parameters and execution status of your Workflow
   1. eg `http GET http://{HOSTNAME}/onboardings/onboarding-123`

**Expected outcome**

1. You should see a Workflow running  [here](http://localhost:8233/namespaces/default/workflows).
   1. WorkflowType: `WorkflowDefinitionDoesntExistYet`
   2. WorkflowId: `onboarding-123`
2. Enter the Workflow and you should see a "No Workers Running" caution and the first two events which indicate the Workflow has been scheduled to execute.

### Temporal Cloud service

1. Ensure you have configured `appsettings.Cloud.json` with the correct `Temporal` configuration
   1. mTLS certs should be configured
   2. Namespace should explicitly be declared
2. Run `Temporal.Curriculum.Starters: cloud` application in launchSettings `cloud` profile
   1. `dotnet run --launch-profile cloud`
3. You can issue a request with the Swagger UI that opens up OR
   1. Using your favorite HTTP Client send `PUT` request like
      1. `http PUT http://{HOSTNAME}/onboardings/onboarding-123 value=some-value`
4. Now issue a `GET` to the same resource to see the input parameters and execution status of your Workflow
   1. eg `http GET http://{HOSTNAME}/onboardings/onboarding-123`

**Expected outcome**

1. You should see a Workflow running in your [Temporal Cloud Namespace UI](https://cloud.temporal.io).
   1. WorkflowType: `WorkflowDefinitionDoesntExistYet`
   2. WorkflowId: `onboarding-123`
2. Enter the Workflow and you should see a "No Workers Running" caution and the first two events which indicate the Workflow has been scheduled to execute.

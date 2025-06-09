# Onboardings

Generated content ends up in the `Onboardings.Generated` project.

### Protobufs

#### Prerequisites
* [Buf](https://buf.build/docs/cli/installation/)

The messages used in the `onboardings` and `snailforce` services will be generated as follows.

```sh
cd {SolutionRoot}/src/Onboardings
# generate onboardings messages
buf generate --path proto/onboardings --template buf.gen.onboardings.yaml

# generate snailforce messages and service
buf generate --path proto/snailforce --template buf.gen.snailforce.yaml
```

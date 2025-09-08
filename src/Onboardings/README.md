# Onboardings

Generated content ends up in the `Onboardings.Generated` project.

### Protobufs

#### Prerequisites
* [Buf](https://buf.build/docs/cli/installation/)

The messages used in the `onboardings` and `snailforce` services will be generated as follows.

```sh
cd {SolutionRoot}/src/Onboardings
# generate messages
buf generate
```

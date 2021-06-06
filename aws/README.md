# PollQT AWS CDK

This sets up a Timestream database to ingest PollQT responses.

## Required Environment Variables

- `TIMESTREAM_DBNAME`: the Timestream database
- `CDK_DEFAULT_ACCOUNT`: the AWS account ID
- `CDK_DEFAULT_REGION`: the AWS region

## Useful commands

 * `npm run build`   compile typescript to js
 * `npm run watch`   watch for changes and compile
 * `npm run test`    perform the jest unit tests
 * `cdk deploy`      deploy this stack to your default AWS account/region
 * `cdk diff`        compare deployed stack with current state
 * `cdk synth`       emits the synthesized CloudFormation template

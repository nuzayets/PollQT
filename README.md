# PollQT
A NET Core application for polling account balances and positions from Questrade for recording.

It can be run by Telegraf with the `execd` plugin and it outputs InfluxDB Line Protocol.
It can also write the data to JSONL files.

Work in progress. If you somehow Googled "Questrade" and found this, these are not the droids you're looking for.

```
PollQT

Usage:
  PollQT [options]

Options:
  --work-dir <work-dir>    workDir
  --log-level <log-level>  logLevel [default: Information]
  --influx-output          influxOutput [default: True]
  --file-output            fileOutput [default: False]
  --log-console            logConsole [default: False]
  --log-file               logFile [default: True]
  --version                Show version information
  -?, -h, --help           Show help and usage information
```

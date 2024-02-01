## DefToIdc - convert RomRaider ECU and logger definitions to IDC script

This is fork of XmlToIdc. DefToIdc does absolutely the same, except it has options to specify file names and dirs.

**To run you need .NET 8 runtime installed**

Usage:

  &emsp;DefToIdc [command] [options]

|Options:||
|---|---|
  -a, --all-defs-dir \<all-defs-dir\>  |Directory where ECU defs, logger defs and .dtd file are placed.
  -l, --logger-dir \<logger-dir\>      |Directory where logger defs and .dtd file are placed.
  -e, --ecu-defs \<ecu-defs\>          |ECU definitions file name. [default: ecu_defs.xml]
  -g, --logger-defs \<logger-defs\>    |Logger definitions file name. [default: logger.xml]
  -d, --logger-dtd \<logger-dtd\>      |Logger dtd file name. [default: logger.dtd]
  --version                          |Show version information
  --keep-cal-id-symbol-case          |Keep CAL ID symbol case, do not transform to uppercase
  -?, -h, --help                     |Show help and usage information

|Commands:||
|---|---|
  t, tables \<cal-id\>                              |Convert tables only.
  s, stdparam \<cpu\> \<target\> \<cal-id\> \<ssm-base\>  |Convert standard parameters only.
  e, extparam \<cpu\> \<target\> \<ecu-id\>             |Convert extended parameters only.
  m, makeall \<target\> \<cal-id\> \<ssm-base\>         |Convert all - tables, standadrd parameters, extended parameters.

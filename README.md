# Cronos
[![NuGet](https://img.shields.io/nuget/v/Cronos.svg)](https://www.nuget.org/packages/Cronos) [![Build status](https://ci.appveyor.com/api/projects/status/4elmix39mvr18cqf?svg=true)](https://ci.appveyor.com/project/hangfireio/cronos) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=HangfireIO_Cronos&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=HangfireIO_Cronos) [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=HangfireIO_Cronos&metric=bugs)](https://sonarcloud.io/summary/new_code?id=HangfireIO_Cronos) [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=HangfireIO_Cronos&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=HangfireIO_Cronos)

Cronos is a .NET library for parsing Cron expressions and calculating next occurrences. It was designed with time zones in mind, and intuitively handles [Daylight saving time](https://en.wikipedia.org/wiki/Daylight_saving_time) (also known as Summer time) transitions as in *nix Cron.

*Please note this library doesn't include any task/job scheduler, it only works with Cron expressions.*

* Supports standard Cron format with optional seconds.
* Supports non-standard characters like `L`, `W`, `#` and their combinations.
* Supports schedule jitter via the `H` character, inspired by Jenkins.
* Supports reversed ranges, like `23-01` (equivalent to `23,00,01`) or `DEC-FEB` (equivalent to `DEC,JAN,FEB`).
* Supports time zones, and performs all the date/time conversions for you.
* Does not skip occurrences, when the clock jumps forward to Daylight saving time (known as Summer time).
* Does not skip interval-based occurrences, when the clock jumps backward from Summer time.
* Does not retry non-interval based occurrences, when the clock jumps backward from Summer time.
* Contains 1000+ unit tests to ensure everything is working correctly.

## Compatibility

This section explains how Cron expressions should be converted when moving to Cronos.

| Library    | Comments                                                                                                                                                                                                                                                                                                       |
|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Vixie Cron | When both day-of-month and day-of-week are specified, Cronos uses AND operator for matching (Vixie Cron uses OR operator for backward compatibility).                                                                                                                                                          |
| Quartz.NET | Cronos uses different, but more intuitive Daylight saving time handling logic (as in Vixie Cron). Full month names such as `september` aren't supported. Day-of-week field in Cronos has different values, `0` and `7` stand for Sunday, `1` for Monday, etc. (as in Vixie Cron). Year field is not supported. |
| NCrontab   | Compatible                                                                                                                                                                                                                                                                                                     |
| CronNET    | Compatible                                                                                                                                                                                                                                                                                                     |

## Installation

Cronos is distributed as a [NuGet package](http://www.nuget.org/packages/Cronos/), you can install it from the official NuGet Gallery. Please use the following command to install it using the NuGet Package Manager Console window.

```
PM> Install-Package Cronos
```

## Usage

We've tried to do our best to make Cronos API as simple and predictable in corner cases as possible. So you can only use `DateTime` with `DateTimeKind.Utc` specified (for example, `DateTime.UtcNow`), or `DateTimeOffset` classes to calculate next occurrences. You **cannot use** local `DateTime` objects (such as `DateTime.Now`), because this may lead to ambiguity during DST transitions, and an exception will be thrown if you attempt to use them.

To calculate the next occurrence, you need to create an instance of the `CronExpression` class, and call its `GetNextOccurrence` method. To search in reverse, use the `GetPreviousOccurrence` method. To learn about Cron format, please refer to the next section.

```csharp
using Cronos;

CronExpression expression = CronExpression.Parse("* * * * *");

DateTime? nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);
DateTime? previousUtc = expression.GetPreviousOccurrence(DateTime.UtcNow);
```

The `nextUtc` will contain the next occurrence in UTC, *after the given time*, or `null` value when it is unreachable (for example, Feb 30). The `previousUtc` will contain the most recent occurrence in UTC, *before the given time*, or `null` when nothing exists. If an invalid Cron expression is given, the `CronFormatException` exception is thrown.

### Working with time zones

It is possible to specify a time zone directly; in this case you should pass `DateTime` with `DateTimeKind.Utc` flag, or use `DateTimeOffset` class, since that is smart enough to always point to an exact, non-ambiguous instant.

```csharp
CronExpression expression = CronExpression.Parse("* * * * *");
TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

DateTime?       next = expression.GetNextOccurrence(DateTime.UtcNow, easternTimeZone);
DateTimeOffset? next = expression.GetNextOccurrence(DateTimeOffset.UtcNow, easternTimeZone);
DateTime?       previous = expression.GetPreviousOccurrence(DateTime.UtcNow, easternTimeZone);
DateTimeOffset? previous = expression.GetPreviousOccurrence(DateTimeOffset.UtcNow, easternTimeZone);
```

If you passed a `DateTime` object, resulting time will be in UTC. If you used `DateTimeOffset`, resulting object will contain the **correct offset**, so don't forget to use it especially during DST transitions (see below).

### Working with local time

If you just want to make all the calculations using local time, you'll have to use the `DateTimeOffset` class, because as I've said earlier, `DateTime` objects may be ambiguous during Summer time transitions. You can get the resulting local time, using the `DateTimeOffset.DateTime` property.

```csharp
CronExpression expression = CronExpression.Parse("* * * * *");
DateTimeOffset? next = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);

var nextLocalTime = next?.DateTime;
```

### Adding seconds to an expression

If you want to specify seconds, use another overload of the `Parse` method and specify the `CronFormat` argument as below:

```csharp
CronExpression expression = CronExpression.Parse("*/30 * * * * *", CronFormat.IncludeSeconds);
DateTime? next = expression.GetNextOccurrence(DateTime.UtcNow);
```

### Getting occurrences within a range

You can also get occurrences within a fixed date/time range using the `GetOccurrences` method. By default, the `from` argument will be included when matched, and `to` argument will be excluded. However, you can configure that behavior.

```csharp
CronExpression expression = CronExpression.Parse("* * * * *");
IEnumerable<DateTime> occurrences = expression.GetOccurrences(
    DateTime.UtcNow,
    DateTime.UtcNow.AddYears(1),
    fromInclusive: true,
    toInclusive: false);
```

To enumerate matches in reverse order, use the `GetOccurrencesDescending` method. The descending overloads treat the `from` argument as the upper bound and the `to` argument as the lower bound.

```csharp
IEnumerable<DateTime> previousOccurrences = expression.GetOccurrencesDescending(
    DateTime.UtcNow,
    DateTime.UtcNow.AddDays(-7),
    fromInclusive: true,
    toInclusive: false);
```

There are different overloads for these methods to support `DateTimeOffset` arguments or time zones.

## Cron format

Cron expression is a mask to define fixed times, dates and intervals. The mask consists of second (optional), minute, hour, day-of-month, month and day-of-week fields. All of the fields allow you to specify multiple values, and any given date/time will satisfy the specified Cron expression, if all the fields contain a matching value.

                                           Allowed values    Allowed special characters   Comment

    ┌───────────── second (optional)       0-59              * , - / H                    
    │ ┌───────────── minute                0-59              * , - / H                    
    │ │ ┌───────────── hour                0-23              * , - / H                    
    │ │ │ ┌───────────── day of month      1-31              * , - / H L W ?                
    │ │ │ │ ┌───────────── month           1-12 or JAN-DEC   * , - / H                    
    │ │ │ │ │ ┌───────────── day of week   0-6  or SUN-SAT   * , - / H # L ?              Both 0 and 7 means SUN
    │ │ │ │ │ │
    * * * * * *

### Base characters

In all fields you can use numbers, `*` to mark a field as *every value*, and `-` to specify ranges of values. Reversed ranges like `22-1` (equivalent to `22,23,0,1,2`) are also supported.

You can also use `H` to choose a *single value* left up to the implementation, for use cases where you might want to distribute load. This is a form a [schedule jitter](#jitter).

It's possible to define **steps** by combining `/` with `*`, `H`, numbers and ranges. For example, `*/5` in the minute field describes *every 5 minutes* and `1-15/3` in day-of-month field describes *every 3 days from the 1st to the 15th*. Pay attention that `*/24` is just equivalent to `0,24,48` and `*/24` in minute field doesn't literally mean *every 24 minutes* - it means *every 0,24,48 minute*.

Concatenate values and ranges by `,`. Comma works like `OR` operator. So `3,5-11/3,12` is equivalent to `3,5,8,11,12`.

In month and day-of-week fields, you can use names of months or days of weeks abbreviated to first three letters (`Jan-Dec` or `Mon-Sun`) instead of their numeric values. Full names like `JANUARY` or `MONDAY` **aren't supported**.

For day of week field, both `0` and `7` stays for Sunday, 1 for Monday.

| Expression           | Description                                                                           |
|----------------------|---------------------------------------------------------------------------------------|
| `* * * * *`          | Every minute                                                                          |
| `0  0 1 * *`         | At midnight, on day 1 of every month                                                  |
| `*/5 * * * *`        | Every 5 minutes                                                                       |
| `30,45-15/2 1 * * *` | Every 2 minute from 1:00 AM to 01:15 AM and from 1:45 AM to 1:59 AM and at 1:30 AM    |
| `0 0 * * MON-FRI`    | At 00:00, Monday through Friday                                                       |

### Special characters

Most expressions you can describe using base characters. If you want to deal with more complex cases like *the last day of month* or *the 2nd Saturday* use special characters:

**`L`** stands for "last". When used in the day-of-week field, it allows you to specify constructs such as *the last Friday* (`5L`or `FRIL`). In the day-of-month field, it specifies the last day of the month.

**`W`** in day-of-month field is the nearest weekday. Use `W`  with single value (not ranges, steps or `*`) to define *the nearest weekday* to the given day. In this case there are two base rules to determine occurrence: we should shift to **the nearest weekday** and **can't shift to different month**. Thus if given day is Saturday we shift to Friday, if it is Sunday we shift to Monday. **But** if given day is **the 1st day of month** (e.g. `0 0 1W * *`) and it is Saturday we shift to the 3rd Monday, if given day is **last day of month** (`0 0 31W 0 0`) and it is Sunday we shift to that Friday. Mix `L` (optionally with offset) and `W` characters to specify *last weekday of month* `LW` or more complex like `L-5W`.

**`#`** in day-of-week field allows to specify constructs such as *second Saturday* (`6#2` or `SAT#2`).

**`?`** is synonym of `*`. It's supported but not obligatory, so `0 0 5 * ?` is the same as `0 0 5 * *`.

| Expression        | Description                                              |
|-------------------|----------------------------------------------------------|
| `0 0 L   * *`     | At 00:00 AM on the last day of the month                 |
| `0 0 L-1 * *`     | At 00:00 AM the day before the last day of the month     |
| `0 0 3W  * *`     | At 00:00 AM, on the 3rd weekday of every month           |
| `0 0 LW  * *`     | At 00:00 AM, on the last weekday of the month            |
| `0 0 *   * 2L`    | At 00:00 AM on the last tuesday of the month             |
| `0 0 *   * 6#3`   | At 00:00 AM on the third Saturday of the month           |
| `0 0 ?   1 MON#1` | At 00:00 AM on the first Monday of the January           |

### Specify Day of month and Day of week

You can set both **day-of-month** and **day-of-week**, it allows you to specify constructs such as **Friday the thirteenth**. Thus `0 0 13 * 5` means at 00:00, Friday the thirteenth.

It differs from Unix crontab and Quartz cron implementations. Crontab handles it like `OR` operator: occurrence can happen in given day of month or given day of week. So `0 0 13 * 5` means *at 00:00 AM, every friday or every the 13th of a month*. Quartz doesn't allow specifying both day-of-month and day-of-week.

### Macro

A macro is a string starting with `@` and representing a shortcut for simple cases like *every day* or *every minute*.

| Macro           | Equivalent    | Comment                                                    |
|-----------------|---------------|------------------------------------------------------------|
| `@every_second` | `* * * * * *` | Run once a second                                          |
| `@every_minute` | `* * * * *`   | Run once a minute at the beginning of the minute           |
| `@hourly`       | `0 * * * *`   | Run once an hour at the beginning of the hour              |
| `@daily`        | `0 0 * * *`   | Run once a day at midnight                                 |
| `@midnight`     | `0 0 * * *`   | Run once a day at midnight                                 |
| `@weekly`       | `0 0 * * 0`   | Run once a week at midnight on Sunday morning              |
| `@monthly`      | `0 0 1 * *`   | Run once a month at midnight of the first day of the month |
| `@yearly`       | `0 0 1 1 *`   | Run once a year at midnight of 1 January                   |
| `@annually`     | `0 0 1 1 *`   | Run once a year at midnight of 1 January                   |

### Cron grammar

Cronos parser uses following case-insensitive grammar:

```
        cron ::= expression | macro
  expression ::= [second space] minute space hour space day-of-month space month space day-of-week
      second ::= field
      minute ::= field
        hour ::= field
day-of-month ::= '*' [step] | '?' [step] | 'H' [step] | lastday | value [ 'W' | range [list] ]
       month ::= field
 day-of-week ::= '*' [step] | '?' [step] | 'H' [step] | value [ dowspec | range [list] ]
       macro ::= '@every_second' | '@every_minute' | '@hourly' | '@daily' | '@midnight' | '@weekly' | '@monthly'|
                 '@yearly' | '@annually'
       field ::= '*' [step] | '?' [step] | 'H' [step] | value [range] [list]
        list ::= { ',' value [range] }
       range ::= '-' value [step] | [step]
        step ::= '/' number
       value ::= number | name
        name ::= month-name | dow-name
  month-name ::= 'JAN' | 'FEB' | 'MAR' | 'APR' | 'MAY' | 'JUN' | 'JUL' | 'AUG' | 'SEP' | 'OCT' | 'NOV' | 'DEC'
    dow-name ::= 'SUN' | 'MON' | 'TUE' | 'WED' | 'THU' | 'FRI' | 'SAT'
     dowspec ::= 'L' | '#' number
     lastday ::= 'L' ['-' number] ['W']
      number ::= digit | number digit
       space ::= ' ' | '\t'
```

## Daylight Saving Time

Cronos is the only library to handle daylight saving time transitions in an intuitive way with the same behavior as Vixie Cron (utility for *nix systems). During a spring transition, we don't skip occurrences scheduled to invalid time during. In an autumn transition we don't get duplicate occurrences for daily expressions, and don't skip interval expressions when the local time is ambiguous.

### Transition to Summer time (in spring)

During the transition to Summer time, the clock is moved forward, for example the next minute after `01:59 AM` is `03:00 AM`. So any daily Cron expression that should match `02:30 AM`, points to an invalid time. It doesn't exist, and can't be mapped to UTC.

Cronos adjusts the next occurrence to the next valid time in these cases. If you use Cron to schedule jobs, you may have shorter or longer intervals between runs when this happens, but you'll not lose your jobs:

```
"30 02 * * *" (every day at 02:30 AM)

Mar 13, 02:30 +03:00 – run
Mar 14, 03:00 +04:00 – run (adjusted)
Mar 15, 02:30 +04:00 – run
```

### Transition from Summer time (in autumn)

When Daylight Saving Time ends you set the clocks backward so there is duration which repeats twice. For example, after `01:59 AM` you get `01:00 AM` again, so the interval between `01:00 AM` to `02:00 AM` (excluding) is ambiguous, and can be mapped to multiple UTC offsets.

We don't want to have multiple occurrences of daily expressions during this transition, but at the same time we want to schedule interval expressions as usually, without skipping them. So we have different behavior for different Cron expressions.

#### Interval expression

Cron expression is **interval based** whose second, minute or hour field contains `*`, ranges or steps, e.g. `30 * * * *` (hour field), `* 1 * * *` (minute field), `0,5 0/10 1 * * *`. In this case there are expectations that occurrences should happen periodically during the day and this rule can't be broken by time transitions. Thus for **interval based** expressions occurrences will be before and after clock shifts.

Consider `*/30 * * * *` interval expression. It should occur every 30 minutes no matter what.

```
Nov 08, 00:30 +04:00 – run
Nov 08, 01:00 +04:00 – run
Nov 08, 01:30 +04:00 – run
Nov 08, 01:00 +03:00 – run
Nov 08, 01:30 +03:00 – run
Nov 08, 02:00 +03:00 – run
```

#### Non-interval expression

Cron expression is **non-interval based** whose second, minute or hour field **does not contain** `*`, ranges or steps, e.g. `0 30 1 * * *` or `0 0,45 1,2 * * *`. We expect they occur given number of times per day. Thus for **non-interval** expressions occurrences will be just before clock shifts.

Consider `30 1 * * *` non-interval expression. It should occur once a day no matter what.

```
Nov 07, 01:30 +04:00 – run
Nov 08, 01:30 +04:00 – run
Nov 08, 01:30 +03:00 – skip
Nov 09, 01:30 +03:00 – run
```

<a name="jitter"></a>
## Jitter

Cronos supports the ability to distribute cron fields randomly in order to spread out system load over time, a feature called "schedule jitter". You can opt into this capability by passing in a seed for a random number generator and optionally using the special character `H` in a cron expression. Using `H` in an expression while failing to provide a seed will throw an exception.

Example API usages:

```csharp
// if you're in a job-schedulig scenario, you would probably want to
// use information from the job itself as a seed, but this can be anything
var seed = jobId.GetHashCode();

// note that each of these 3 expressions are equivalent
var hourlyExpressionWithoutJitter = CronExpression.Parse("0 * * * *");
var hourlyMacroWithoutJitter      = CronExpression.Parse("@hourly");
var hourlyNamedWithoutJitter      = CronExpression.Hourly;

// and each of these are equivalent because the same seed was used
var hourlyExpressionWithJitter    = CronExpression.Parse("H H * * * *", CronFormat.IncludeSeconds, seed);
var hourlyMacroWithJitter         = CronExpression.Parse("@hourly", seed);
var hourlyNamedWithJitter         = CronExpression.HourlyWithJitter(seed);
```

Just as it is possible to generate impossible combinations in basic cron expressions (e.g. `* * 31 2 *` being February 31st), care should be taken when combining an `H` with other fields. One common protection is built-in: when `H` is used for the day of the month, the range is limited to the first 28 days of the month. However, expressions like `* * 31 H *` (i.e. the 31st day of a random month) share the same limitations as `* * 31 * *` for months that don't have a 31st day. 

The presence of a jitter seed also adjusts the behavior of some macros by offsetting the times by a random amount:

| Macro           | Equivalent    | Comment                                     |
|-----------------|---------------|---------------------------------------------|
| `@every_minute` | `H * * * * *` | Run once a minute at an unspecified time    |
| `@hourly`       | `H H * * * *` | Run once an hour at an unspecified time     |
| `@daily`        | `H H H * * *` | Run once a day at an unspecified time       |
| `@weekly`       | `H H H * * H` | Run once a week at an unspecified day/time  |
| `@monthly`      | `H H H H * *` | Run once a month at an unspecified day/time |
| `@yearly`       | `H H H H H *` | Run once a year at an unspecified day/time  |
| `@annually`     | `H H H H H *` | Run once a year at an unspecified day/time  |

## Benchmarks

Since [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) project appeared, it's hard to ignore the performance. We tried hard to make Cronos not only feature-rich, but also really fast when parsing expressions and calculating next occurrences. As a result, Cronos is faster more than in an order of magnitude than alternative libraries, here is a small comparison:

```
Cronos Method                               |           Mean |        StdDev
------------------------------------------- | -------------- | -------------
CronExpression.Parse("* * * * *")           |     30.8473 ns |     0.0515 ns
CronExpression.Parse("*/10 12-20 ? DEC 3")  |     81.5010 ns |     0.0924 ns
Simple.GetNextOccurrence(DateTime.UtcNow)   |    123.4712 ns |     0.5928 ns
Complex.GetNextOccurrence(DateTime.UtcNow)  |    212.0422 ns |     0.3997 ns

NCrontab Method                             |           Mean |        StdDev
------------------------------------------- | -------------- | -------------
CrontabSchedule.Parse("* * * * *")          |  1,813.7313 ns |     3.3718 ns
CrontabSchedule.Parse("*/10 12-20 * DEC 3") |  3,174.3625 ns |     6.8522 ns
Simple.GetNextOccurrence(DateTime.UtcNow)   |    147.7866 ns |     0.1689 ns
Complex.GetNextOccurrence(DateTime.UtcNow)  |  1,001.3253 ns |     1.6205 ns

Quartz Method                               |           Mean |        StdDev
------------------------------------------- | -------------- | -------------
new CronExpression("* * * * * ?")           | 48,157.7744 ns | 1,417.3101 ns
new CronExpression("* */10 12-20 ? DEC 3")  | 33,731.9992 ns |    38.3192 ns
Simple.GetTimeAfter(DateTimeOffset.Now)     |  1,416.9867 ns |     1.2784 ns
Complex.GetTimeAfter(DateTimeOffset.Now)    |  6,573.0269 ns |     7.9192 ns
```

## License

Copyright © 2017 Hangfire OÜ. Cronos is licensed under [The MIT License (MIT)][LICENSE].

[LICENSE]:LICENSE

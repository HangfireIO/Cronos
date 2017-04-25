# Cronos
[![NuGet](https://img.shields.io/nuget/v/Cronos.svg)](https://www.nuget.org/packages/Cronos) [![AppVeyor](https://img.shields.io/appveyor/ci/odinserj/cronos/master.svg?label=appveyor)](https://ci.appveyor.com/project/odinserj/cronos/branch/master) [![Travis](https://img.shields.io/travis/HangfireIO/Cronos/master.svg?label=travis)](https://travis-ci.org/HangfireIO/Cronos) [![Codecov branch](https://img.shields.io/codecov/c/github/HangfireIO/Cronos/master.svg)](https://codecov.io/gh/HangfireIO/Cronos)

Cronos is a .NET library for parsing Cron expressions and calculating next occurrences. It was designed with time zones in mind, and correctly handles [Daylight saving time](https://en.wikipedia.org/wiki/Daylight_saving_time) (also known as Summer time) transitions (as in *nix Cron).

*Please note this library doesn't include any task/job scheduler, it only works with Cron expressions.*

* Supports standard Cron format with optional seconds.
* Supports non-standard characters like `L`, `W`, `#` and their combinations.
* Supports reversed ranges, like `23-01` (equivalent to `23,00,01`) or `DEC-FEB` (equivalent to `DEC,JAN,FEB`).
* Supports time zones, and performs all the date/time conversions for you.
* Does not skip occurrences, when the clock jumps forward to Daylight saving time (knows as Summer time).
* Does not skip interval-based occurrences, when the clock jumps backward from Summer time.
* Does not retry non-interval based occurrences, when the clock jumps backward from Summer time.
* Contains 1000+ unit tests to ensure all is working correctly.

## Installation

Cronos is distributed as a [NuGet package](http://www.nuget.org/packages/Cronos/), you can install it from the official NuGet Gallery. Please use the following command to install it using the NuGet Package Manager Console window.

```
PM> Install-Package Cronos
```

## Usage

We've tried to do our best to make Cronos API as simple and predictable in corner cases as possible. So you can only use `DateTime` with `DateTimeKind.Utc` specified (for example, `DateTime.UtcNow`), or `DateTimeOffset` classes to calculate next occurrences. You **can not use** local `DateTime` objects (such as `DateTime.Now`), because this may lead to ambiguity during DST transitions, and an exception will be thrown if you attempt to use them.

To calculate the next occurrence, you need to create an instance of the `CronExpression` class, and call its `GetNextOccurrence` method. To learn about Cron format, please refer to the next section.

```csharp
using Cronos;

CronExpression expression = CronExpression.Parse("* * * * *");

DateTime? nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);
```

The `nextUtc` will contain the next occurrence in UTC, *after the given time*, or `null` value when it is unreachable (for example, Feb 30). If an invalid Cron expression is given, the `CronFormatException` exception is thrown.

### Working with time zones

It is possible to specify a time zone directly, in this case you should pass `DateTime` with `DateTimeKind.Utc` flag, or use `DateTimeOffset` class, that's is smart enough to always point to an exact, non-ambiguous instant.

```csharp
CronExpression expression = CronExpression.Parse("* * * * *");
TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

DateTime?       next = expression.GetNextOccurrence(DateTime.UtcNow, easternTimeZone));
DateTimeOffset? next = expression.GetNextOccurrence(DateTimeOffset.UtcNow, easternTimeZone);
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
DateTime? next = expression.GetNextOccurrence(DateTime.UtcNow));
```

## Cron format

Cron expression is a mask to define fixed times, dates and intervals. The mask consists of second (optional), minute, hour, day-of-month, month and day-of-week fields. All of the fields allow you to specify multiple values, and any given date/time will satisfy the specified Cron expression, if all the fields contain a matching value.

                                           Allowed values    Allowed special characters   Comment

    ┌───────────── second (optional)       0-59              * , - /                      
    │ ┌───────────── minute                0-59              * , - /                      
    │ │ ┌───────────── hour                0-23              * , - /                      
    │ │ │ ┌───────────── day of month      1-31              * , - / L W ?                
    │ │ │ │ ┌───────────── month           1-12 or JAN-DEC   * , - /                      
    │ │ │ │ │ ┌───────────── day of week   0-7  or MON-SUN   * , - / # L ?                Both 0 and 7 means SUN
    │ │ │ │ │ │
    │ │ │ │ │ │
    │ │ │ │ │ │
    * * * * * *

### Base characters

In all fields you can use number, `*` to mark field as *any value*, `-` to specify ranges of values. Reversed ranges like `22-1`(equivalent to `22,23,0,1,2`) are also supported.

It's possible to define **step** combining `/` with `*`, numbers and ranges. For example, `*/5` in minute field describes *every 5 minute* and `1-15/3` in day-of-month field – *every 3 days from the 1st to the 15th*. Pay attention that `*/24` is just equivalent to `0,24,48` and `*/24` in minute field doesn't literally mean *every 24 minutes* it means *every 0,24,48 minute*.

Concatinate values and ranges by `,`. Comma works like `OR` operator. So `3,5-11/3,12` is equivalent to `3,5,8,11,12`.

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

**`W`** in day-of-month field is the nearest weekday. Use `W`  with single value (not ranges, steps or `*`) to define *the nearest weekday* to the given day. In this case there are two base rules to determine occurrence: we should shift to **the nearest weekday** and **can't shift to different month**. Thus if given day is Saturday we shift to Friday, if it is Sunday we shift to Monday. **But** if given day is **the 1st day of month** (e.g. `0 0 1W * *`) and it is Saturday we shift to the 3rd Monday, if given day is **last day of month** (`0 0 31W 0 0`) and it is Sunday we shift to that Friday. Mix `L` (optionaly with offset) and `W` characters to specify *last weekday of month* `LW` or more complex like `L-5W`.

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

It differs from Unix crontab and Quartz cron implementations. Crontab handles it like `OR` operator: occurrence can happen in given day of month or given day of week. So `0 0 13 * 5` means *at 00:00 AM, every friday or every the 13th of a month*. Quartz doesn't allow specify both day-of-month and day-of-week.

### Macro

A macro is a string starting with `@` and representing a shortcut for simple cases like *every day* or *every minute*.

| Macro          | Equivalent    |
|----------------|---------------|
|`@every_second` | `* * * * * *` |
|`@every_minute` | `* * * * *`   |
|`@hourly`       | `0 * * * *`   |
|`@daily`        | `0 0 * * *`   |
|`@midnight`     | `0 0 * * *`   |
|`@weekly`       | `0 0 * * 0`   |
|`@monthly`      | `0 0 1 * *`   |
|`@yearly`       | `0 0 1 1 *`   |
|`@annually`     | `0 0 1 1 *`   |

### Cron grammar

Cronos parser uses following case-insensitive grammar:

```
cron :: expression | macro
expression :: [second space] minute space hour space day-of-month space month space day-of-week
second :: field
minute :: field
hour :: field
day-of-month :: '*' step | lastday | value [ 'W' | range [list] ] | '?'
month :: field
day-of-week :: '*' step | value [ dowspec | range [list] ] | '?'
macro :: '@every_second' | '@every_minute' | '@hourly' | '@daily' | '@midnight' | '@weekly' | '@monthly' |
         '@yearly' | '@annually'
field :: '*' step | value [range] [list] | '?'
list :: { ',' value [range] }
range :: '-' value [step] | [step]
step :: '/' number
value :: number | name
name :: month-name | dow-name
month-name :: 'JAN' | 'FEB' | 'MAR' | 'APR' | 'MAY' | 'JUN' | 'JUL' | 'AUG' | 'SEP' | 'OCT' | 'NOV' | 'DEC'
dow-name :: 'SUN' | 'MON' | 'TUE' | 'WED' | 'THU' | 'FRI' | 'SAT'
dowspec :: 'L' | '#' number
lastday :: 'L' ['-' number] ['W']
number :: digit | number digit
space :: ' ' | '\t'
```

## Daylight Saving Time

Cronos is the only library to handle daylight saving time transitions correctly. 

There aren't skipping occurrences on Standard Time (ST) to Daylight Saving Time (DST) transitions (when the clock jumps forward). Also there are no skipping interval based (whose second, minute or hour field contains `*`, ranges or steps) and no duplicate non-interval based occurrences on DST to ST transitions (backward jump).

### Standard Time to Daylight Saving Time transition

When Standard Time (ST) transit to Daylight Saving Time (DST) the clock jumps forward. So there is non-existing, invalid time and if an occurrence falls on that time we can't just skip it. Instead we shift that occurrence to next valid time. 

**Example:**

2016-03-13 is the day when Daylight Saving Time starts in Eastern time zone. The clocks jump from `02:00 AM -05:00` to `03:00 AM -04:00`. So duration from `02:00 AM` to `02:59 AM` is invalid.

Consider `0 30 2 * * *` expression.

It should occur at `02:30 AM` every day. **But** `2016-03-13 02:30 AM` is invalid and that occurrence shifts to next valid time `2016-03-13 03:00 AM -04:00`. Thus occurrences of given expression will be at:
* `2016-03-12 02:30 AM`,
* `2016-03-13 03:00 AM`, **it shifted** from `2016-03-13 02:30 AM`
* `2016-03-14 02:30 AM`,
* so on at `02:30 AM` every day.

### Daylight Saving Time to Standard Time transition

When Daylight Saving Time ends you set the clocks backward so there is duration which repeats twice. If next occurrence falls on this duration behavior depends on kind of cron expression: non-interval based or interval based. 

*We will consider Eastern time zone in examples. 2016-11-06 is the day when Daylight Saving Time ends. The clocks jump from `02:00 AM -04:00` to `01:00 AM -05:00`. So **duration from 01:00 AM to 01:59 AM is ambiguous** because it exists in two offsets.*

#### Non-interval based expression

Cron expression is **non-interval based** whose second, minute or hour field **does not contain** `*`, ranges or steps, e.g. `0 30 1 * * *` or `0 0,45 1,2 * * *`. We expect they occur given number of times per day. Thus for **non-interval** expressions occurrences will be just before clock shifts.

**Example:**

Consider `30 1 * * *` non-interval expression.

It should occur once a day no matter what. Thus occurrences in Eastern time zone will be at:
* `2016-11-05 01:30 AM -04:00`, 
* `2016-11-06 01:30 AM -04:00`, 
* ~~`2016-11-06 01:30 AM -05:00`~~, **it was skipped**,
* `2016-11-07 01:30 AM -05:00`
* so on at `01:30` every day.

#### Interval based expression

Cron expression is **interval based** whose second, minute or hour field contains `*`, ranges or steps, e.g. `0 30 * * * *`, `0 * 1 * * *`, `0,5 0/10 1 * * *`. In this case there are expectations that occurrences should happen periodically during the day and this rule can't be broken by time transitions. Thus for **interval based** expressions occurrences will be before and after clock shifts.

**Example:**

Consider `30 * * * *` interval expression.

It should occur every 30 minutes no matter what. Thus occurrences Eastern time zone will be on 2016-11-06 at:
* `00:00 AM -04:00`, 
* `00:30 AM -04:00`, 
* `01:30 AM -04:00`,
* `01:30 AM -05:00`, **it wasn't skipped**,
* `02:30 AM -05:00`,
* so on every 30 minutes.

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

Cronos is under the [Apache License 2.0][Apache-2.0].

Copyright © 2016-2017 Sergey Odinokov.

[Apache-2.0]:LICENSE

# Cronos
[![NuGet](https://img.shields.io/nuget/v/Cronos.svg)](https://www.nuget.org/packages/Cronos) [![AppVeyor](https://img.shields.io/appveyor/ci/odinserj/cronos/master.svg?label=appveyor)](https://ci.appveyor.com/project/odinserj/cronos/branch/master) [![Travis](https://img.shields.io/travis/HangfireIO/Cronos/master.svg?label=travis)](https://travis-ci.org/HangfireIO/Cronos) [![Codecov branch](https://img.shields.io/codecov/c/github/HangfireIO/Cronos/master.svg)](https://codecov.io/gh/HangfireIO/Cronos)

**Cronos** is a .NET library for parsing Cron expressions and calculating next occurrences, that targets .NET Framework and .NET Standard. It was designed with time zones in mind, and correctly handles forward/backward Daylight Saving Time transitions (as in *nix Cron). And it's blazingly fast!

*Please note this library doesn't include any task/job scheduler, it only works with Cron expressions.*

* Supports **standard Cron format** with optional seconds.
* Supports **non-standard characters** like `L`, `W`, `#` and their combinations.
* Supports **reversed ranges**, like `23-01` (equivalent to `23,00,01`) or `DEC-FEB` (equivalent to `DEC,JAN,FEB`).
* Supports **time zones**, and performs all the date/time conversions for you.
* **Does not skip** occurrences on Standard Time (ST) to Daylight Saving Time (DST) transitions (when the clock jumps forward).
* **Does not skip** interval-based occurrences on DST to ST transitions (backward jump).
* **Does not retry** non-interval based occurrences on DST to ST transitions (backward jump).
* When both *day of week* and *day of month* specified, *AND* operator will be used (different than in *nix Cron).
* For day of week field, 0 and 7 stays for Sunday, 1 for Monday.
* Contains 1000+ unit tests to ensure all is working correctly.

## Installation

Cronos is distributed as a NuGet package, you can install it from the official NuGet Gallery. Please use the following command to install it using the NuGet Package Manager Console window.

```
PM> Install-Package Cronos
```

## Usage

We've tried to do our best to make Cronos API as simple and predictable in corner cases as possible. To calculate the next occurrence, you need to create an instance of the `CronExpression` class, and call its `GetNextOccurrence` method. To learn about Cron format, please see the next section.

```csharp
using Cronos;

CronExpression expression = CronExpression.Parse("* * * * *");

DateTime? nextLocal = expression.GetNextOccurrence(DateTime.Now);
DateTime? nextUtc   = expression.GetNextOccurrence(DateTime.UtcNow);
```

Both the `nextLocal` and `nextUtc` will contain the next occurrence, *after the given time*, or `null` value when it is unreachable (for example, Feb 30).

All the time zone handling logic will be done behind the scenes: `nextLocal` will contain an occurrence in the `TimeZoneInfo.Local` time zone with `DateTimeKind.Local` specified, and `nextUtc` will contain an occurrence in the `TimeZoneInfo.Utc` zone with `DateTimeKind.Utc` specified. All Daylight Saving Time transition's corner cases are handled automatically (see below).

When invalid Cron expression is given, an instance of the `CronFormatException` class is thrown.

### Passing custom DateTime or DateTimeOffset

When dealing with custom `DateTime` instances, always specify its `Kind` property (for example, using the `DateTime.SpecifyKind` method). When a date/time with `DateTimeKind.Unspecified` is given, Cronos will throw the `ArgumentException`, because it's unclear what time zone to use, and the result is prone to errors.

```csharp
CronExpression expression = CronExpression.Parse("* * * * *");
DateTime from = new DateTime(2017, 03, 21, 18, 23, 00, DateTimeKind.Local); // Or DateTimeKind.Utc

DateTime? next = expression.GetNextOccurrence(from);
```

If you are using the `DateTimeOffset` class, you either need to convert it to local or UTC first (using `UtcDateTime` or `LocalDateTime` properties), or specify a time zone explicitly (please see the next section).

```csharp
DateTime? next = expression.GetNextOccurrence(DateTimeOffset.Now.UtcDateTime);
```

### Working with time zones

It is possible to specify a time zone directly, but in this case you should always pass `DateTime` with `DateTimeKind.Utc` flag, or use `DateTimeOffset` class.

```csharp
CronExpression expression = CronExpression.Parse("* * * * *");
TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

DateTime?       next = expression.GetNextOccurrence(DateTime.UtcNow, easternTimeZone));
DateTimeOffset? next = expression.GetNextOccurrence(DateTimeOffset.UtcNow, easternTimeZone);
```

Resulting time will be in UTC. All Daylight Saving Time transition's corner cases are handled automatically (see below).

### Adding seconds to an expression

If you want to specify seconds, use another overload of the `Parse` method and specify the `CronFields` argument as below:

```csharp
CronExpression expression = CronExpression.Parse("*/30 * * * * *", CronFields.IncludeSeconds);
DateTime? next = expression.GetNextOccurrence(DateTime.UtcNow));
```

## Cron format

**Cronos** uses a cron expression comprising five or six fields separated by white space that represents a set of times.

                                             Allowed values     Allowed special characters     Comment
												                                           
    ┌───────────── second (optional)         0-59               * , - /                    
    │ ┌───────────── minute                  0-59               * , - /                    
    │ │ ┌───────────── hour                  0-23               * , - /                    
    │ │ │ ┌───────────── day of month        1-31               * , - / L W ?              
    │ │ │ │ ┌───────────── month             1-12 or JAN-DEC    * , - /                    
    │ │ │ │ │ ┌───────────── day of week     0-7  or MON-SUN    * , - / # L ?                  0 and 7 means SUN
    │ │ │ │ │ │
    │ │ │ │ │ │
    │ │ │ │ │ │
    * * * * * *

**Star `*`**

`*` means any value. Used to select all values within a field. For example, `*` in the hour field means "every hour":

| Expression    | Description                          |
|---------------|--------------------------------------|
| `* * * * * *` | Every second                         |
| `* * * * *`   | Every minute                         |
| `30 3 * * *`  | At 3:30 AM every day                 |
| `0  0 1 * *`  | At midnight, on day 1 of every month |

**Comma `,`**

Commas are used to separate items of a list.

| Expression        | Description                           |
|-------------------|---------------------------------------|
| `15,45 * * * * *` | Every minute at 15 and 45 seconds     |
| `* * * * SAT,SUN` | Every minute on saturdays and sundays |
| `* * * * 6,7`     | Every minute on saturdays and sundays |
| `* * * * 0,6`     | Every minute on saturdays and sundays |

**Hyphens `-`**

Hyphens define ranges. 

| Expression        | Description                                                       |
|-------------------|-------------------------------------------------------------------|
| `0-30 1 * * *`    | Every minute between 01:00 AM and 01:30 AM                        |
| `45-15 1 * * *`   | Every minute from 1:00 AM to 01:15 AM and from 1:45 AM to 1:59 AM |
| `0 0 * * MON-FRI` | At 00:00, Monday through Friday                                   |

**L character**

`L` stands for "last". When used in the day-of-week field, it allows you to specify constructs such as "the last Friday" (`5L`) of a given month. In the day-of-month field, it specifies the last day of the month.

| Expression    | Description                                          |
|---------------|------------------------------------------------------|
| `0 0 L * *`   | At 00:00 AM on the last day of the month             |
| `0 0 L-1 * *` | At 00:00 AM the day before the last day of the month |
| `0 0 * * 1L`  | At 00:00 AM on the last monday of the month          |

**W character**

`W` character is allowed for the day-of-month field. This character is used to specify the weekday (Monday-Friday) nearest the given day. As an example, if you were to specify `15W` as the value for the day-of-month field, the meaning is: "the nearest weekday to the 15th of the month." So, if the 15th is a Saturday, occurrence is Friday the 14th. If the 15th is a Sunday, occurrence is Monday the 16th. If the 15th is a Tuesday, then occurrence is Tuesday the 15th. However, if you specify "1W" as the value for day-of-month, and the 1st is a Saturday, occurrence will be the 3rd, as it does not 'jump' over the boundary of a month's days. The 'W' character can be specified only when the day-of-month is a single day, not a range or list of days.

| Expression        | Description                                              |
|-------------------|----------------------------------------------------------|
| `0 0 1W * *`      | At 00:00 AM, on the first weekday of every month         |
| `0 0 10W * *`     | At 00:00 AM on the weekday nearest day 10 of every month |
| `0 0 LW * *`      | At 00:00, on the last weekday of the month               |

**Hash `#`**

`#` is allowed for the day-of-week field, and must be followed by a number between one and five. It allows you to specify constructs such as "the second Friday" of a given month. 

| Expression        | Description                                              |
|-------------------|----------------------------------------------------------|
| `0 0 * * 6#3`     | At 00:00 AM on the third Friday of the month             |
| `0 0 * * 1#1`     | At 00:00 AM on the first Monday of the month             |
| `0 0 * 1 1#1`     | At 00:00 AM on the first Monday of the January           |

**Question mark `?`**

`?` is "no specific value" and a synonym of `*`. It's supported but **non-obligatory**. `0 0 5 * *` is the same as `0 0 5 * ?`. You can specify `?` only in one field. For example, `* * ? * ?` is wrong expression.

| Expression    | Description                          |
|---------------|--------------------------------------|
| `* * * * * ?` | Every second                         |
| `* * * ? * *` | Every second                         |
| `* * * * ?`   | Every minute                         |
| `* * ? * *`   | Every minute                         |
| `0  0 1 * ?`  | At midnight, on day 1 of every month |
| `0  0 ? * 1`  | At midnight every Monday             |

**Slash `/`**

Slashes can be combined with ranges to specify step values. 

| Expression        | Description                                                                                |
|-------------------|--------------------------------------------------------------------------------------------|
| `*/5 * * * * *`   | Every 5 seconds                                                                            |
| `0 1/5 * * *`     | Every 5 hours, starting at 01:00                                                           |
| `*/30 */6 * * *`  | Every 30 minutes, every 6 hours: at 00:00, 00:30, 06:00, 06:30, 12:00, 12:30, 18:00, 18:30 |
| `0 0  15/2 * *`   | At 00:00, every 2 days, starting on day 15 of the month                                    |
| `0 0 * 2/3 *`     | At 00:00, every 3 months, February through December                                        |
| `0 0 * * 1/2`     | At 00:00, every 2 days of the week, starting on Monday                                     |

**Specify Day of month and Day of week**

You can specify both Day of month and Day of week, it allows you to specify constructs such as "Friday the thirteenth". 

| Expression        | Description                                                                                |
|-------------------|--------------------------------------------------------------------------------------------|
| `0 0 13 * 5`      | At 00:00, Friday the thirteenth                                                            |
| `0 0 13 2 5`      | At 00:00, Friday the thirteenth, only in February                                          |

## Daylight Saving Time

**Cronos** handles the transition from standard time (ST) to Daylight saving time (DST). 

### Setting the clocks forward

If next occurrence falls on invalid time when the clocks jump forward then next occurrence will shift to next valid time. See example:

```csharp
var expression = CronExpression.Parse("0 30 2 * * *");
var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

// 2016-03-13 - the day when DST starts in Eastern time zone. The clocks jump from 1:59 am ST to 3:00 am DST. 
// So duration from 2:00 am to 2:59 am is invalid.

var startTime = new DateTimeOffset(2016, 03, 13, 01, 50, 00, easternTimeZone.BaseUtcOffset);

// Should be scheduled to 2:30 am ST but that time is invalid. Next valid time is 3:00 am DST.
var next = expression.GetOccurrenceAfter(startTime, easternTimeZone);

Console.WriteLine("Next occurrence at " + next);

// Next occurrence at 2016-03-13 03:00:00 AM -04:00
```

### Setting the clocks backward

When DST ends you set the clocks backward so you have duration which repeats twice. If you are in USA the duration was e.g. 2016/11/06 from 1:00 am to 1:59 am. If next occurrence falls on this duration behavior depends on kind of cron expression: non-interval or interval.

#### Non-interval

Cron expression is non-interval if it describes certain time of a day, e.g. `"0 30 1 * * ?"` - 1:30 am every day, or `"0 0,45 1,2 * * ?"` - 1:00 am, 1:45 am, 2:00 am, 2:45 am every day. In this case each cron job will be scheduled only before clock shifts. Reason is when you describe certain time of day you mean that it should be scheduled once a day regardless whether there is clock shifts in that day.

```csharp
var expression = CronExpression.Parse("0 30 1 * * ?");
var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

var startTime = new DateTime(2016, 11, 06, 00, 59, 00);
var startTimeWithOffset = new DateTimeOffset(startTime, easternTimeZone.GetUtcOffset(startTime));

var next = expression.GetOccurrenceAfter(startTimeWithOffset, easternTimeZone);
Console.WriteLine("Next occurrence at " + next);

next = expression.GetOccurrenceAfter(next.Value);
Console.WriteLine("Next occurrence at " + next);

// Next occurrence at 2016-03-13 01:30:00 AM -04:00
// Next occurrence at 2016-03-13 02:30:00 AM -05:00
```

#### Interval

Cron expression is interval if it describes secondly, minutely or hourly job, e.g. `"0 30 * * * ?"`, `"0 * 1 * * ?"`, `"0,5 */10 * * * ?"`. In this case each cron job will be scheduled before and after clock shifts.

```csharp
var expression = CronExpression.Parse("0 30 * * * ?");
var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

var startTime = new DateTime(2016, 11, 06, 00, 59, 00);
var startTimeWithOffset = new DateTimeOffset(startTime, easternTimeZone.GetUtcOffset(startTime));

var next = expression.GetOccurrenceAfter(startTimeWithOffset, easternTimeZone);
Console.WriteLine("Next occurrence at " + next);

next = expression.GetOccurrenceAfter(next.Value);
Console.WriteLine("Next occurrence at " + next);

next = expression.GetOccurrenceAfter(next.Value);
Console.WriteLine("Next occurrence at " + next);

// Next occurrence at 2016-11-06 01:30:00 AM -04:00
// Next occurrence at 2016-11-06 01:30:00 AM -05:00
// Next occurrence at 2016-11-06 02:30:00 AM -05:00
```

## License

Cronos is under the [Apache License 2.0][Apache-2.0].

[Apache-2.0]:LICENSE

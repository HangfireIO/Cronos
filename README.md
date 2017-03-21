# Cronos
[![NuGet](https://img.shields.io/nuget/v/Cronos.svg)](https://www.nuget.org/packages/Cronos) [![AppVeyor](https://img.shields.io/appveyor/ci/odinserj/cronos/master.svg?label=appveyor)](https://ci.appveyor.com/project/odinserj/cronos/branch/master) [![Travis](https://img.shields.io/travis/HangfireIO/Cronos/master.svg?label=travis)](https://travis-ci.org/HangfireIO/Cronos)

Cronos is .NET library to parse [Cron expressions](https://en.wikipedia.org/wiki/Cron#CRON_expression) and calculate occurrences for them. Cronos works correctly regardless of the time zone: **UTC**, **Local** or any other. You  shouldn't care about [Daylight saving time](https://en.wikipedia.org/wiki/Daylight_saving_time) because Cronos deals with it. When Daylight saving time [starts](#setting-the-clocks-forward) (the clock jumps forward) no jobs will be missed, when Daylight saving time [ends](#setting-the-clocks-backward) (the clock jumps forward) [interval jobs](#interval) won't be missed, [non-interval jobs](#non-interval) won't be repeated.

## Features

* Calculate occurrences in UTC and custom time zone. Handle the transition from standard time to **daylight saving time** and vice versa.
* Parse cron expressions comprising five or six fields. See [Cron format](#cron-format).
* Support extended format with non-standard characters: `?`, `L`, `W`, `#`.

## Installation

```
PM> Install-Package Cronos
```

## Usage

### Get next occurrence

Get next occurrence of **every minute** job:

```csharp
var expression = CronExpression.Parse("* * * * *");
var next = expression.GetOccurrenceAfter(DateTime.UtcNow);
```

### Using local time zone

Calculate next occurrences in **Local** time zone. Notice that `Kind` property of passing parameter mast be `DateTimeKind.Local`:

```csharp
var expression = CronExpression.Parse("* * * * *");
var next = expression.GetOccurrenceAfter(DateTime.Now);
```

### Secondly jobs

Use cron expressions containing seconds:

```csharp
var expression = CronExpression.Parse("* * * * * *", CronFields.IncludeSeconds);
var next = expression.GetOccurrenceAfter(DateTime.UtcNow));
```

### Specify custom time zone

Calculate next occurrences in custom time zone. Notice that `Kind` property of passing parameter mast be `DateTimeKind.Utc`:

```csharp
var expression = CronExpression.Parse("30 * * * *");
var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
var startUtc = DateTime.UtcNow;

var next = expression.GetOccurrenceAfter(startUtc, easternTimeZone));
```

### Using DateTimeOffset

```csharp
var expression = CronExpression.Parse("30 * * * *");
var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

var next = expression.GetOccurrenceAfter(DateTimeOffset.Now, easternTimeZone));
```

### Friday the thirteenth

Find next friday the thirteenth:

```csharp
var expression = CronExpression.Parse("0 0 13 * FRI");
var friday13th = expression.GetOccurrenceAfter(DateTime.Now);
```

### First weekday of each month

Cronos support special characters `L`, `W`, `#` and `?`. So you can specify "first weekday of each month":

```csharp
var expression = CronExpression.Parse("0 0 1W * *");
var firstWeekDay = expression.GetOccurrenceAfter(DateTime.Now);
```

Read [cron format describing](#cron-format) to learn more about using non-standard characters. 

### Daylight Saving Time

**Cronos** handles the transition from standard time (ST) to Daylight saving time (DST). 

#### Setting the clocks forward

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

#### Setting the clocks backward

When DST ends you set the clocks backward so you have duration which repeats twice. If you are in USA the duration was e.g. 2016/11/06 from 1:00 am to 1:59 am. If next occurrence falls on this duration behavior depends on kind of cron expression: non-interval or interval.

##### Non-interval

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

##### Interval

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

`W` character is allowed for the day-of-month field. This character is used to specify the weekday (Monday-Friday) nearest the given day. As an example, if you were to specify `15W` as the value for the day-of-month field, the meaning is: "the nearest weekday to the 15th of the month." So, if the 15th is a Saturday, `Next` returns Friday the 14th. If the 15th is a Sunday, `Next` returns Monday the 16th. If the 15th is a Tuesday, then `Next` returns Tuesday the 15th. However, if you specify "1W" as the value for day-of-month, and the 1st is a Saturday, `Next` returns the 3rd, as it does not 'jump' over the boundary of a month's days. The 'W' character can be specified only when the day-of-month is a single day, not a range or list of days.

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


## License

Cronos is under the [Apache License 2.0][Apache-2.0].

[Apache-2.0]:LICENSE

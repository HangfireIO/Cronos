# Cronos

## Build status

TBD.

## Overview

High performance, full-featured Cron implementation for .NET.

## Installation

TBD

## Usage

### Format

A CRON expression is a string comprising six or seven fields separated by white space that represents a set of times, normally as a schedule to execute some routine.

| Field        | Required | Allowed values  | Allowed special charecters | Comment                  |
|--------------|----------|-----------------|----------------------------|--------------------------|
| Seconds      | Yes      | 0-59            | * , - /                    |                          |
| Minutes      | Yes      | 0-59            | * , - /                    |                          |
| Hours        | Yes      | 0-23            | * , - /                    |                          |
| Day of month | Yes      | 1-31            | * , - / ? L W              |                          |
| Month        | Yes      | 1-12 or JAN-DEC | * , - /                    |                          |
| Day of week  | Yes      | 0-7 or SUN-SAT  | * , - / ? L #              | 0 and 7 standing for SUN |
| Year         | No       | 1970–2099       | * , - /                    |                          |

*
:  "All values". Used to select all values within a field. For example, '*'' in the hour field means "every hour".

**,**
:  Commas are used to separate items of a list. For example, using "MON,WED,FRI" in the 5th field (day of week) means Mondays, Wednesdays and Fridays.

**-**
:  Hyphens define ranges. For example, 2000-2010 indicates every year between 2000 and 2010, inclusive.

**L**
:  'L' stands for "last". When used in the day-of-week field, it allows you to specify constructs such as "the last Friday" ("5L") of a given month. In the day-of-month field, it specifies the last day of the month.

**W**
:  The 'W' character is allowed for the day-of-month field. This character is used to specify the weekday (Monday-Friday) nearest the given day. As an example, if you were to specify "15W" as the value for the day-of-month field, the meaning is: "the nearest weekday to the 15th of the month." So, if the 15th is a Saturday, the trigger fires on Friday the 14th. If the 15th is a Sunday, the trigger fires on Monday the 16th. If the 15th is a Tuesday, then it fires on Tuesday the 15th. However, if you specify "1W" as the value for day-of-month, and the 1st is a Saturday, the trigger fires on Monday the 3rd, as it does not 'jump' over the boundary of a month's days. The 'W' character can be specified only when the day-of-month is a single day, not a range or list of days.

**#**
:  '#' is allowed for the day-of-week field, and must be followed by a number between one and five. It allows you to specify constructs such as "the second Friday" of a given month. For example, entering "6#3" in the day-of-week field corresponds to the third Friday of every month.

**?**
:  "No specific value". Useful when you need to specify something in one of the two fields in which the character is allowed, but not the other. For example, if I want my trigger to fire on a particular day of the month (say, the 10th), but don’t care what day of the week that happens to be, I would put "10" in the day-of-month field, and "?" in the day-of-week field. See the examples below for clarification. 

**/**
:  Slashes can be combined with ranges to specify step values. For example, */5 in the minutes field indicates every 5 minutes. It is shorthand for the more verbose form 5,10,15,20,25,30,35,40,45,50,55,00.


### Daylight Saving Time (DST)

For ease of understanding, we consider a case, where a one-hour shift occurs at 02:00 local time, in spring the clock jumps forward from the last instant of 01:59 standard time to 03:00 DST, whereas in autumn the clock jumps backward from the last instant of 01:59 am DST to 01:00 am standard time, repeating that hour.

Cron behavior is clear and logical until Daylight saving time comes. Lots of implementations don't handle the DST. Thus if a job is scheduled for 1:30 am then it can be performed twice: before and after shifting clock backward. Or worse, if your job should be performed at 2:30 every day when time will jump from 2:00 am to 3:00 am the job won't be performed. Very bad.

The easiest solution: do not schedule any cron jobs from 1:00 am to 3:00 am on Sunday morning! But who rememberes about DST until it comes?

Cronos solution: properly handle the daylight saving time. But before we should answer to questions:
- If a job should be performed in the period of the clock jumping should we perform it?
- How many times?

**Shift clocks forward**.
A job will be skipped if it is secondly or minutely (second or minute field conatains '*') and it should be performed from 2:00 am to 2:59 am. In other cases, a job will be performed once at 3:00 am. See examples:
* `0 30 * * * ?`, `0 30 1-12 * * ?` - job will be performed at 1:30 am, then at 3:00 am and then at 3:30 am. 
* `0 30 2 * * ?` - job will be perfomed at 3:00 am.
* `0 * * * * ?` - job will be performed every minute from 1:00 am to 2:00 am. Then time will jump from 2:00 am to 3:00 am. Then the job will continue to run every minute. So all jobs which had to be perfomed from 2:00 am to 2:59 am will be skipped. 
* `0 * 2 ? * *` - job will be skipped in that day. So it will be performed just next day every minute from 2:00 am to 2:59 am.

**Shift clocks backward**. 
A job will be performed twice before and after the clock shifting if it is secondly, minutely or hourly (an cron expression contains '*' in second, minute or hour field). In other cases a job will be performed only before the clock shifting. See examples:
* `0 30 * * * ?` - job will be scheduled at 1:30 am before and after shifting clock backward. 
* `0 30 1 * * ?`, `0 30 1,2,10-12 * * ?` - job will be scheduled once at 1:30 before shifting clock backward.
* `0 * * * * ?` - job will be perfomed every minute from 1:00 am to 1:59 am. After shifting clock backward job will continue to run every second.
* `0 * 1 ? * *` - job will be perfomed every minute from 1:00 am to 1:59 am. After shifting clock backward job will continue to run every second from 1:00 am to 1:59 am.

## License

Cronos is under the [Apache License 2.0][Apache-2.0].

[Apache-2.0]:LICENSE

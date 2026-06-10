/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { IUtf8SpanFormattable } from "./i-utf8-span-formattable";

export class TimeSpan implements IUtf8SpanFormattable {
    static readonly zero: TimeSpan;
    static readonly maxValue: TimeSpan = "10675199.02:48:05.4775807";
    static readonly minValue: TimeSpan = "-10675199.02:48:05.4775808";
    static readonly nanosecondsPerTick: number = 100;
    static readonly ticksPerMicrosecond: number = 10;
    static readonly ticksPerMillisecond: number = 10000;
    static readonly ticksPerSecond: number = 10000000;
    static readonly ticksPerMinute: number = 600000000;
    static readonly ticksPerHour: number = 36000000000;
    static readonly ticksPerDay: number = 864000000000;
    static readonly microsecondsPerMillisecond: number = 1000;
    static readonly microsecondsPerSecond: number = 1000000;
    static readonly microsecondsPerMinute: number = 60000000;
    static readonly microsecondsPerHour: number = 3600000000;
    static readonly microsecondsPerDay: number = 86400000000;
    static readonly millisecondsPerSecond: number = 1000;
    static readonly millisecondsPerMinute: number = 60000;
    static readonly millisecondsPerHour: number = 3600000;
    static readonly millisecondsPerDay: number = 86400000;
    static readonly secondsPerMinute: number = 60;
    static readonly secondsPerHour: number = 3600;
    static readonly secondsPerDay: number = 86400;
    static readonly minutesPerHour: number = 60;
    static readonly minutesPerDay: number = 1440;
    static readonly hoursPerDay: number = 24;
    ticks: number;
    days: number;
    hours: number;
    milliseconds: number;
    microseconds: number;
    nanoseconds: number;
    minutes: number;
    seconds: number;
    totalDays: number;
    totalHours: number;
    totalMilliseconds: number;
    totalMicroseconds: number;
    totalNanoseconds: number;
    totalMinutes: number;
    totalSeconds: number;
}

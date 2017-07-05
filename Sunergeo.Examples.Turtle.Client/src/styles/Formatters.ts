import * as DateFormat from 'dateformat';

export namespace DateFormats {
    export const ShortDateAndTime = "d mmm yyyy, hh:MM TT"     // eg. 28 Feb 2017, 04:47 PM
}

// Format characters:
// d	Day of the month as digits; no leading zero for single-digit days.
// dd	Day of the month as digits; leading zero for single-digit days.
// ddd	Day of the week as a three-letter abbreviation.
// dddd	Day of the week as its full name.
// m	Month as digits; no leading zero for single-digit months.
// mm	Month as digits; leading zero for single-digit months.
// mmm	Month as a three-letter abbreviation.
// mmmm	Month as its full name.
// yy	Year as last two digits; leading zero for years less than 10.
// yyyy	Year represented by four digits.
// h	Hours; no leading zero for single-digit hours (12-hour clock).
// hh	Hours; leading zero for single-digit hours (12-hour clock).
// H	Hours; no leading zero for single-digit hours (24-hour clock).
// HH	Hours; leading zero for single-digit hours (24-hour clock).
// M	Minutes; no leading zero for single-digit minutes.
// MM	Minutes; leading zero for single-digit minutes.
// N	ISO 8601 numeric representation of the day of the week.
// o	GMT/UTC timezone offset, e.g. -0500 or +0230.
// s	Seconds; no leading zero for single-digit seconds.
// ss	Seconds; leading zero for single-digit seconds.
// S	The date's ordinal suffix (st, nd, rd, or th). Works well with d.
// l	Milliseconds; gives 3 digits.
// L	Milliseconds; gives 2 digits.
// t	Lowercase, single-character time marker string: a or p.
// tt	Lowercase, two-character time marker string: am or pm.
// T	Uppercase, single-character time marker string: A or P.
// TT	Uppercase, two-character time marker string: AM or PM.
// W	ISO 8601 week number of the year, e.g. 42
// Z	US timezone abbreviation, e.g. EST or MDT. With non-US timezones or in the
// '...', "..."	Literal character sequence. Surrounding quotes are removed.
// UTC:	Must be the first four characters of the mask. Converts the date from local time to UTC/GMT/Zulu time before applying the mask. The "UTC:" prefix is removed.
export const dateToString = (value:Date, format?:string):string => DateFormat(value, format || DateFormats.ShortDateAndTime)

export const ticksToString = (value:number, format?:string):string => dateToString(new Date(value*1000), format)

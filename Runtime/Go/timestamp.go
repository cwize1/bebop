package bebop

// Timestamp represents a UTC date/time value.
type Timestamp struct {
	// The number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001 UTC in the Gregorian calendar.
	Ticks int64
}

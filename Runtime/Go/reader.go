package bebop

import (
	"math"
	"math/bits"

	"github.com/google/uuid"
)

const (
	// ErrorUnexpectedEOF means not enough bytes were available for the type.
	ErrorUnexpectedEOF = -1

	// ArrayTooLong means the string or array value is too long for the system.
	ArrayTooLong = -2
)

// ReadBool reads a bool value.
func ReadBool(in []byte) (value bool, num int) {
	v, n := ReadByte(in)
	return (v != 0), n
}

// ReadByte reads a byte value.
func ReadByte(in []byte) (value byte, num int) {
	const length = 1

	if len(in) < length {
		return 0, ErrorUnexpectedEOF
	}

	return in[0], length
}

// ReadUInt16 reads a uint16 value.
func ReadUInt16(in []byte) (value uint16, num int) {
	const length = 4

	if len(in) < length {
		return 0, ErrorUnexpectedEOF
	}

	v := uint16(in[0])<<0 | uint16(in[1])<<8
	return v, length
}

// ReadUInt32 reads a uint32 value.
func ReadUInt32(in []byte) (value uint32, num int) {
	const length = 4

	if len(in) < length {
		return 0, ErrorUnexpectedEOF
	}

	v := uint32(in[0])<<0 | uint32(in[1])<<8 | uint32(in[2])<<16 | uint32(in[3])<<24
	return v, length
}

// ReadUInt64 reads a uint64 value.
func ReadUInt64(in []byte) (value uint64, num int) {
	const length = 8

	if len(in) < length {
		return 0, ErrorUnexpectedEOF
	}

	v := uint64(in[0])<<0 | uint64(in[1])<<8 | uint64(in[2])<<16 | uint64(in[3])<<24 | uint64(in[4])<<32 | uint64(in[5])<<40 | uint64(in[6])<<48 | uint64(in[7])<<56
	return v, length
}

// ReadInt16 reads a int16 value.
func ReadInt16(in []byte) (value int16, num int) {
	v, n := ReadUInt16(in)
	return int16(v), n
}

// ReadInt32 reads a int32 value.
func ReadInt32(in []byte) (value int32, num int) {
	v, n := ReadUInt32(in)
	return int32(v), n
}

// ReadInt64 reads a int64 value.
func ReadInt64(in []byte) (value int64, num int) {
	v, n := ReadUInt64(in)
	return int64(v), n
}

// ReadFloat32 reads a float32 value.
func ReadFloat32(in []byte) (value float32, num int) {
	v, n := ReadUInt32(in)
	return math.Float32frombits(v), n
}

// ReadFloat64 reads a float64 value.
func ReadFloat64(in []byte) (value float64, num int) {
	v, n := ReadUInt64(in)
	return math.Float64frombits(v), n
}

// ReadTimestamp reads a bebop.Timestamp value.
func ReadTimestamp(in []byte) (value Timestamp, num int) {
	v, n := ReadInt64(in)
	return Timestamp{Ticks: v}, n
}

// ReadGUID reads a UUID value.
func ReadGUID(in []byte) (value uuid.UUID, num int) {
	const length = 16

	if len(in) < length {
		return uuid.UUID{}, ErrorUnexpectedEOF
	}

	// Order is: 3 2 1 0, 5 4, 7 6, 8 9 10 11 12 13 14 15
	v := uuid.UUID{
		in[3], in[2], in[1], in[0],
		in[5], in[4],
		in[7], in[6],
		in[8], in[9], in[10], in[11], in[12], in[13], in[14], in[15]}

	return v, length
}

// ReadString reads a string value.
func ReadString(in []byte) (value string, num int) {
	v, n := ReadByteArray(in)
	if n < 0 {
		return "", n
	}
	return string(v), n
}

// ReadByteArray reads a byte array.
func ReadByteArray(in []byte) (value []byte, num int) {
	arrayLength, n := ReadUInt32(in)
	if n < 0 {
		return nil, n
	}

	if bits.UintSize == 32 && arrayLength > uint32(math.MaxInt32) {
		return nil, ArrayTooLong
	}

	in = in[n:]
	arrayLengthAsInt := int(arrayLength)

	if len(in) < arrayLengthAsInt {
		return nil, ErrorUnexpectedEOF
	}

	return in[:arrayLengthAsInt], n + arrayLengthAsInt
}

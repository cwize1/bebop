package bebop

import (
	"math"

	"github.com/google/uuid"
)

// WriteBool writes a bool value.
func WriteBool(out []byte, value bool) []byte {
	tmp := byte(0)
	if value {
		tmp = 1
	}
	return WriteByte(out, tmp)
}

// WriteByte writes a byte value.
func WriteByte(out []byte, value byte) []byte {
	return append(out, value)
}

// WriteUInt16 writes a uint16 value.
func WriteUInt16(out []byte, value uint16) []byte {
	return append(out, byte(value>>0), byte(value>>8))
}

// WriteUInt32 writes a uint32 value.
func WriteUInt32(out []byte, value uint32) []byte {
	return append(out, byte(value>>0), byte(value>>8), byte(value>>16), byte(value>>24))
}

// WriteUInt64 writes a uint64 value.
func WriteUInt64(out []byte, value uint64) []byte {
	return append(out, byte(value>>0), byte(value>>8), byte(value>>16), byte(value>>24), byte(value>>32), byte(value>>40), byte(value>>48), byte(value>>56))
}

// WriteInt16 writes a int16 value.
func WriteInt16(out []byte, value int16) []byte {
	return WriteUInt16(out, uint16(value))
}

// WriteInt32 writes a int32 value.
func WriteInt32(out []byte, value int32) []byte {
	return WriteUInt32(out, uint32(value))
}

// WriteInt64 writes a int64 value.
func WriteInt64(out []byte, value int64) []byte {
	return WriteUInt64(out, uint64(value))
}

// WriteFloat32 writes a float32 value.
func WriteFloat32(out []byte, value float32) []byte {
	return WriteUInt32(out, math.Float32bits(value))
}

// WriteFloat64 writes a float64 value.
func WriteFloat64(out []byte, value float64) []byte {
	return WriteUInt64(out, math.Float64bits(value))
}

// WriteTimestamp writes a bebop.Timestamp value.
func WriteTimestamp(out []byte, value Timestamp) []byte {
	return WriteInt64(out, int64(value))
}

// WriteGUID writes a UUID value.
func WriteGUID(out []byte, value uuid.UUID) []byte {
	// Order is: 3 2 1 0, 5 4, 7 6, 8 9 10 11 12 13 14 15
	return append(out,
		value[3], value[2], value[1], value[0],
		value[5], value[4],
		value[7], value[6],
		value[8], value[9], value[10], value[11], value[12], value[13], value[14], value[15])
}

// WriteString writes a string value.
func WriteString(out []byte, value string) []byte {
	out = WriteUInt32(out, uint32(len(value)))
	return append(out, value...)
}

// WriteByteArray writes an array of bytes.
func WriteByteArray(out []byte, value []byte) []byte {
	return append(out, value...)
}

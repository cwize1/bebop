package bebop

import (
	"errors"
	"math"
	"math/bits"

	"github.com/google/uuid"
)

var (
	// ErrUnexpectedEOF means not enough bytes were available for the type.
	ErrUnexpectedEOF = errors.New("unexpected end of file")

	// ErrArrayTooLong means the string or array value is too long for the system.
	ErrArrayTooLong = errors.New("array too long")

	// ErrMessageBodyTooLong means the message body length is too long for the system.
	ErrMessageBodyTooLong = errors.New("message body too long")

	// ErrMessageBodyLengthEOF means the specified message size would go passed the end of file.
	ErrMessageBodyLengthEOF = errors.New("message length passed end of file")

	// ErrMessageBodyOverrun means the message's body didn't terminate within the length specified.
	ErrMessageBodyOverrun = errors.New("message body went passed the specified length")
)

// ReadBool reads a bool value.
func ReadBool(in []byte) (bool, []byte, error) {
	v, in, err := ReadByte(in)
	return (v != 0), in, err
}

// ReadByte reads a byte value.
func ReadByte(in []byte) (byte, []byte, error) {
	const length = 1

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	return in[0], in[length:], nil
}

// ReadUInt16 reads a uint16 value.
func ReadUInt16(in []byte) (uint16, []byte, error) {
	const length = 4

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	v := uint16(in[0])<<0 | uint16(in[1])<<8
	return v, in[length:], nil
}

// ReadUInt32 reads a uint32 value.
func ReadUInt32(in []byte) (uint32, []byte, error) {
	const length = 4

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	v := uint32(in[0])<<0 | uint32(in[1])<<8 | uint32(in[2])<<16 | uint32(in[3])<<24
	return v, in[length:], nil
}

// ReadUInt64 reads a uint64 value.
func ReadUInt64(in []byte) (uint64, []byte, error) {
	const length = 8

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	v := uint64(in[0])<<0 | uint64(in[1])<<8 | uint64(in[2])<<16 | uint64(in[3])<<24 | uint64(in[4])<<32 | uint64(in[5])<<40 | uint64(in[6])<<48 | uint64(in[7])<<56
	return v, in[length:], nil
}

// ReadInt16 reads a int16 value.
func ReadInt16(in []byte) (int16, []byte, error) {
	v, in, err := ReadUInt16(in)
	return int16(v), in, err
}

// ReadInt32 reads a int32 value.
func ReadInt32(in []byte) (int32, []byte, error) {
	v, in, err := ReadUInt32(in)
	return int32(v), in, err
}

// ReadInt64 reads a int64 value.
func ReadInt64(in []byte) (int64, []byte, error) {
	v, in, err := ReadUInt64(in)
	return int64(v), in, err
}

// ReadFloat32 reads a float32 value.
func ReadFloat32(in []byte) (float32, []byte, error) {
	v, in, err := ReadUInt32(in)
	return math.Float32frombits(v), in, err
}

// ReadFloat64 reads a float64 value.
func ReadFloat64(in []byte) (float64, []byte, error) {
	v, in, err := ReadUInt64(in)
	return math.Float64frombits(v), in, err
}

// ReadTimestamp reads a bebop.Timestamp value.
func ReadTimestamp(in []byte) (Timestamp, []byte, error) {
	v, in, err := ReadInt64(in)
	return Timestamp(v), in, err
}

// ReadGUID reads a UUID value.
func ReadGUID(in []byte) (uuid.UUID, []byte, error) {
	const length = 16

	if len(in) < length {
		return uuid.UUID{}, in, ErrUnexpectedEOF
	}

	// Order is: 3 2 1 0, 5 4, 7 6, 8 9 10 11 12 13 14 15
	v := uuid.UUID{
		in[3], in[2], in[1], in[0],
		in[5], in[4],
		in[7], in[6],
		in[8], in[9], in[10], in[11], in[12], in[13], in[14], in[15]}

	return v, in[length:], nil
}

// ReadString reads a string value.
func ReadString(in []byte) (string, []byte, error) {
	v, in, err := readByteArray(in)
	return string(v), in, err
}

// ReadByteArray reads a byte array.
func ReadByteArray(in []byte) ([]byte, []byte, error) {
	v, in, err := readByteArray(in)
	if err != nil {
		return nil, in, err
	}

	// Copy the result into a new array.
	return append([]byte(nil), v...), in, nil
}

// Reads a byte array from the input but returns an inline slice (not a new array).
func readByteArray(in []byte) ([]byte, []byte, error) {
	arrayLength, in, err := ReadArrayLength(in)
	if err != nil {
		return nil, in, err
	}

	if len(in) < arrayLength {
		return nil, in, ErrUnexpectedEOF
	}

	return in[:arrayLength], in[arrayLength:], nil
}

// ReadArrayLength reads an array length prefix.
func ReadArrayLength(in []byte) (int, []byte, error) {
	arrayLength, in, err := ReadUInt32(in)
	if err != nil {
		return 0, in, err
	}

	if bits.UintSize == 32 && arrayLength > uint32(math.MaxInt32) {
		return 0, in, ErrArrayTooLong
	}

	return int(arrayLength), in, nil
}

// ReadMessageLength reads the length of a message.
func ReadMessageLength(in []byte) (int, []byte, error) {
	arrayLength, in, err := ReadUInt32(in)
	if err != nil {
		return 0, in, err
	}

	if bits.UintSize == 32 && arrayLength > uint32(math.MaxInt32) {
		return 0, in, ErrMessageBodyTooLong
	}

	arrayLengthInt := int(arrayLength)

	if arrayLengthInt > len(in) {
		return 0, in, ErrMessageBodyLengthEOF
	}

	return arrayLengthInt, in, nil
}

// +build 386 amd64 arm arm64
//
// On platforms that are native little endian and support unaligned reads and writes,
// the integer encode and decode operations can be optimized using unsafe code.

package bebop

import (
	"unsafe"
)

// ReadUInt16 reads a uint16 value.
func ReadUInt16(in []byte) (uint16, []byte, error) {
	const length = 2

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	v := *(*uint16)(unsafe.Pointer(&in[0]))
	return v, in[length:], nil
}

// ReadUInt32 reads a uint32 value.
func ReadUInt32(in []byte) (uint32, []byte, error) {
	const length = 4

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	v := *(*uint32)(unsafe.Pointer(&in[0]))
	return v, in[length:], nil
}

// ReadUInt64 reads a uint64 value.
func ReadUInt64(in []byte) (uint64, []byte, error) {
	const length = 8

	if len(in) < length {
		return 0, in, ErrUnexpectedEOF
	}

	v := *(*uint64)(unsafe.Pointer(&in[0]))
	return v, in[length:], nil
}

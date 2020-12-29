// +build 386 amd64 arm arm64
//
// On platforms that are native little endian and support unaligned reads and writes,
// the integer encode and decode operations can be optimized using unsafe code.

package bebop

import (
	"unsafe"
)

// WriteUInt16 writes a uint16 value.
func WriteUInt16(out []byte, value uint16) []byte {
	return append(out, (*(*[2]byte)(unsafe.Pointer(&value)))[:]...)
}

// WriteUInt32 writes a uint32 value.
func WriteUInt32(out []byte, value uint32) []byte {
	return append(out, (*(*[4]byte)(unsafe.Pointer(&value)))[:]...)
}

// WriteUInt64 writes a uint64 value.
func WriteUInt64(out []byte, value uint64) []byte {
	return append(out, (*(*[8]byte)(unsafe.Pointer(&value)))[:]...)
}

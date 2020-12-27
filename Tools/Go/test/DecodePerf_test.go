package test

import (
	"encoding/binary"
	"math/rand"
	"testing"
	"unsafe"

	"github.com/RainwayApp/bebop/Runtime/Go/bebop"
)

const (
	readArrayLength = 100
)

func BenchmarkReadUInt64_1(b *testing.B) {
	var out []byte
	for j := 0; j < readArrayLength; j++ {
		out = writeUInt64_1(out, rand.Uint64())
	}

	b.ResetTimer()

	for i := 0; i < b.N; i++ {
		in := out

		for j := 0; j < readArrayLength; j++ {
			var err error
			var v uint64
			v, in, err = readUInt64_1(in)
			if err != nil {
				b.Fatal("Read failed: ", err)
			}
			_ = v
		}
	}
}

func BenchmarkReadUInt64_2(b *testing.B) {
	var out []byte
	for j := 0; j < readArrayLength; j++ {
		out = writeUInt64_1(out, rand.Uint64())
	}

	b.ResetTimer()

	for i := 0; i < b.N; i++ {
		in := out

		for j := 0; j < readArrayLength; j++ {
			var err error
			var v uint64
			v, in, err = readUInt64_2(in)
			if err != nil {
				b.Fatal("Read failed: ", err)
			}
			_ = v
		}
	}
}

func BenchmarkReadUInt64_3(b *testing.B) {
	var out []byte
	for j := 0; j < readArrayLength; j++ {
		out = writeUInt64_1(out, rand.Uint64())
	}

	b.ResetTimer()

	for i := 0; i < b.N; i++ {
		in := out

		for j := 0; j < readArrayLength; j++ {
			var err error
			var v uint64
			v, in, err = readUInt64_3(in)
			if err != nil {
				b.Fatal("Read failed: ", err)
			}
			_ = v
		}
	}
}

func readUInt64_1(in []byte) (uint64, []byte, error) {
	const length = 8

	if len(in) < length {
		return 0, in, bebop.ErrUnexpectedEOF
	}

	v := uint64(in[0])<<0 | uint64(in[1])<<8 | uint64(in[2])<<16 | uint64(in[3])<<24 | uint64(in[4])<<32 | uint64(in[5])<<40 | uint64(in[6])<<48 | uint64(in[7])<<56
	return v, in[length:], nil
}

func readUInt64_2(in []byte) (uint64, []byte, error) {
	const length = 8

	if len(in) < length {
		return 0, in, bebop.ErrUnexpectedEOF
	}

	v := binary.LittleEndian.Uint64(in)
	return v, in[length:], nil
}

func readUInt64_3(in []byte) (uint64, []byte, error) {
	const length = 8

	if len(in) < length {
		return 0, in, bebop.ErrUnexpectedEOF
	}

	v := *(*uint64)(unsafe.Pointer(&in[0]))
	return v, in[length:], nil
}

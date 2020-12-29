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

func BenchmarkReadUInt64_V2_1(b *testing.B) {
	out := makeBuffer()
	ins := make([][]byte, 0, b.N/readArrayLength+1)
	var values [readArrayLength]uint64

	b.ResetTimer()

	for i := 0; i < b.N; {
		in := out
		blockLength := min(i+readArrayLength, b.N)

		for ; i < blockLength; i++ {
			var err error
			var v uint64
			v, in, err = readUInt64_1(in)
			if err != nil {
				b.Fatal("Read failed: ", err)
			}
			values[i%readArrayLength] = v
		}

		ins = append(ins, in)
	}
}

func BenchmarkReadUInt64_V2_2(b *testing.B) {
	out := makeBuffer()
	ins := make([][]byte, 0, b.N/readArrayLength+1)
	var values [readArrayLength]uint64

	b.ResetTimer()

	for i := 0; i < b.N; {
		in := out
		blockLength := min(i+readArrayLength, b.N)

		for ; i < blockLength; i++ {
			var err error
			var v uint64
			v, in, err = readUInt64_2(in)
			if err != nil {
				b.Fatal("Read failed: ", err)
			}
			values[i%readArrayLength] = v
		}

		ins = append(ins, in)
	}
}

func BenchmarkReadUInt64_V2_3(b *testing.B) {
	out := makeBuffer()
	ins := make([][]byte, 0, b.N/readArrayLength+1)
	var values [readArrayLength]uint64

	b.ResetTimer()

	for i := 0; i < b.N; {
		in := out
		blockLength := min(i+readArrayLength, b.N)

		for ; i < blockLength; i++ {
			var err error
			var v uint64
			v, in, err = readUInt64_3(in)
			if err != nil {
				b.Fatal("Read failed: ", err)
			}
			values[i%readArrayLength] = v
		}

		ins = append(ins, in)
	}
}

func makeBuffer() []byte {
	var out []byte
	for j := 0; j < readArrayLength; j++ {
		out = writeUInt64_1(out, rand.Uint64())
	}
	return out
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

package test

import (
	"encoding/binary"
	"math/rand"
	"testing"
	"unsafe"
)

const (
	writeArrayLength = 100
)

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}

func BenchmarkWriteUInt64_1(b *testing.B) {
	value := rand.Uint64()
	outs := make([][]byte, 0, b.N/writeArrayLength+1)

	b.ResetTimer()
	for i := 0; i < b.N; {
		blockLength := min(i+writeArrayLength, b.N)

		var out []byte
		for ; i < blockLength; i++ {
			out = writeUInt64_1(out, value)
		}
		outs = append(outs, out)
	}
}

func BenchmarkWriteUInt64_2(b *testing.B) {
	value := rand.Uint64()
	outs := make([][]byte, 0, b.N/writeArrayLength+1)

	b.ResetTimer()
	for i := 0; i < b.N; {
		blockLength := min(i+writeArrayLength, b.N)

		var out []byte
		for ; i < blockLength; i++ {
			out = writeUInt64_2(out, value)
		}
		outs = append(outs, out)
	}
}

func BenchmarkWriteUInt64_3(b *testing.B) {
	value := rand.Uint64()
	outs := make([][]byte, 0, b.N/writeArrayLength+1)

	b.ResetTimer()
	for i := 0; i < b.N; {
		blockLength := min(i+writeArrayLength, b.N)

		var out []byte
		for ; i < blockLength; i++ {
			out = writeUInt64_3(out, value)
		}
		outs = append(outs, out)
	}
}

func BenchmarkWriteUInt64_V2_1(b *testing.B) {
	value := rand.Uint64()

	b.ResetTimer()
	var out []byte
	for i := 0; i < b.N; i++ {
		out = writeUInt64_1(out, value)
	}
	b.StopTimer()

	_ = out[rand.Intn(b.N)]
}

func BenchmarkWriteUInt64_V2_2(b *testing.B) {
	value := rand.Uint64()

	b.ResetTimer()
	var out []byte
	for i := 0; i < b.N; i++ {
		out = writeUInt64_2(out, value)
	}
	b.StopTimer()

	_ = out[rand.Intn(b.N)]
}

func BenchmarkWriteUInt64_V2_3(b *testing.B) {
	value := rand.Uint64()

	b.ResetTimer()
	var out []byte
	for i := 0; i < b.N; i++ {
		out = writeUInt64_3(out, value)
	}
	b.StopTimer()

	_ = out[rand.Intn(b.N)]
}

func writeUInt64_1(out []byte, value uint64) []byte {
	return append(out, byte(value>>0), byte(value>>8), byte(value>>16), byte(value>>24), byte(value>>32), byte(value>>40), byte(value>>48), byte(value>>56))
}

func writeUInt64_2(out []byte, value uint64) []byte {
	const length = 8
	var tmp [length]byte
	binary.LittleEndian.PutUint64(tmp[:], value)
	return append(out, tmp[:]...)
}

func writeUInt64_3(out []byte, value uint64) []byte {
	return append(out, (*(*[8]byte)(unsafe.Pointer(&value)))[:]...)
}

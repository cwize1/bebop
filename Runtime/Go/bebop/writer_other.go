// +build !386,!amd64,!arm,!arm64

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

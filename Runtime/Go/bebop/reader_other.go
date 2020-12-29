// +build !386,!amd64,!arm,!arm64

package bebop

// ReadUInt16 reads a uint16 value.
func ReadUInt16(in []byte) (uint16, []byte, error) {
	const length = 2

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

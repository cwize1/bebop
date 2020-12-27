//go:generate dotnet run --project ../../../Compiler/ --config bebopConfig.json
//go:generate dotnet run --project ../../../Compiler/ --files "../../../Laboratory/Schemas/array_of_strings.bop" --go generated/arrofstr/arrofstr.go --gopkg arrofstr

package test

import (
	"reflect"
	"testing"

	"github.com/RainwayApp/bebop/Runtime/Go/bebop"
	"github.com/RainwayApp/bebop/Tools/Go/test/generated/arrofstr"
	"github.com/RainwayApp/bebop/Tools/Go/test/generated/other"
	"github.com/google/uuid"
)

type BebopStruct interface {
	Encode(out []byte) []byte
	Decode(in []byte) ([]byte, error)
}

func TestEncodeDecodeEqual(t *testing.T) {
	testEncodeDecodeEqual(t, &arrofstr.ArrayOfStrings{Strings: []string{"", "a", "bb", "ccc"}})
	testEncodeDecodeEqual(t, &other.BasicArrays{
		ABool:    []bool{false, true},
		AByte:    []byte{0, 1},
		AInt16:   []int16{-2, 3},
		AUint16:  []uint16{4, 5},
		AInt32:   []int32{6, -7},
		AUint32:  []uint32{8, 9},
		AInt64:   []int64{-10, 11},
		AUint64:  []uint64{12, 13},
		AFloat32: []float32{-14.14, 15.15},
		AFloat64: []float64{16.16, -17.17},
		AString:  []string{"18", "19"},
		AGuid:    []uuid.UUID{uuid.New(), uuid.New()},
	})
	testEncodeDecodeEqual(t, &other.BasicTypes{
		ABool:    true,
		AByte:    1,
		AInt16:   -2,
		AUint16:  3,
		AInt32:   -4,
		AUint32:  5,
		AInt64:   -6,
		AUint64:  7,
		AFloat32: -8.8,
		AFloat64: 9.9,
		AString:  "10",
		AGuid:    uuid.New(),
		ADate:    bebop.Timestamp(10),
	})
	testEncodeDecodeEqual(t, &other.Library{
		Songs: map[uuid.UUID]other.Song{
			uuid.New(): other.Song{},
			uuid.New(): other.Song{
				Title: (func() *string {
					tmp := "string"
					return &tmp
				})(),
				Year: (func() *uint16 {
					tmp := uint16(1990)
					return &tmp
				})(),
				Performers: []other.Musician{
					{
						Name:  "Player 1",
						Plays: other.Instrument_Clarinet,
					},
					{
						Name:  "Player 2",
						Plays: other.Instrument_Sax,
					},
				},
			},
			uuid.New(): other.Song{
				Performers: []other.Musician{
					{
						Name:  "Player 3",
						Plays: other.Instrument_Trumpet,
					},
					{
						Name:  "Player 4",
						Plays: other.Instrument_Clarinet,
					},
				},
			},
		},
	})
}

func invokeMethodByName(methodName string, obj interface{}, args ...interface{}) []interface{} {
	inputs := make([]reflect.Value, len(args))
	for i := range args {
		inputs[i] = reflect.ValueOf(args[i])
	}
	resultValues := reflect.ValueOf(obj).MethodByName(methodName).Call(inputs)
	resultInterfaces := make([]interface{}, len(resultValues))
	for i := range resultValues {
		resultInterfaces[i] = resultValues[i].Interface()
	}
	return resultInterfaces
}

func deref(value interface{}) interface{} {
	return reflect.ValueOf(value).Elem().Interface()
}

func testEncodeDecodeEqual(t *testing.T, value interface{}) {
	encoded := invokeMethodByName("Encode", value, []byte(nil))[0].([]byte)

	decoded := reflect.New(reflect.TypeOf(value).Elem()).Interface()

	decodeResult := invokeMethodByName("Decode", decoded, encoded)
	decodeEnd, err := decodeResult[0].([]byte), decodeResult[1]
	if err != nil {
		t.Errorf("Decode failed: %v: %v", err, value)
		return
	}

	if len(decodeEnd) != 0 {
		t.Errorf("Decode left data in buffer: %v bytes: %v", len(decodeEnd), value)
		return
	}

	if !invokeMethodByName("Equal", deref(value), deref(decoded))[0].(bool) {
		t.Errorf("Decoded value is not the same as the original: %v", value)
		return
	}
}

func TestVersionCompatability(t *testing.T) {
	valueNewVersion := other.SkipTestNewContainer{
		S: &other.SkipTestNew{
			X: (func() *int32 {
				tmp := int32(1)
				return &tmp
			})(),
			Y: (func() *int32 {
				tmp := int32(2)
				return &tmp
			})(),
			Z: (func() *int32 {
				tmp := int32(3)
				return &tmp
			})(),
		},
		After: (func() *int32 {
			tmp := int32(42)
			return &tmp
		})(),
	}

	valueOldVersion := other.SkipTestOldContainer{
		S: &other.SkipTestOld{
			X: (func() *int32 {
				tmp := int32(1)
				return &tmp
			})(),
			Y: (func() *int32 {
				tmp := int32(2)
				return &tmp
			})(),
		},
		After: (func() *int32 {
			tmp := int32(42)
			return &tmp
		})(),
	}

	encodedNewVersion := valueNewVersion.Encode(nil)

	decodedOldVersion := other.SkipTestOldContainer{}
	_, err := decodedOldVersion.Decode(encodedNewVersion)
	if err != nil {
		t.Errorf("Old version decode failed: %v", err)
	}

	if !valueOldVersion.Equal(decodedOldVersion) {
		t.Errorf("Decoded old version didn't match expected")
	}

	encodedOldVersion := decodedOldVersion.Encode(nil)

	decodedNewVersion := other.SkipTestNewContainer{}
	_, err = decodedNewVersion.Decode(encodedOldVersion)
	if err != nil {
		t.Errorf("New version decode failed: %v", err)
	}

	valueNewVersion.S.Z = nil
	if !valueNewVersion.Equal(decodedNewVersion) {
		t.Errorf("Decoded new version didn't match expected")
	}
}

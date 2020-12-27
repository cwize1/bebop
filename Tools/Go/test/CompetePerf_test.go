package test

import (
	"math/rand"
	"testing"

	"github.com/RainwayApp/bebop/Tools/Go/test/generated/other"
	"github.com/google/uuid"
)

var (
	Data *other.Library
)

func init() {
	Data = &other.Library{
		Songs: make(map[uuid.UUID]other.Song),
	}

	for i := 0; i < 10; i++ {
		song := other.Song{}
		song.Title = new(string)
		*song.Title = randomString(10)

		song.Year = new(uint16)
		*song.Year = uint16(rand.Intn(2000))

		song.Performers = make([]other.Musician, 10)
		for j := 0; j < 10; j++ {
			song.Performers[j] = other.Musician{
				Name:  randomString(10),
				Plays: other.Instrument(rand.Intn(3)),
			}
		}

		Data.Songs[uuid.New()] = song
	}
}

func BenchmarkBebopEncode(b *testing.B) {
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_ = Data.Encode(nil)
	}
}

// From: https://stackoverflow.com/questions/22892120/how-to-generate-a-random-string-of-a-fixed-length-in-go/22892986#22892986
var letters = []rune("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")

func randomString(n int) string {
	b := make([]rune, n)
	for i := range b {
		b[i] = letters[rand.Intn(len(letters))]
	}
	return string(b)
}

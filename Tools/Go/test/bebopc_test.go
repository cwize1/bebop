//go:generate dotnet run --project ../../../Compiler/ --config bebopConfig.json

package test

import (
	"testing"

	"github.com/RainwayApp/bebop/Tools/Go/test/generated"
)

func TestBebopc(t *testing.T) {
	_ = generated.Musician{}
}

//go:generate dotnet run --project ../../../Compiler/ --config bebopConfig.json
//go:generate dotnet run --project ../../../Compiler/ --files "../../../Laboratory/Schemas/array_of_strings.bop" --go generated/arrofstr/arrofstr.go --gopkg arrofstr

package test

import (
	"testing"

	"github.com/RainwayApp/bebop/Tools/Go/test/generated/arrofstr"
	"github.com/RainwayApp/bebop/Tools/Go/test/generated/other"
)

func TestBebopc(t *testing.T) {
	_ = other.Musician{}
	_ = arrofstr.ArrayOfStrings{}
}

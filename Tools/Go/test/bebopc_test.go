//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/array_of_strings.bop --go ./generated/array_of_strings.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/basic_arrays.bop --go ./generated/basic_arrays.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/basic_types.bop --go ./generated/basic_types.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/documentation.bop --go ./generated/documentation.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/jazz.bop --go ./generated/jazz.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/lab.bop --go ./generated/lab.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/map_types.bop --go ./generated/map_types.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/msgpack_comparison.bop --go ./generated/msgpack_comparison.go --gopkg generated
//go:generate dotnet run --project ../../../Compiler/ --files ../../../Laboratory/Schemas/request.bop --go ./generated/request.go --gopkg generated

package test

import (
	"testing"

	"github.com/RainwayApp/bebop/Tools/Go/test/generated"
)

func TestBebopc(t *testing.T) {
	_ = generated.Musician{}
}

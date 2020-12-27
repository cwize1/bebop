module github.com/RainwayApp/bebop/Tools/Go/test

go 1.15

require (
	github.com/RainwayApp/bebop/Runtime/Go/bebop v0.0.0-00010101000000-000000000000
	github.com/google/uuid v1.1.2
)

replace (
	github.com/RainwayApp/bebop/Runtime/Go/bebop => ../../../Runtime/Go/bebop
)

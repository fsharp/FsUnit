namespace FsUnit.Test

open Xunit
open FsUnit.Xunit

type ``be Null tests``() =
    [<Fact>]
    member __.``null should be Null``() =
        null |> should be Null

    [<Fact>]
    member __.``null should fail to not be Null``() =
        shouldFail(fun () -> null |> should not' (be Null))

    [<Fact>]
    member __.``non-null should fail to be Null``() =
        shouldFail(fun () -> "something" |> should be Null)

    [<Fact>]
    member __.``non-null should not be Null``() =
        "something" |> should not' (be Null)

    [<Fact>]
    member __.``null should be null``() =
        null |> should be null

    [<Fact>]
    member __.``null should fail to not be null``() =
        shouldFail(fun () -> null |> should not' (be null))

    [<Fact>]
    member __.``non-null should fail to be null``() =
        shouldFail(fun () -> "something" |> should be null)

    [<Fact>]
    member __.``non-null should not be null``() =
        "something" |> should not' (be null)

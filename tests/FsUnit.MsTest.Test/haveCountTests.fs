namespace FsUnit.Test

open Microsoft.VisualStudio.TestTools.UnitTesting
open FsUnit.MsTest

[<TestClass>]
type ``have Count tests``() =
    let emptyList =
        new System.Collections.Generic.List<int>()

    let singleItemList =
        new System.Collections.Generic.List<int>()

    do singleItemList.Add(1)

    // Collection
    [<TestMethod>]
    member __.``Collection with 1 item should have Count 1``() =
        singleItemList |> should haveCount 1

    [<TestMethod>]
    member __.``empty Collection should fail to have Count 1``() =
        shouldFail(fun () -> emptyList |> should haveCount 1)

    [<TestMethod>]
    member __.``empty Collection should not have Count 1``() =
        emptyList |> should not' (haveCount 1)

    [<TestMethod>]
    member __.``Collection with 1 item should fail to not have Count 1``() =
        shouldFail(fun () -> singleItemList |> should not' (haveCount 1))

    // Seq
    [<TestMethod>]
    member __.``Seq with 1 item should fail to have Count 1``() =
        (fun () -> seq { 1 } |> should haveCount 1)
        |> should throw typeof<System.ArgumentException>

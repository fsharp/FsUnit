﻿module FsUnit.CustomMatchers

open System
open System.Collections
open NHamcrest
open NHamcrest.Core

let equal x = CustomMatcher<obj>(sprintf "Equals %s" (x.ToString()), fun a -> a = x)

//TODO: Look into a better way of doing this.
let equalWithin (t:obj) (x:obj) = CustomMatcher<obj>(sprintf "%s with a tolerance of %s" (x.ToString()) (t.ToString()), 
                                                     fun a -> let actualParsed, actual = Double.TryParse(string a)
                                                              let expectedParsed, expect = Double.TryParse(string x)
                                                              let toleranceParsed, tol = Double.TryParse(string t)
                                                              if actualParsed && expectedParsed && toleranceParsed then
                                                                  abs(actual - expect) <= tol
                                                              else false )

let not' (x:obj) = match box x with
                   | :? IMatcher<obj> as matcher -> Is.Not<obj>(matcher)
                   |  x -> Is.Not<obj>(CustomMatcher<obj>(sprintf "Equals %s" (x.ToString()), fun a -> a = x) :> IMatcher<obj>)

let throw (t:Type) = CustomMatcher<obj>(string t, 
                         fun f -> match f with
                                  | :? (unit -> unit) as testFunc -> 
                                      try
                                        testFunc() 
                                        false
                                      with
                                      | ex -> if ex.GetType() = t then true else false
                                  | _ -> false )

let be = id

let Null = Is.Null()

let EmptyString = CustomMatcher<obj>("A non empty string", fun s -> (string s).Trim() = "")

let NullOrEmptyString = CustomMatcher<obj>("A not empty or not null string", fun s -> String.IsNullOrEmpty(unbox s))

let True = CustomMatcher<obj>("True", fun b -> unbox b = true)

let False = CustomMatcher<obj>("False", fun b -> unbox b = false)

let sameAs x = Is.SameAs<obj>(x)

let greaterThan (x:obj) = CustomMatcher<obj>(string x, 
                                     fun actual -> (unbox actual :> IComparable).CompareTo(unbox x) > 0)

let greaterThanOrEqualTo (x:obj) = CustomMatcher<obj>(string x, 
                                              fun actual -> (unbox actual :> IComparable).CompareTo(unbox x) >= 0)

let lessThan (x:obj) = CustomMatcher<obj>(string x, 
                                    fun actual -> (unbox actual :> IComparable).CompareTo(unbox x) < 0)

let lessThanOrEqualTo (x:obj) = CustomMatcher<obj>(string x, 
                                           fun actual -> (unbox actual :> IComparable).CompareTo(unbox x) <= 0)

let endWith (x:string) = CustomMatcher<obj>(string x, fun s -> (string s).EndsWith x)

let startWith (x:string) = CustomMatcher<obj>(string x, fun s -> (string s).StartsWith x)

let ofExactType<'a> = CustomMatcher<obj>(typeof<'a>.ToString(), fun x -> (unbox x).GetType() = typeof<'a>)

let contain x = CustomMatcher<obj>(sprintf "Contains %s" (x.ToString()), 
                          fun c -> match c with
                                   | :? list<_> as l -> l |> List.exists(fun i -> i = x)
                                   | :? array<_> as a -> a |> Array.exists(fun i -> i = x)
                                   | :? seq<_> as s -> s |> Seq.exists(fun i -> i = x)
                                   | _ -> false)

let containf f = CustomMatcher<obj>(sprintf "Contains %s" (f.ToString()),
                          fun c -> match c with
                                   | :? list<_> as l -> l |> List.exists f
                                   | :? array<_> as a -> a |> Array.exists f
                                   | :? seq<_> as s -> s |> Seq.exists f
                                   | _ -> false)

